// Copyright © Veeam Software Group GmbH.

using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyPlugin.Service.Manifest;
using Veeam.AC.Service.OrganizationalStructure;
using Veeam.AC.Service.PluginManager;
using Veeam.AC.Service.ProductInformation.Grpc;
using Veeam.AC.Service.Users;

namespace MyPlugin.Service.Registration;

public class PluginRegistrationHostedService : IHostedService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly PluginRegistry.PluginRegistryClient _registryClient;
    private readonly PluginManifestService _pluginManifestService;
    private readonly ILogger<PluginRegistrationHostedService> _logger;
    private readonly CancellationTokenSource _stopCancellationSource;
    private bool _isRegistrationSkipped;

    public PluginRegistrationHostedService(
        IWebHostEnvironment webHostEnvironment,
        PluginRegistry.PluginRegistryClient registryClient,
        PluginManifestService pluginManifestService,
        ILogger<PluginRegistrationHostedService> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _registryClient = registryClient ?? throw new ArgumentNullException(nameof(registryClient));
        _pluginManifestService = pluginManifestService ?? throw new ArgumentNullException(nameof(pluginManifestService));
        _logger = logger;
        _stopCancellationSource = new CancellationTokenSource();
        _isRegistrationSkipped = false;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
#pragma warning disable CS4014
        StartRegistrationAsync();
#pragma warning restore CS4014

        return Task.CompletedTask;
    }

    private async Task StartRegistrationAsync()
    {
        await Task.Yield();

        var errorIsLogged = false;
        while (!_stopCancellationSource.IsCancellationRequested)
        {
            try
            {
                var registerRequest = new PluginRegisterRequest
                {
                    PluginId = _pluginManifestService.Current.PluginId,
                    Manifest = _pluginManifestService.Current,
                };

                await _registryClient.registerAsync(
                    registerRequest,
                    new CallOptions(cancellationToken: _stopCancellationSource.Token)
                ).ResponseAsync;
                
                _logger.LogInformation("Successfully registered plugin on VSPC service.");

                break;
            }
            catch (Exception error)
            {
                if (_webHostEnvironment.IsDevelopment())
                {
                    _isRegistrationSkipped = true;
                    return; // no VSPC Server is detected, stop connecting
                }

                if (!errorIsLogged)
                {
                    errorIsLogged = true;
                    _logger.LogInformation(error, "Failed to register plugin on VSPC service due error. Retrying in 5 seconds.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), _stopCancellationSource.Token);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _stopCancellationSource.Cancel();

        if (_isRegistrationSkipped)
        {
            return Task.CompletedTask;
        }

        var unregisterRequest = new PluginUnregisterRequest
        {
            PluginId = _pluginManifestService.Current.PluginId
        };
        return _registryClient.unregisterAsync(
            unregisterRequest,
            new CallOptions(cancellationToken: cancellationToken)
        ).ResponseAsync.IgnoreFaultOrCancellation();
    }
}
