// Copyright © Veeam Software Group GmbH.

using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyPlugin.Service.Registration;

public static class PluginRegistrationHostedServiceRegistration
{
    public static IServiceCollection AddStartupRegistration(this IServiceCollection services, IWebHostEnvironment webHostEnvironment)
    {
        services.AddTransient<IHostedService, PluginRegistrationHostedService>();

        return services;
    }
}
