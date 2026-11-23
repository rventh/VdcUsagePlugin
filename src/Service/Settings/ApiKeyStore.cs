using Microsoft.AspNetCore.DataProtection;
using System;
using System.IO;
using System.Text.Json;

namespace MyPlugin.Service.Settings;

public sealed record StoredApiKey(string ApiKey, DateTimeOffset SavedAt, string SavedBy);

/// <summary>
/// Keeps the VSPC API key entered by the user. The key is encrypted with ASP.NET Core Data Protection
/// (key ring protected by Windows DPAPI where available) and is never returned to the UI.
/// </summary>
public sealed class ApiKeyStore
{
    private sealed class FileModel
    {
        public string? ProtectedKey { get; set; }
        public DateTimeOffset SavedAt { get; set; }
        public string? SavedBy { get; set; }
    }

    private readonly IDataProtector _protector;
    private readonly object _sync = new();

    public ApiKeyStore(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("VdcUsage.ApiKey.v1");
    }

    public StoredApiKey? Load()
    {
        lock (_sync)
        {
            if (!File.Exists(PluginPaths.SettingsFile))
            {
                return null;
            }

            try
            {
                var model = JsonSerializer.Deserialize<FileModel>(File.ReadAllText(PluginPaths.SettingsFile));
                if (model == null || string.IsNullOrEmpty(model.ProtectedKey))
                {
                    return null;
                }

                return new StoredApiKey(_protector.Unprotect(model.ProtectedKey), model.SavedAt, model.SavedBy ?? string.Empty);
            }
            catch (Exception)
            {
                // unreadable or tampered file: behave as if there is no key saved
                return null;
            }
        }
    }

    public void Save(string apiKey, string savedBy)
    {
        lock (_sync)
        {
            Directory.CreateDirectory(PluginPaths.DataDirectory);
            var model = new FileModel
            {
                ProtectedKey = _protector.Protect(apiKey),
                SavedAt = DateTimeOffset.UtcNow,
                SavedBy = savedBy,
            };
            File.WriteAllText(PluginPaths.SettingsFile, JsonSerializer.Serialize(model));
        }
    }

    public void Delete()
    {
        lock (_sync)
        {
            if (File.Exists(PluginPaths.SettingsFile))
            {
                File.Delete(PluginPaths.SettingsFile);
            }
        }
    }

    /// <summary>Safe hint for the UI: only the last four characters.</summary>
    public static string Hint(string apiKey)
    {
        var tail = apiKey.Length >= 4 ? apiKey.Substring(apiKey.Length - 4) : string.Empty;
        return "••••" + tail;
    }
}
