using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyPlugin.Service.Manifest;
using MyPlugin.Service.Settings;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Veeam.AC.Service.PluginManager;

namespace MyPlugin.Service.Vspc;

public sealed record ServiceLocation(string Endpoint, string TrustedCertHash);

/// <param name="Key">The API key, or null when none is available.</param>
/// <param name="Source">"user" (entered in the plugin settings) or "none".</param>
public sealed record ResolvedKey(string? Key, string Source, string Hint, DateTimeOffset? SavedAt, string? SavedBy);

/// <summary>Finds VSPC services through the plugin registry and calls the VSPC REST API.</summary>
public sealed class VspcConnection : IDisposable
{
    private readonly PluginRegistry.PluginRegistryClient _registry;
    private readonly PluginManifestService _manifest;
    private readonly ApiKeyStore _store;
    private readonly IConfiguration _config;
    private readonly ILogger<VspcConnection> _logger;
    private readonly ConcurrentDictionary<string, HttpClient> _restClients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, GrpcChannel> _channels = new(StringComparer.OrdinalIgnoreCase);

    public VspcConnection(
        PluginRegistry.PluginRegistryClient registry,
        PluginManifestService manifest,
        ApiKeyStore store,
        IConfiguration config,
        ILogger<VspcConnection> logger)
    {
        _registry = registry;
        _manifest = manifest;
        _store = store;
        _config = config;
        _logger = logger;
    }

    public string PluginIdText => _manifest.PluginId.ToString();

    public async Task<ServiceLocation> DiscoverAsync(VspcServiceName service, CancellationToken ct)
    {
        if (service == VspcServiceName.Rest)
        {
            // optional override for local development, e.g. "https://localhost:1280/api/v3/"
            var overrideUrl = _config["VSPC_REST_BASE_URL"];
            if (!string.IsNullOrWhiteSpace(overrideUrl))
            {
                return new ServiceLocation(overrideUrl, string.Empty);
            }
        }

        var reply = await _registry.discoverServiceAsync(
            new PluginDiscoverServiceRequest { Service = service, PluginId = PluginIdText },
            cancellationToken: ct);
        return new ServiceLocation(reply.ServiceEndpoint, reply.TrustedTlsCertificateHash ?? string.Empty);
    }

    /// <summary>Accepts the certificate if it validates normally, or if it matches the SHA-1 hash VSPC told us to trust.</summary>
    public static HttpMessageHandler CreateHandler(string trustedCertHash)
    {
        var trusted = (trustedCertHash ?? string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty);
        return new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                {
                    if (errors == SslPolicyErrors.None)
                    {
                        return true;
                    }

                    if (certificate == null || trusted.Length == 0)
                    {
                        return false;
                    }

                    using var cert = new X509Certificate2(certificate);
                    return string.Equals(cert.GetCertHashString(), trusted, StringComparison.OrdinalIgnoreCase);
                },
            },
        };
    }

    public GrpcChannel GetChannel(ServiceLocation location)
    {
        return _channels.GetOrAdd(location.Endpoint, endpoint => GrpcChannel.ForAddress(endpoint, new GrpcChannelOptions
        {
            HttpHandler = CreateHandler(location.TrustedCertHash),
        }));
    }

    private static string NormalizeRestBase(string endpoint)
    {
        var uri = new Uri(endpoint);
        var text = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        if (text.IndexOf("/api/v", StringComparison.OrdinalIgnoreCase) < 0)
        {
            text += "/api/v3";
        }

        return text + "/";
    }

    /// <summary>Returns the key an administrator saved in the plugin settings. The plugin does not use any other key.</summary>
    public Task<ResolvedKey> ResolveApiKeyAsync(CancellationToken ct)
    {
        var stored = _store.Load();
        var key = stored != null
            ? new ResolvedKey(stored.ApiKey, "user", ApiKeyStore.Hint(stored.ApiKey), stored.SavedAt, stored.SavedBy)
            : new ResolvedKey(null, "none", string.Empty, null, null);
        return Task.FromResult(key);
    }

    /// <summary>GET a VSPC REST API path such as "pulse/monthlyUsages?limit=50". The caller owns the returned document.</summary>
    public async Task<JsonDocument> GetJsonAsync(string relativeUrl, string apiKey, CancellationToken ct)
    {
        ServiceLocation location;
        try
        {
            location = await DiscoverAsync(VspcServiceName.Rest, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw new VspcApiException(502, "unreachable", "The plugin could not locate the VSPC REST service.", error);
        }

        var baseUrl = NormalizeRestBase(location.Endpoint);
        var client = _restClients.GetOrAdd(
            baseUrl + "|" + location.TrustedCertHash,
            _ => new HttpClient(CreateHandler(location.TrustedCertHash), disposeHandler: true) { Timeout = TimeSpan.FromSeconds(90) });

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUrl + relativeUrl));
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            throw new VspcApiException(502, "unreachable", "The VSPC REST API could not be reached.", error);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                _logger.LogWarning("VSPC REST call {Path} returned HTTP {Status}.", relativeUrl.Split('?')[0], status);
                throw status switch
                {
                    401 => new VspcApiException(status, "unauthorized", "VSPC rejected the API key (invalid, expired or revoked)."),
                    403 => new VspcApiException(status, "forbidden", "The API key is not allowed to read this data. Use a key with access to Pulse usage."),
                    404 => new VspcApiException(status, "not_found", "This VSPC version does not provide the requested data."),
                    _ => new VspcApiException(status, "upstream_error", $"VSPC returned HTTP {status}."),
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
    }

    public void Dispose()
    {
        foreach (var client in _restClients.Values)
        {
            client.Dispose();
        }

        foreach (var channel in _channels.Values)
        {
            channel.Dispose();
        }
    }
}
