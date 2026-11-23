// Copyright © Veeam Software Group GmbH.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace MyPlugin.Service.Manifest;

public static class PluginManifestServiceRegistration
{
    public static IServiceCollection AddManifest(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        services.AddTransient<IFileProvider>(_ => new PhysicalFileProvider(contentRootPath));
        services.AddTransient<PluginManifestService, PluginManifestService>();
        return services;
    }
}
