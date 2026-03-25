using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CombinedWatching.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the combined view is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of items to show.
    /// </summary>
    public int MaxItems { get; set; } = 24;

    /// <summary>
    /// Gets or sets the maximum days for next up cutoff.
    /// </summary>
    public int MaxDaysForNextUp { get; set; } = 365;

    /// <summary>
    /// Gets or sets the section title.
    /// </summary>
    public string SectionTitle { get; set; } = "Weiterschauen";
}
