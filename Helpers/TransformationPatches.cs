using System.Reflection;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.CombinedWatching.Helpers;

/// <summary>
/// Patches index.html to inject the fetch interceptor before all other scripts.
/// </summary>
public static class TransformationPatches
{
    /// <summary>
    /// Injects the Combined Watching scripts into index.html right after the opening head tag.
    /// </summary>
    public static string IndexHtml(PatchRequestPayload payload)
    {
        if (string.IsNullOrEmpty(payload.Contents))
        {
            return payload.Contents ?? string.Empty;
        }

        if (payload.Contents.Contains("CombinedWatching"))
        {
            return payload.Contents;
        }

        var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Jellyfin.Plugin.CombinedWatching.Web.inject.html");

        if (stream is null)
        {
            return payload.Contents;
        }

        using var reader = new StreamReader(stream);
        var injectHtml = reader.ReadToEnd();

        // Insert AFTER <head> tag — before all deferred scripts
        return Regex.Replace(
            payload.Contents,
            @"(<head[^>]*>)",
            $"$1{injectHtml}",
            RegexOptions.IgnoreCase);
    }
}
