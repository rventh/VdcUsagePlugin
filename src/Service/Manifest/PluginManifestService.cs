// Copyright © Veeam Software Group GmbH.

using System.Globalization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.Extensions.FileProviders;
using Veeam.AC.Service.PluginManager;

namespace MyPlugin.Service.Manifest;

public class PluginManifestService
{
    public const string MANIFEST_DEBUG_PATH = "plugin.manifest.xml";
    public const string MANIFEST_DEPLOYMENT_PATH = "content/any/any/plugin.manifest.xml";

    private readonly PluginManifest _manifest;

    public PluginManifest Current => _manifest;
    public Guid PluginId { get; }

    public PluginManifestService(IFileProvider fileProvider)
    {
        if (fileProvider == null) throw new ArgumentNullException(nameof(fileProvider));

        var manifestFile = fileProvider.GetFileInfo(MANIFEST_DEBUG_PATH);
        if (!manifestFile.Exists)
        {
            manifestFile = fileProvider.GetFileInfo(MANIFEST_DEPLOYMENT_PATH);
            if (!manifestFile.Exists)
            {
                throw new FileNotFoundException(
                    $"Unable to find Plugin's manifest file at '{MANIFEST_DEPLOYMENT_PATH}' or '{MANIFEST_DEBUG_PATH}' locations",
                    MANIFEST_DEPLOYMENT_PATH);
            }
        }

        var nugetSpecFile = fileProvider.GetDirectoryContents("").FirstOrDefault(file => file.Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        if (nugetSpecFile == null || !nugetSpecFile.Exists)
        {
            throw new FileNotFoundException("Unable to find Plugin's Nuget specification file at '/' location");
        }

        _manifest = ReadManifest(fileProvider, manifestFile, nugetSpecFile);
        PluginId = Guid.Parse(_manifest.PluginId);
    }

    private PluginManifest ReadManifest(IFileProvider fileProvider, IFileInfo manifestFile, IFileInfo nugetSpecFile)
    {
        using var nugetSpecStream = nugetSpecFile.CreateReadStream();
        var specificationDocument = XDocument.Load(nugetSpecStream);
        var nugetSpecNamespace = (XNamespace)"http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";
        var packageElement = specificationDocument.Element(nugetSpecNamespace + "package");
        if (packageElement == null)
            throw new InvalidOperationException("Missing required '<package>' element in package specification.");

        var metadataElement = packageElement.Element(nugetSpecNamespace + "metadata");
        if (metadataElement == null)
            throw new InvalidOperationException("Missing required '<metadata>' element in package specification.");

        using var manifestFileStream = manifestFile.CreateReadStream();
        var manifestDocument = XDocument.Load(manifestFileStream);
        var pluginElement = manifestDocument.Element("plugin");
        if (pluginElement == null)
            throw new InvalidOperationException("Missing required '<plugin>' element in plugin manifest.");

        var manifest = new PluginManifest
        {
            ManifestVersion = int.Parse(pluginElement.Attribute("manifestVersion")?.Value ?? "1"),

            PluginId = GetMetadataValue(pluginElement, "pluginId", required: true),
				PackageId = GetMetadataValue(metadataElement, nugetSpecNamespace + "id", required: true),
            Name = GetMetadataValue(pluginElement, "pluginName", required: true),
            Description = GetMetadataValue(metadataElement, nugetSpecNamespace + "description", required: false) ?? string.Empty,
            Version = GetMetadataValue(metadataElement, nugetSpecNamespace + "version", required: true),
            Icon = GetMetadataValue(pluginElement, "iconUrl", required: false) ?? GetMetadataValue(metadataElement, nugetSpecNamespace + "icon", required: true),

            ServiceExecutableName = GetMetadataValue(pluginElement, "serviceExecutableName", required: true),
            ServiceStartArguments = GetMetadataValue(pluginElement, "serviceStartArguments", required: false) ?? string.Empty,
            ServiceOsServiceName = GetMetadataValue(pluginElement, "serviceOsServiceName", required: false) ?? string.Empty,
            ServiceHosting = Enum.Parse<PluginHosting>(GetMetadataValue(pluginElement, "serviceHosting", required: false) ?? "external", ignoreCase: true),
            
            ServiceHttpPort = Convert.ToInt32(GetMetadataValue(pluginElement, "serviceHttpPort", true), CultureInfo.InvariantCulture),
            ServiceHealthCheckUrl = GetMetadataValue(pluginElement, "serviceHealthCheckUrl", required: false) ?? string.Empty,
            ServiceReadinessCheckUrl = GetMetadataValue(pluginElement, "serviceReadinessCheckUrl", required: false) ?? string.Empty,
            ServicePurgeDataRequestUrl = GetMetadataValue(pluginElement, "servicePurgeDataRequestUrl", required: false) ?? string.Empty,
            ServiceLogDownloadRequestUrl = GetMetadataValue(pluginElement, "serviceLogDownloadRequestUrl", required: false) ?? string.Empty,
            ServiceUseDb = Convert.ToBoolean(GetMetadataValue(pluginElement, "serviceUseDb", required: false) ?? "false", CultureInfo.InvariantCulture),
            ServiceApiKeyObtainMethod = Enum.Parse<PluginApiKeyObtainMethod>(GetMetadataValue(pluginElement, "serviceApiKeyObtainMethod", required: false) ?? "none", ignoreCase: true),

            AgentExecutableName = GetMetadataValue(pluginElement, "agentExecutableName", required: false) ?? string.Empty,
            AgentStartArguments = GetMetadataValue(pluginElement, "agentStartArguments", required: false) ?? string.Empty,
        };
        

        foreach (var (relativePath, fileInfo) in EnumerateFilesRecursive(fileProvider, "content/"))
        {
            // content/<platform>/<architecture>/<purpose>/...
            var pathParts = relativePath.Split(new[] { '/', '\\' });
            if (pathParts.Length < 5) continue;

            PluginFilePurpose purpose;
            switch (pathParts[3])
            {
                case "service": purpose = PluginFilePurpose.ServiceContent; break;
                case "agent": purpose = PluginFilePurpose.AgentContent; break;
                case "ui-script": purpose = PluginFilePurpose.UiScript; break;
                case "ui-content": purpose = PluginFilePurpose.UiContent; break;
                case "ui-layout-config": purpose = PluginFilePurpose.UiLayoutConfig; break;
                case "rest-specification": purpose = PluginFilePurpose.RestSpecification; break;
                case "grpc-specification": purpose = PluginFilePurpose.GrpcSpecification; break;
                case "json-rpc-specification": purpose = PluginFilePurpose.JsonRpcSpecification; break;
                default: continue; // skip file in unknown base directory
            }

            manifest.Files.Add(new PluginFile
            {
                Path = relativePath,
                Sha1Hash = ComputeSha1Hash(fileInfo),
                Sha256Hash = ComputeSha256Hash(fileInfo),
                Purpose = purpose
            });
        }

        return manifest;

        static string? GetMetadataValue(XElement metadataElement, XName name, bool required)
        {
            if (metadataElement == null) throw new ArgumentNullException(nameof(metadataElement));
            if (name == null) throw new ArgumentNullException(nameof(name));

            var valueElement = metadataElement.Element(name);
            var value = valueElement?.Value;
            if (string.IsNullOrEmpty(value) && required)
            {
                throw new InvalidOperationException($"Missing required '<{name}>' element in package specification.");
            }
            return value;
        }

        static string ComputeSha256Hash(IFileInfo fileToCompute)
        {
            using var fileStream = fileToCompute.CreateReadStream();
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(fileStream);
            return Convert.ToBase64String(hash);
        }
        static string ComputeSha1Hash(IFileInfo fileToCompute)
        {
            using var fileStream = fileToCompute.CreateReadStream();
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(fileStream);
            return Convert.ToBase64String(hash);
        }
    }

    private static IEnumerable<(string relativePath, IFileInfo fileInfo)> EnumerateFilesRecursive(IFileProvider fileProvider, string path)
    {
        foreach (var fileInfo in fileProvider.GetDirectoryContents(path))
        {
            if (fileInfo.IsDirectory)
            {
                foreach (var subFileInfo in EnumerateFilesRecursive(fileProvider, path + fileInfo.Name + "/"))
                {
                    yield return subFileInfo;
                }
            }
            else
            {
                yield return (path + fileInfo.Name, fileInfo);
            }
        }
    }
}
