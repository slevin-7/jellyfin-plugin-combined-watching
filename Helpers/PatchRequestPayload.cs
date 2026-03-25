using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CombinedWatching.Helpers;

/// <summary>
/// Payload received from the File Transformation plugin callback.
/// </summary>
public class PatchRequestPayload
{
    /// <summary>
    /// Gets or sets the file contents to transform.
    /// </summary>
    [JsonPropertyName("contents")]
    public string? Contents { get; set; }
}
