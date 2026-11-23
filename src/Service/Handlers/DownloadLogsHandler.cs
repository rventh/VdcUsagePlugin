using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace MyPlugin.Service.Handlers;

internal class DownloadLogsHandler
{
    public static string ApplicationLogDirectory => PluginPaths.LogDirectory;

    public static async Task InvokeAsync(HttpContext context)
    {
        await Task.Yield();

        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/zip";
        context.Response.Headers["Content-Disposition"] = "attachment; filename=\"vdc_usage_plugin_logs.zip\"";

        using var archive = new ZipArchive(context.Response.Body, ZipArchiveMode.Create);
        if (!Directory.Exists(ApplicationLogDirectory))
        {
            return;
        }

        var logger = context.RequestServices.GetRequiredService<ILogger<DownloadLogsHandler>>();
        var notBeforeDate = DateTime.UtcNow - TimeSpan.FromDays(30);
        foreach (var filePath in Directory.GetFiles(ApplicationLogDirectory, "*.log"))
        {
            if (File.GetLastWriteTimeUtc(filePath) < notBeforeDate)
            {
                continue; // must be fresh enough
            }

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
                var fileEntry = archive.CreateEntry(Path.GetFileName(filePath));
                using var entryStream = fileEntry.Open();
                await fileStream.CopyToAsync(entryStream, 4096);
            }
            catch (Exception readError)
            {
                logger.LogWarning(readError, "Failed to read log file {FileName} due to an IO error.", filePath);
            }
        }
        await context.Response.Body.FlushAsync();
    }
}
