using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CombinedWatching.Api;

/// <summary>
/// Serves embedded web resources (JS, CSS) for the plugin.
/// </summary>
[ApiController]
[Route("CombinedWatching/Web")]
public class WebController : ControllerBase
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    /// <summary>
    /// Serves the plugin JavaScript.
    /// </summary>
    [HttpGet("plugin.js")]
    [AllowAnonymous]
    public IActionResult GetPluginJs()
    {
        var stream = _assembly.GetManifestResourceStream(
            "Jellyfin.Plugin.CombinedWatching.Web.plugin.js");
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, "application/javascript");
    }

    /// <summary>
    /// Serves the plugin CSS.
    /// </summary>
    [HttpGet("plugin.css")]
    [AllowAnonymous]
    public IActionResult GetPluginCss()
    {
        var stream = _assembly.GetManifestResourceStream(
            "Jellyfin.Plugin.CombinedWatching.Web.plugin.css");
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, "text/css");
    }

    /// <summary>
    /// Serves the loader JavaScript.
    /// </summary>
    [HttpGet("loader.js")]
    [AllowAnonymous]
    public IActionResult GetLoaderJs()
    {
        var stream = _assembly.GetManifestResourceStream(
            "Jellyfin.Plugin.CombinedWatching.Web.loader.js");
        if (stream is null)
        {
            return NotFound();
        }

        return File(stream, "application/javascript");
    }
}
