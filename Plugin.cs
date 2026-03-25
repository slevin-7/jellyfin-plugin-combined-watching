using System.Globalization;
using Jellyfin.Plugin.CombinedWatching.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.CombinedWatching;

/// <summary>
/// Combined Watching plugin - merges "Continue Watching" and "Next Up" into one section.
/// </summary>
public class CombinedWatchingPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CombinedWatchingPlugin"/> class.
    /// </summary>
    public CombinedWatchingPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Combined Watching";

    /// <inheritdoc />
    public override string Description => "Merges Continue Watching and Next Up into a single home section.";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static CombinedWatchingPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Pages.configPage.html",
                    GetType().Namespace)
            }
        ];
    }
}
