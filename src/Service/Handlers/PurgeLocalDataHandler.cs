using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyPlugin.Service.Settings;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyPlugin.Service.Handlers;

/// <summary>Called by VSPC before uninstall or on request: removes logs and the saved (encrypted) API key.</summary>
internal class PurgeLocalDataHandler
{
    public static async Task InvokeAsync(HttpContext context)
    {
        await Task.Yield();

        var logger = context.RequestServices.GetRequiredService<ILogger<PurgeLocalDataHandler>>();

        try
        {
            context.RequestServices.GetRequiredService<ApiKeyStore>().Delete();
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Failed to delete the saved API key during local data purge.");
        }

        if (Directory.Exists(PluginPaths.LogDirectory))
        {
            foreach (var filePath in Directory.GetFiles(PluginPaths.LogDirectory))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception deleteError)
                {
                    logger.LogWarning(deleteError, "Failed to delete file {FileName} during local data purge.", filePath);
                }
            }
        }

        context.Response.StatusCode = 200;
    }
}
