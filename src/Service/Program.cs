// Copyright © Veeam Software Group GmbH.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MyPlugin.Service;
using MyPlugin.Service.Handlers;
using MyPlugin.Service.Manifest;
using MyPlugin.Service.Registration;
using MyPlugin.Service.Security;
using MyPlugin.Service.Settings;
using MyPlugin.Service.Usage;
using MyPlugin.Service.Vspc;
using Microsoft.AspNetCore.DataProtection;
using NReco.Logging.File;
using Veeam.AC.Service.PluginManager;

var builder = WebApplication.CreateBuilder(args);
var registryAddress = builder.Configuration.GetValue("GRPC_PLUGIN_REGISTRY_ADDRESS", string.Empty); // like "http://localhost:5017/"
var registryPort = builder.Configuration.GetValue("GRPC_PLUGIN_REGISTRY_PORT", 5017);

if (registryAddress == null || string.IsNullOrEmpty(registryAddress))
{
    registryAddress = $"http://localhost:{registryPort}";
}


var contentRootPath = builder.Environment.ContentRootPath = AppContext.BaseDirectory;
if (builder.Environment.IsDevelopment())
{
    contentRootPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../../../.."));
}
else
{
    // The service exe is installed at <package root>/content/any/any/service/, but the manifest, the UI files and the
    // .nuspec are found from the package root. Walk up from the exe folder until the package root is found.
    var probe = new DirectoryInfo(AppContext.BaseDirectory);
    while (probe != null
        && !(File.Exists(Path.Combine(probe.FullName, "content", "any", "any", "plugin.manifest.xml"))
             && probe.GetFiles("*.nuspec").Length > 0))
    {
        probe = probe.Parent;
    }

    if (probe != null)
    {
        contentRootPath = probe.FullName;
    }
}

builder.Services
    .AddGrpcClient<PluginRegistry.PluginRegistryClient>(options => options.Address = new Uri(registryAddress));

var dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName("VdcUsagePlugin")
    .PersistKeysToFileSystem(new DirectoryInfo(PluginPaths.KeyRingDirectory));
if (OperatingSystem.IsWindows())
{
    // encrypt the key ring at rest with Windows DPAPI (machine scope, because the service runs as a system account)
    dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
}

builder.Services.AddSingleton<ApiKeyStore>();
builder.Services.AddSingleton<MappingStore>();
builder.Services.AddSingleton<VspcConnection>();
builder.Services.AddSingleton<UserAccess>();
builder.Services.AddSingleton<UsageService>();

builder.Services
    .AddStartupRegistration(builder.Environment)
    .AddManifest(builder.Configuration, contentRootPath);

builder.Services.AddLogging(loggingBuilder => {
    loggingBuilder.AddFile(Path.Combine(DownloadLogsHandler.ApplicationLogDirectory, string.Format("app_{0:yyyy}-{0:MM}-{0:dd}.log", DateTime.UtcNow)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGet("/api/v1/healthcheck", () => "OK");
app.MapGet("/api/v1/readiness", () => "READY");
app.MapGet("/api/v1/logs", DownloadLogsHandler.InvokeAsync);
app.MapDelete("/api/v1/localdata", PurgeLocalDataHandler.InvokeAsync);
app.MapUsageEndpoints();

var staticFileOptions = default(StaticFileOptions);

// Serve Plugin's files from "content" folder at root "/content/"
// these files are declared in plugin's manifest and requested by VSPC

app.UseStaticFiles(new StaticFileOptions
{
	RequestPath = "/content",
	FileProvider = new PhysicalFileProvider(Path.Combine(contentRootPath, "content")),
	ContentTypeProvider = new StaticFileExtensionContentTypeProvider()
});

// Serve UI-related content from "content/any/any/ui-content" at root "/"

app.UseStaticFiles(staticFileOptions = new StaticFileOptions
{
	RequestPath = "",
	FileProvider = new PhysicalFileProvider(Path.Combine(contentRootPath, "content", "any", "any", "ui-content")),
	ContentTypeProvider = new StaticFileExtensionContentTypeProvider()
});

// Redirect all unknown requests to /index.html

app.UseSpa(spaBuilder =>
{
    spaBuilder.Options.DefaultPage = "/index.html";
    spaBuilder.Options.DefaultPageStaticFileOptions = staticFileOptions;
});

app.Run();