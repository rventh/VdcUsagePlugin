using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MyPlugin.Service.Export;
using MyPlugin.Service.Security;
using MyPlugin.Service.Settings;
using MyPlugin.Service.Usage;
using MyPlugin.Service.Vspc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyPlugin.Service;

public sealed record ApiKeyBody(string? ApiKey);
public sealed record MappingBody(string? TenantUid, string? CompanyUid);

public static class Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Handled outcomes are returned as HTTP 200 + { data, error }: a 401/403 from a plugin can sign the user out of VSPC.
    private static IResult Ok(object? data) => Results.Json(new { data, error = (object?)null }, Json);

    private static IResult Fail(string code, string message) =>
        Results.Json(new { data = (object?)null, error = new { code, message } }, Json);

    private static async Task<IResult> Guard(HttpContext ctx, UserAccess access, Func<AccessDecision, Task<IResult>> action)
    {
        try
        {
            var decision = await access.CheckAsync(ctx, ctx.RequestAborted);
            if (!decision.Allowed)
            {
                return Fail(decision.Code, decision.Message);
            }

            return await action(decision);
        }
        catch (VspcApiException error)
        {
            return Fail(error.Code, error.Message);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(499);
        }
        catch (Exception error)
        {
            var logger = (ILoggerFactory?)ctx.RequestServices.GetService(typeof(ILoggerFactory));
            logger?.CreateLogger("Endpoints").LogError(error, "Unhandled error while serving {Path}.", ctx.Request.Path);
            return Fail("server_error", "The plugin hit an unexpected error. Download the plugin logs for details.");
        }
    }

    public static void MapUsageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/settings", (HttpContext ctx, UserAccess access, VspcConnection connection, UsageService usage) =>
            Guard(ctx, access, async _ =>
            {
                var key = await connection.ResolveApiKeyAsync(ctx.RequestAborted);
                return Ok(new
                {
                    hasKey = key.Key != null,
                    source = key.Source,
                    hint = key.Hint,
                    savedAt = key.SavedAt,
                    savedBy = key.SavedBy,
                });
            }));

        app.MapPut("/api/v1/settings/apikey", (HttpContext ctx, ApiKeyBody body, UserAccess access, VspcConnection connection, ApiKeyStore store, UsageService usage) =>
            Guard(ctx, access, async decision =>
            {
                var apiKey = (body.ApiKey ?? string.Empty).Trim();
                if (apiKey.Length < 16 || apiKey.Length > 4096 || apiKey.Any(char.IsWhiteSpace))
                {
                    return Fail("invalid_key", "That does not look like a VSPC API key.");
                }

                // test the key before keeping it
                using (await connection.GetJsonAsync("pulse/tenants?limit=1", apiKey, ctx.RequestAborted)) { }

                store.Save(apiKey, decision.UserId);
                usage.Invalidate();
                return Ok(new { saved = true, hint = ApiKeyStore.Hint(apiKey) });
            }));

        app.MapDelete("/api/v1/settings/apikey", (HttpContext ctx, UserAccess access, ApiKeyStore store, UsageService usage) =>
            Guard(ctx, access, _ =>
            {
                store.Delete();
                usage.Invalidate();
                return Task.FromResult(Ok(new { removed = true }));
            }));

        app.MapGet("/api/v1/mapping", (HttpContext ctx, UserAccess access, UsageService usage) =>
            Guard(ctx, access, async _ =>
            {
                var ds = await usage.GetDatasetAsync(false, ctx.RequestAborted);
                return Ok(new
                {
                    companies = ds.Companies.Select(c => new { uid = c.Uid, name = c.Name }).ToList(),
                    tenants = ds.Tenants.Select(t => new { tenantUid = t.TenantUid, tenantName = t.TenantName, companyUid = t.CompanyUid, companyName = t.CompanyName, source = t.Source }).ToList(),
                });
            }));

        app.MapPut("/api/v1/mapping", (HttpContext ctx, MappingBody body, UserAccess access, UsageService usage, MappingStore store) =>
            Guard(ctx, access, async _ =>
            {
                var tenantUid = (body.TenantUid ?? string.Empty).Trim();
                var companyUid = (body.CompanyUid ?? string.Empty).Trim();
                var ds = await usage.GetDatasetAsync(false, ctx.RequestAborted);
                if (!ds.Tenants.Any(t => t.TenantUid.Equals(tenantUid, StringComparison.OrdinalIgnoreCase)))
                {
                    return Fail("invalid_mapping", "That tenant was not found in Pulse.");
                }

                if (companyUid.Length > 0 && !ds.Companies.Any(c => c.Uid.Equals(companyUid, StringComparison.OrdinalIgnoreCase)))
                {
                    return Fail("invalid_mapping", "That company was not found in VSPC.");
                }

                store.Set(tenantUid, companyUid);
                usage.Invalidate();
                return Ok(new { saved = true });
            }));

        app.MapPost("/api/v1/usage/refresh", (HttpContext ctx, UserAccess access, UsageService usage) =>
            Guard(ctx, access, async _ =>
            {
                var ds = await usage.GetDatasetAsync(true, ctx.RequestAborted);
                return Ok(new { loadedAt = ds.LoadedAt, rows = ds.Rows.Count });
            }));

        app.MapGet("/api/v1/usage/options", (HttpContext ctx, UserAccess access, UsageService usage) =>
            Guard(ctx, access, async _ =>
            {
                var ds = await usage.GetDatasetAsync(false, ctx.RequestAborted);
                var rows = ds.Rows;
                return Ok(new
                {
                    loadedAt = ds.LoadedAt,
                    pulseLastUpdate = ds.PulseLastUpdate,
                    months = rows.Select(r => r.Month).Distinct().OrderBy(m => m, StringComparer.Ordinal).ToList(),
                    tenants = rows.GroupBy(r => r.TenantUid).Select(g => new { uid = g.Key, name = g.First().TenantName, mapped = g.First().TenantMapped })
                        .OrderBy(t => t.name, StringComparer.OrdinalIgnoreCase).ToList(),
                    families = ProductCatalog.Families,
                    clouds = Distinct(rows.Select(r => r.Cloud)),
                    regionTypes = Distinct(rows.Select(r => r.RegionType)),
                    locations = Distinct(rows.Select(r => r.Location)),
                    skus = rows.GroupBy(r => r.SkuName)
                        .Select(g => new { name = g.Key, label = g.First().SkuLabel, families = g.First().Families, inData = true })
                        .OrderBy(x => x.label, StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                });
            }));

        app.MapGet("/api/v1/usage/rows", (HttpContext ctx, UserAccess access, UsageService usage) =>
            Guard(ctx, access, async _ =>
            {
                var ds = await usage.GetDatasetAsync(false, ctx.RequestAborted);
                var filtered = UsageFilter.FromQuery(ctx.Request.Query).Apply(ds.Rows);
                filtered = Sort(filtered, ctx.Request.Query["sort"].ToString(), ctx.Request.Query["dir"].ToString());

                var offset = Math.Max(0, ParseInt(ctx.Request.Query["offset"], 0));
                var limit = Math.Clamp(ParseInt(ctx.Request.Query["limit"], 50), 1, 500);
                return Ok(new
                {
                    total = filtered.Count,
                    totalPoints = Math.Round(filtered.Sum(r => r.Points), 2),
                    loadedAt = ds.LoadedAt,
                    rows = filtered.Skip(offset).Take(limit).ToList(),
                });
            }));

        app.MapGet("/api/v1/usage/dashboard", (HttpContext ctx, UserAccess access, UsageService usage) =>
            Guard(ctx, access, async _ =>
            {
                var ds = await usage.GetDatasetAsync(false, ctx.RequestAborted);
                var filtered = UsageFilter.FromQuery(ctx.Request.Query).Apply(ds.Rows);
                return Ok(DashboardBuilder.Build(filtered));
            }));

        app.MapGet("/api/v1/usage/export/csv", (HttpContext ctx, UserAccess access, UsageService usage) =>
            Guard(ctx, access, async _ =>
            {
                var ds = await usage.GetDatasetAsync(false, ctx.RequestAborted);
                var filtered = UsageFilter.FromQuery(ctx.Request.Query).Apply(ds.Rows);
                return Ok(FilePayload(CsvExport.Build(filtered), "text/csv", $"vdc-usage-{DateTime.UtcNow:yyyyMMdd}.csv"));
            }));

        app.MapGet("/api/v1/usage/export/pdf", (HttpContext ctx, UserAccess access, UsageService usage) =>
            Guard(ctx, access, async decision =>
            {
                var ds = await usage.GetDatasetAsync(false, ctx.RequestAborted);
                var filter = UsageFilter.FromQuery(ctx.Request.Query);
                var filtered = filter.Apply(ds.Rows);
                var names = ds.Rows.GroupBy(r => r.TenantUid).ToDictionary(g => g.Key, g => g.First().TenantName, StringComparer.OrdinalIgnoreCase);
                var pdf = PdfExport.Build(filtered, filter.Describe(names), decision.UserId);
                return Ok(FilePayload(pdf, "application/pdf", $"vdc-usage-{DateTime.UtcNow:yyyyMMdd}.pdf"));
            }));
    }

    // exports travel as JSON so the UI can send them through VSPC's authenticated transport and save them in the browser
    private static object FilePayload(byte[] bytes, string contentType, string fileName) =>
        new { fileName, contentType, base64 = Convert.ToBase64String(bytes) };

    private static List<string> Distinct(IEnumerable<string> values) =>
        values.Where(v => !string.IsNullOrEmpty(v)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();

    private static int ParseInt(string? text, int fallback) => int.TryParse(text, out var v) ? v : fallback;

    private static List<UsageRow> Sort(List<UsageRow> rows, string sort, string dir)
    {
        var desc = !string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
        Func<UsageRow, object> key = (sort ?? string.Empty).ToLowerInvariant() switch
        {
            "tenant" => r => r.TenantName.ToLowerInvariant(),
            "product" => r => r.SkuName.ToLowerInvariant(),
            "family" => r => r.Family,
            "units" => r => r.UsedUnits,
            "ppu" => r => r.Ppu,
            "points" => r => r.Points,
            _ => r => r.Month,
        };
        return desc ? rows.OrderByDescending(key).ToList() : rows.OrderBy(key).ToList();
    }
}
