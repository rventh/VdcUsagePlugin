using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyPlugin.Service.Usage;

/// <summary>Filters shared by the report, the dashboard and both exports. Repeated query keys mean "any of".</summary>
public sealed class UsageFilter
{
    public string From { get; private set; } = string.Empty;
    public string To { get; private set; } = string.Empty;
    public HashSet<string> Tenants { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Families { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Clouds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> RegionTypes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Locations { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Skus { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string Mapping { get; private set; } = string.Empty; // "", "mapped" or "unmapped"

    public static UsageFilter FromQuery(IQueryCollection query)
    {
        var filter = new UsageFilter
        {
            From = Single(query, "from"),
            To = Single(query, "to"),
            Mapping = Single(query, "mapping").ToLowerInvariant(),
        };
        Fill(filter.Tenants, query, "tenant");
        Fill(filter.Families, query, "family");
        Fill(filter.Clouds, query, "cloud");
        Fill(filter.RegionTypes, query, "regionType");
        Fill(filter.Locations, query, "location");
        Fill(filter.Skus, query, "sku");
        return filter;
    }

    private static string Single(IQueryCollection query, string key) => query[key].ToString().Trim();

    private static void Fill(HashSet<string> target, IQueryCollection query, string key)
    {
        foreach (var value in query[key])
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(value.Trim());
            }
        }
    }

    public bool Matches(UsageRow row)
    {
        // months are "yyyy-MM", so ordinal comparison orders them correctly
        if (From.Length > 0 && string.CompareOrdinal(row.Month, From) < 0) return false;
        if (To.Length > 0 && string.CompareOrdinal(row.Month, To) > 0) return false;
        if (Tenants.Count > 0 && !Tenants.Contains(row.TenantUid)) return false;
        if (Families.Count > 0 && !row.Families.Any(f => Families.Contains(f))) return false;
        if (Clouds.Count > 0 && !Clouds.Contains(row.Cloud)) return false;
        if (RegionTypes.Count > 0 && !RegionTypes.Contains(row.RegionType)) return false;
        if (Locations.Count > 0 && !Locations.Contains(row.Location)) return false;
        if (Skus.Count > 0 && !Skus.Contains(row.SkuName)) return false;
        if (Mapping == "mapped" && !row.TenantMapped) return false;
        if (Mapping == "unmapped" && row.TenantMapped) return false;
        return true;
    }

    public List<UsageRow> Apply(IEnumerable<UsageRow> rows) => rows.Where(Matches).ToList();

    /// <summary>Plain-language list of the active filters, used in the PDF header.</summary>
    public IReadOnlyList<string> Describe(IReadOnlyDictionary<string, string> tenantNames)
    {
        var lines = new List<string>();
        if (From.Length > 0 || To.Length > 0)
        {
            lines.Add($"Months: {(From.Length > 0 ? MonthFormat.Label(From) : "first")} to {(To.Length > 0 ? MonthFormat.Label(To) : "latest")}");
        }

        if (Tenants.Count > 0)
        {
            lines.Add("Tenants: " + string.Join(", ", Tenants.Select(uid => tenantNames.TryGetValue(uid, out var name) ? name : uid)));
        }

        if (Families.Count > 0) lines.Add("Product families: " + string.Join(", ", Families));
        if (Clouds.Count > 0) lines.Add("Cloud: " + string.Join(", ", Clouds));
        if (RegionTypes.Count > 0) lines.Add("Region type: " + string.Join(", ", RegionTypes));
        if (Locations.Count > 0) lines.Add("Locations: " + string.Join(", ", Locations));
        if (Skus.Count > 0) lines.Add($"Products (SKUs): " + string.Join(", ", Skus));
        if (Mapping.Length > 0) lines.Add("Tenants: " + Mapping + " only");
        if (lines.Count == 0)
        {
            lines.Add("Filters: none (all usage)");
        }

        return lines;
    }
}
