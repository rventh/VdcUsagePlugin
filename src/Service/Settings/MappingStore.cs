using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MyPlugin.Service.Settings;

/// <summary>
/// Remembers which VSPC company an admin picked for a Pulse tenant. Both sides are stored by ID, so renaming either one does not break the link.
/// The file holds no secrets.
/// </summary>
public sealed class MappingStore
{
    private readonly object _sync = new();

    public Dictionary<string, string> Load()
    {
        lock (_sync)
        {
            try
            {
                if (File.Exists(PluginPaths.MappingFile))
                {
                    var map = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(PluginPaths.MappingFile));
                    if (map != null)
                    {
                        return new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception)
            {
                // unreadable file: start with no manual links
            }

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Saves the link, or removes it when companyUid is empty.</summary>
    public void Set(string tenantUid, string companyUid)
    {
        lock (_sync)
        {
            var map = Load();
            if (string.IsNullOrEmpty(companyUid))
            {
                map.Remove(tenantUid);
            }
            else
            {
                map[tenantUid] = companyUid;
            }

            Directory.CreateDirectory(PluginPaths.DataDirectory);
            var temp = PluginPaths.MappingFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(map));
            File.Move(temp, PluginPaths.MappingFile, overwrite: true);
        }
    }
}
