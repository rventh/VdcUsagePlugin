using Microsoft.Extensions.Logging;
using MyPlugin.Service.Settings;
using MyPlugin.Service.Vspc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyPlugin.Service.Usage;

/// <summary>Loads VDC usage and tenant names from the VSPC REST API and keeps them in a short-lived cache.</summary>
public sealed class UsageService
{
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(10);
    private const int PageSize = 100;
    private const int MaxPages = 200;

    private readonly VspcConnection _connection;
    private readonly MappingStore _mappingStore;
    private readonly ILogger<UsageService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private UsageDataset? _cache;
    private string _cacheFingerprint = string.Empty;

    public UsageService(VspcConnection connection, MappingStore mappingStore, ILogger<UsageService> logger)
    {
        _connection = connection;
        _mappingStore = mappingStore;
        _logger = logger;
    }

    public void Invalidate()
    {
        _cache = null;
    }

    public async Task<UsageDataset> GetDatasetAsync(bool force, CancellationToken ct)
    {
        var key = await _connection.ResolveApiKeyAsync(ct);
        if (key.Key == null)
        {
            throw new VspcApiException(409, "key_missing", "No API key is saved yet. Enter a VSPC API key on the Settings page.");
        }

        var fingerprint = key.Source + ":" + key.Hint;
        await _gate.WaitAsync(ct);
        try
        {
            if (!force && _cache != null && _cacheFingerprint == fingerprint && DateTimeOffset.UtcNow - _cache.LoadedAt < CacheTime)
            {
                return _cache;
            }

            var dataset = await LoadAsync(key.Key, ct);
            _cache = dataset;
            _cacheFingerprint = fingerprint;
            return dataset;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<UsageDataset> LoadAsync(string apiKey, CancellationToken ct)
    {
        var tenants = await LoadTenantsAsync(apiKey, ct);
        var companies = await LoadCompaniesAsync(apiKey, ct);
        var mappings = BuildMappings(tenants, companies);

        var rows = new List<UsageRow>();
        await foreach (var month in PagedAsync("pulse/monthlyUsages", apiKey, ct))
        {
            var monthText = Str(month, "month");
            if (!month.TryGetProperty("tenants", out var tenantArray) || tenantArray.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var tenant in tenantArray.EnumerateArray())
            {
                var uid = Str(tenant, "tenantUid");
                tenants.TryGetValue(uid, out var info);
                var name = info?.Name ?? uid;
                mappings.TryGetValue(uid, out var link);
                var mapped = link != null && link.Source != "none";

                if (!tenant.TryGetProperty("workloads", out var workloads) || workloads.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var workload in workloads.EnumerateArray())
                {
                    var code = Str(workload, "productCode");
                    var skuName = Str(workload, "productName");
                    var product = ProductCatalog.Resolve(skuName, code);
                    rows.Add(new UsageRow(
                        monthText,
                        uid,
                        name,
                        mapped,
                        code,
                        skuName,
                        product.Family,
                        product.Families,
                        product.Edition,
                        product.Label,
                        product.Cloud,
                        product.RegionType,
                        Str(workload, "unitType"),
                        Num(workload, "usedUnits"),
                        Num(workload, "totalUnits"),
                        Num(workload, "weight"),
                        Num(workload, "usedPoints"),
                        Num(workload, "minCommitQty"),
                        Str(workload, "location"),
                        link?.CompanyName ?? string.Empty,
                        link?.Source ?? "none"));
                }
            }
        }

        string? pulseLastUpdate = null;
        try
        {
            using var pulse = await _connection.GetJsonAsync("pulse", apiKey, ct);
            if (pulse.RootElement.TryGetProperty("data", out var data))
            {
                pulseLastUpdate = Str(data, "lastUpdateDate");
            }
        }
        catch (VspcApiException error)
        {
            _logger.LogInformation("Could not read the Pulse configuration: {Message}", error.Message);
        }

        _logger.LogInformation("Loaded {Rows} usage rows for {Tenants} tenants.", rows.Count, tenants.Count);

        return new UsageDataset
        {
            Rows = rows
                .OrderByDescending(r => r.Month, StringComparer.Ordinal)
                .ThenBy(r => r.TenantName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.SkuName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            LoadedAt = DateTimeOffset.UtcNow,
            PulseLastUpdate = pulseLastUpdate,
            Tenants = mappings.Values.OrderBy(m => m.TenantName, StringComparer.OrdinalIgnoreCase).ToList(),
            Companies = companies,
        };
    }

    private async Task<Dictionary<string, TenantInfo>> LoadTenantsAsync(string apiKey, CancellationToken ct)
    {
        var result = new Dictionary<string, TenantInfo>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await foreach (var tenant in PagedAsync("pulse/tenants", apiKey, ct))
            {
                var uid = Str(tenant, "instanceUid");
                if (uid.Length == 0)
                {
                    continue;
                }

                var name = Str(tenant, "name");
                var mappedTo = Str(tenant, "mappedMasterOrganizationUid");
                result[uid] = new TenantInfo(uid, name.Length > 0 ? name : uid, mappedTo);
            }
        }
        catch (VspcApiException error) when (error.Code is "forbidden" or "not_found")
        {
            // tenant names are a nice-to-have: fall back to showing tenant IDs
            _logger.LogWarning("Tenant names are not available: {Message}", error.Message);
        }

        return result;
    }

    private async Task<List<CompanyInfo>> LoadCompaniesAsync(string apiKey, CancellationToken ct)
    {
        var result = new List<CompanyInfo>();
        try
        {
            await foreach (var company in PagedAsync("organizations/companies", apiKey, ct))
            {
                var uid = Str(company, "instanceUid");
                var name = Str(company, "name");
                if (uid.Length > 0 && name.Length > 0)
                {
                    result.Add(new CompanyInfo(uid, name));
                }
            }
        }
        catch (VspcApiException error)
        {
            // company names are optional: without them the mapping page just has nothing to pick from
            _logger.LogWarning("VSPC companies are not available: {Message}", error.Message);
        }

        return result.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Works out each tenant's VSPC company: set in Pulse first, then picked by an admin here, then an exact (unique) name match.</summary>
    private Dictionary<string, TenantMapping> BuildMappings(Dictionary<string, TenantInfo> tenants, List<CompanyInfo> companies)
    {
        var byUid = companies.ToDictionary(c => c.Uid, StringComparer.OrdinalIgnoreCase);
        var byName = companies.GroupBy(c => NormalizeName(c.Name)).Where(g => g.Key.Length > 0).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var manual = _mappingStore.Load();
        var result = new Dictionary<string, TenantMapping>(StringComparer.OrdinalIgnoreCase);

        foreach (var tenant in tenants.Values)
        {
            TenantMapping link;
            if (tenant.PulseCompanyUid.Length > 0)
            {
                var name = byUid.TryGetValue(tenant.PulseCompanyUid, out var c) ? c.Name : tenant.PulseCompanyUid;
                link = new TenantMapping(tenant.Uid, tenant.Name, tenant.PulseCompanyUid, name, "pulse");
            }
            else if (manual.TryGetValue(tenant.Uid, out var chosen) && byUid.TryGetValue(chosen, out var picked))
            {
                link = new TenantMapping(tenant.Uid, tenant.Name, picked.Uid, picked.Name, "manual");
            }
            else if (byName.TryGetValue(NormalizeName(tenant.Name), out var same) && same.Count == 1)
            {
                link = new TenantMapping(tenant.Uid, tenant.Name, same[0].Uid, same[0].Name, "name");
            }
            else
            {
                link = new TenantMapping(tenant.Uid, tenant.Name, string.Empty, string.Empty, "none");
            }

            result[tenant.Uid] = link;
        }

        return result;
    }

    /// <summary>Lower case, letters and digits only, without a trailing company suffix, so "Acme Pty Ltd" and "ACME" compare equal.</summary>
    private static string NormalizeName(string name)
    {
        var words = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var ch in name ?? string.Empty)
        {
            if (char.IsLetterOrDigit(ch))
            {
                current.Append(char.ToLowerInvariant(ch));
            }
            else if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            words.Add(current.ToString());
        }

        var suffixes = new HashSet<string> { "pty", "ltd", "limited", "inc", "llc", "corp", "corporation", "co", "company", "gmbh", "plc" };
        while (words.Count > 1 && suffixes.Contains(words[words.Count - 1]))
        {
            words.RemoveAt(words.Count - 1);
        }

        return string.Concat(words);
    }

    /// <summary>Walks a paged VSPC collection (meta.pagingInfo + data[]) and yields each element of "data".</summary>
    private async IAsyncEnumerable<JsonElement> PagedAsync(
        string path,
        string apiKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var offset = 0;
        for (var page = 0; page < MaxPages; page++)
        {
            using var doc = await _connection.GetJsonAsync($"{path}?limit={PageSize}&offset={offset}", apiKey, ct);
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            var count = 0;
            foreach (var item in data.EnumerateArray())
            {
                count++;
                // clone so the element stays valid after the document is disposed
                yield return item.Clone();
            }

            offset += count;
            var total = -1;
            if (root.TryGetProperty("meta", out var meta)
                && meta.TryGetProperty("pagingInfo", out var paging)
                && paging.TryGetProperty("total", out var totalElement)
                && totalElement.ValueKind == JsonValueKind.Number)
            {
                total = totalElement.GetInt32();
            }

            if (count == 0 || (total >= 0 && offset >= total) || (total < 0 && count < PageSize))
            {
                yield break;
            }
        }
    }

    private static string Str(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
    }

    private static double Num(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
                ? value.GetDouble()
                : 0d;
    }
}
