using System;
using System.IO;

namespace MyPlugin.Service;

/// <summary>Local folders used by the plugin service (logs, encrypted key ring, saved settings).</summary>
public static class PluginPaths
{
    public static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VdcUsage");

    public static readonly string LogDirectory = Path.Combine(DataDirectory, "Logs");
    public static readonly string KeyRingDirectory = Path.Combine(DataDirectory, "Keys");
    public static readonly string SettingsFile = Path.Combine(DataDirectory, "settings.json");
    public static readonly string MappingFile = Path.Combine(DataDirectory, "tenant-mapping.json");
}
