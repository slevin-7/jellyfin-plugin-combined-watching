using System.Security.Claims;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using User = Jellyfin.Database.Implementations.Entities.User;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CombinedWatching.Api;

/// <summary>
/// API controller that returns merged Continue Watching + Next Up items.
/// </summary>
[ApiController]
[Route("CombinedWatching")]
[Authorize]
public class CombinedWatchingController : ControllerBase
{
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ITVSeriesManager _tvSeriesManager;
    private readonly IDtoService _dtoService;
    private readonly IUserDataManager _userDataManager;
    private readonly ILogger<CombinedWatchingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CombinedWatchingController"/> class.
    /// </summary>
    public CombinedWatchingController(
        IUserManager userManager,
        ILibraryManager libraryManager,
        ITVSeriesManager tvSeriesManager,
        IDtoService dtoService,
        IUserDataManager userDataManager,
        ILogger<CombinedWatchingController> logger)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _tvSeriesManager = tvSeriesManager;
        _dtoService = dtoService;
        _userDataManager = userDataManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets the combined watching items (Continue Watching + Next Up merged and sorted by recency).
    /// </summary>
    [HttpGet("Items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetCombinedItems(
        [FromQuery] Guid? userId,
        [FromQuery] int limit = 24,
        [FromQuery] ItemFields[]? fields = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] bool enableImages = true,
        [FromQuery] int? imageTypeLimit = null,
        [FromQuery] bool enableUserData = true)
    {
        var config = CombinedWatchingPlugin.Instance?.Configuration;
        if (config is null || !config.Enabled)
        {
            return new QueryResult<BaseItemDto>();
        }

        var resolvedUserId = userId ?? GetAuthenticatedUserId();
        if (resolvedUserId == Guid.Empty)
        {
            return Unauthorized("Unable to determine user identity.");
        }

        var user = _userManager.GetUserById(resolvedUserId);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        var dtoOptions = new DtoOptions
        {
            Fields = fields ?? Array.Empty<ItemFields>(),
            EnableImages = enableImages,
            EnableUserData = enableUserData
        };

        if (imageTypeLimit.HasValue)
        {
            dtoOptions.ImageTypeLimit = imageTypeLimit.Value;
        }

        var effectiveLimit = Math.Min(limit, config.MaxItems);
        var seenIds = new HashSet<Guid>();
        var allItems = new List<BaseItem>();

        // 1. Resume items
        try
        {
            var resumeQuery = new InternalItemsQuery(user)
            {
                OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                IsResumable = true,
                Limit = effectiveLimit,
                IncludeItemTypes = new[]
                {
                    BaseItemKind.Movie,
                    BaseItemKind.Episode,
                    BaseItemKind.Video,
                    BaseItemKind.Audio,
                    BaseItemKind.AudioBook
                },
                IsVirtualItem = false,
                ParentId = parentId ?? Guid.Empty
            };

            var resumeResult = _libraryManager.GetItemsResult(resumeQuery);
            _logger.LogInformation("CombinedWatching: Resume returned {Count} items", resumeResult.Items.Count);

            foreach (var item in resumeResult.Items)
            {
                if (seenIds.Add(item.Id))
                {
                    allItems.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CombinedWatching: Resume query failed");
        }

        // 2. NextUp items
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-config.MaxDaysForNextUp);
            var nextUpQuery = new NextUpQuery
            {
                User = user,
                Limit = effectiveLimit,
                EnableTotalRecordCount = false,
                EnableResumable = true,
                EnableRewatching = false,
                NextUpDateCutoff = cutoff
            };

            if (parentId.HasValue)
            {
                nextUpQuery.ParentId = parentId;
            }

            var nextUpResult = _tvSeriesManager.GetNextUp(nextUpQuery, dtoOptions);
            _logger.LogInformation("CombinedWatching: NextUp returned {Count} items", nextUpResult.Items.Count);

            foreach (var item in nextUpResult.Items)
            {
                if (seenIds.Add(item.Id))
                {
                    allItems.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CombinedWatching: NextUp query failed");
        }

        // 3. Prefetch series dates in one query (avoids N+1 per NextUp episode)
        var seriesDates = PrefetchSeriesDates(user, allItems);

        // 4. Assign sort dates
        var itemsWithDates = allItems
            .Select(item => (Item: item, SortDate: GetSortDate(user, item, seriesDates)))
            .ToList();

        // 5. Sort ALL items by date descending and take limit
        var sorted = itemsWithDates
            .OrderByDescending(x => x.SortDate)
            .ToList();

        var minDateCount = sorted.Count(x => x.SortDate == DateTime.MinValue);
        if (minDateCount > 0)
        {
            _logger.LogWarning("CombinedWatching: {Count} items have DateTime.MinValue sort date (will be at bottom)", minDateCount);
        }

        var finalItems = sorted
            .Take(effectiveLimit)
            .Select(x => x.Item)
            .ToArray();

        var episodeCount = finalItems.OfType<Episode>().Count();
        var movieCount = finalItems.Length - episodeCount;
        _logger.LogInformation("CombinedWatching: Returning {Count} combined items ({MovieCount} movies/videos, {EpisodeCount} episodes)", finalItems.Length, movieCount, episodeCount);

        // 4. Convert to DTOs
        var dtos = _dtoService.GetBaseItemDtos(finalItems, dtoOptions, user);

        return new QueryResult<BaseItemDto>
        {
            Items = dtos,
            TotalRecordCount = dtos.Count
        };
    }

    /// <summary>
    /// Gets the plugin configuration for the client.
    /// </summary>
    [HttpGet("Config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetConfig()
    {
        var config = CombinedWatchingPlugin.Instance?.Configuration;
        return new
        {
            enabled = config?.Enabled ?? false,
            maxItems = config?.MaxItems ?? 24,
            sectionTitle = config?.SectionTitle ?? "Weiterschauen"
        };
    }

    /// <summary>
    /// Gets the best sort date for an item.
    /// For resume items: when the user last played them.
    /// For NextUp items: when the user last played that series.
    /// </summary>
    /// <summary>
    /// Prefetches the last played date per series in a single query, so GetSortDate
    /// doesn't need one query per NextUp episode.
    /// </summary>
    private Dictionary<Guid, DateTime> PrefetchSeriesDates(User user, IEnumerable<BaseItem> items)
    {
        var seriesIds = items
            .OfType<Episode>()
            .Where(e => e.Series is not null)
            .Select(e => e.Series!.Id)
            .Distinct()
            .ToList();

        var result = new Dictionary<Guid, DateTime>();

        if (seriesIds.Count == 0)
        {
            return result;
        }

        // Single query: get the most recently played episode per series
        try
        {
            var playedEpisodes = _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                IsPlayed = true,
                OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                Limit = seriesIds.Count * 2,
                IsVirtualItem = false
            });

            foreach (var ep in playedEpisodes.Items.OfType<Episode>())
            {
                if (ep.Series is null || result.ContainsKey(ep.Series.Id))
                {
                    continue;
                }

                var epData = _userDataManager.GetUserData(user, ep);
                if (epData?.LastPlayedDate is not null)
                {
                    result[ep.Series.Id] = epData.LastPlayedDate.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "CombinedWatching: Failed to prefetch series dates");
        }

        return result;
    }

    private DateTime GetSortDate(User user, BaseItem item, Dictionary<Guid, DateTime> seriesDates)
    {
        // Try the item's own last played date first
        var userData = _userDataManager.GetUserData(user, item);
        if (userData?.LastPlayedDate is not null)
        {
            return userData.LastPlayedDate.Value;
        }

        // For episodes, use prefetched series dates
        if (item is Episode episode && episode.Series is not null)
        {
            if (seriesDates.TryGetValue(episode.Series.Id, out var seriesDate))
            {
                return seriesDate;
            }

            // Fallback: series' own user data
            var seriesData = _userDataManager.GetUserData(user, episode.Series);
            if (seriesData?.LastPlayedDate is not null)
            {
                return seriesData.LastPlayedDate.Value;
            }
        }

        // Last resort: use item's DateCreated so it doesn't get sorted to the very bottom
        if (item.DateCreated != default)
        {
            return item.DateCreated;
        }

        return DateTime.MinValue;
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsed))
        {
            return parsed;
        }

        userIdClaim = User.FindFirstValue("Jellyfin-UserId");
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out parsed))
        {
            return parsed;
        }

        return Guid.Empty;
    }
}
