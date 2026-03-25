using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.CombinedWatching.Services;

/// <summary>
/// Registers the index.html transformation with the File Transformation plugin.
/// </summary>
public class FileTransformationHostedService : IHostedService
{
    private readonly ILogger<FileTransformationHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTransformationHostedService"/> class.
    /// </summary>
    public FileTransformationHostedService(ILogger<FileTransformationHostedService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            RegisterTransformation();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register File Transformation. Is the File Transformation plugin installed?");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void RegisterTransformation()
    {
        var ftAssembly = AssemblyLoadContext.All
            .SelectMany(x => x.Assemblies)
            .FirstOrDefault(x => x.FullName?.Contains("FileTransformation", StringComparison.OrdinalIgnoreCase) ?? false);

        if (ftAssembly is null)
        {
            _logger.LogWarning("File Transformation plugin not found. Web UI injection will not work.");
            return;
        }

        var pluginInterfaceType = ftAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
        if (pluginInterfaceType is null)
        {
            _logger.LogWarning("File Transformation PluginInterface type not found.");
            return;
        }

        var payload = new JObject
        {
            { "id", CombinedWatchingPlugin.Instance!.Id.ToString() },
            { "fileNamePattern", "index.html" },
            { "callbackAssembly", GetType().Assembly.FullName },
            { "callbackClass", typeof(Helpers.TransformationPatches).FullName },
            { "callbackMethod", nameof(Helpers.TransformationPatches.IndexHtml) }
        };

        var registerMethod = pluginInterfaceType.GetMethod("RegisterTransformation");
        if (registerMethod is null)
        {
            _logger.LogWarning("RegisterTransformation method not found on PluginInterface.");
            return;
        }

        registerMethod.Invoke(null, new object?[] { payload });
        _logger.LogInformation("Combined Watching: Successfully registered index.html transformation.");
    }
}
