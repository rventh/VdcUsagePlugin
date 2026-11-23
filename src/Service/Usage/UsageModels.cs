using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MyPlugin.Service.Usage;

/// <summary>One line of VDC usage: a tenant's workload (SKU) in one month.</summary>
public sealed record UsageRow(
    string Month,
    string TenantUid,
    string TenantName,
    bool TenantMapped,
    string SkuCode,
    string SkuName,
    string Family,
    string[] Families,
    string Edition,
    string SkuLabel,
    string Cloud,
    string RegionType,
    string UnitType,
    double UsedUnits,
    double TotalUnits,
    double Ppu,
    double Points,
    double MinCommitQty,
    string Location,
    string CompanyName,
    string MapSource);

public sealed record TenantInfo(string Uid, string Name, string PulseCompanyUid);

public sealed record CompanyInfo(string Uid, string Name);

/// <summary>How a Pulse tenant is tied to a VSPC company. Source is pulse (set in Pulse), manual (set in this plugin), name (exact name match) or none.</summary>
public sealed record TenantMapping(string TenantUid, string TenantName, string CompanyUid, string CompanyName, string Source);

public sealed record SkuInfo(string Code, string Name, string Family);

public sealed class UsageDataset
{
    public IReadOnlyList<UsageRow> Rows { get; init; } = Array.Empty<UsageRow>();
    public DateTimeOffset LoadedAt { get; init; }
    public string? PulseLastUpdate { get; init; }
    public IReadOnlyList<TenantMapping> Tenants { get; init; } = Array.Empty<TenantMapping>();
    public IReadOnlyList<CompanyInfo> Companies { get; init; } = Array.Empty<CompanyInfo>();
}

/// <summary>Works out the product family, Microsoft 365 plan, cloud and region type from a SKU's name and code.</summary>
public static class ProductClassifier
{
    private static bool Has(string text, string part) => text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

    public static string Family(string name)
    {
        if (Has(name, "Vault"))
        {
            if (Has(name, "Archive")) return "Vault Archive";
            if (Has(name, "Advanced")) return "Vault Advanced";
            if (Has(name, "Foundation")) return "Vault Foundation";
            return "Vault";
        }

        if (Has(name, "Microsoft 365")) return "Microsoft 365";
        if (Has(name, "Entra")) return "Microsoft Entra ID";
        if (Has(name, "Salesforce")) return "Salesforce";
        if (Has(name, "Azure")) return "Azure";
        return "Other";
    }

    public static string M365Plan(string name)
    {
        const string marker = "Microsoft 365";
        var index = name.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return string.Empty;
        }

        var tail = name.Substring(index + marker.Length);
        foreach (var plan in new[] { "Premium Plus", "Advanced Plus", "Premium", "Advanced", "Foundation", "Express" })
        {
            if (Has(tail, plan))
            {
                return plan;
            }
        }

        return string.Empty;
    }

    public static string Cloud(string name)
    {
        if (Has(name, "AWS")) return "AWS";
        if (Has(name, "Azure")) return "Azure";
        return string.Empty;
    }

    /// <summary>Core and Non-Core only exist for Vault products.</summary>
    public static string RegionType(string name, string code)
    {
        if (!Has(name, "Vault"))
        {
            return string.Empty;
        }

        if (Has(name, "Non-Core") || Has(name, "Non Core")) return "Non-Core";
        if (Has(name, "Core")) return "Core";
        if (code.EndsWith("-NR", StringComparison.OrdinalIgnoreCase)) return "Non-Core";
        if (code.EndsWith("-CR", StringComparison.OrdinalIgnoreCase)) return "Core";
        return string.Empty;
    }
}

public static class MonthFormat
{
    private static readonly string[] Names = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    /// <summary>"2026-10" becomes "Oct 2026"; anything unexpected is returned unchanged.</summary>
    public static string Label(string month)
    {
        var parts = (month ?? string.Empty).Split('-');
        return parts.Length == 2 && int.TryParse(parts[1], out var m) && m >= 1 && m <= 12 ? $"{Names[m - 1]} {parts[0]}" : month ?? string.Empty;
    }
}

public static class UnitFormat
{
    private static string Num(double v) => v.ToString("#,0.##", CultureInfo.InvariantCulture);

    /// <summary>"5" with unit "1TB" becomes "5 TB", and "17" with unit "User" becomes "17 users".</summary>
    public static string Label(double used, string unitType)
    {
        var unit = (unitType ?? string.Empty).Trim();
        var match = Regex.Match(unit, @"^(\d+(?:\.\d+)?)\s*TB$", RegexOptions.IgnoreCase);
        if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
        {
            return Num(used * size) + " TB";
        }

        if (unit.Equals("User", StringComparison.OrdinalIgnoreCase))
        {
            return Num(used) + (Math.Abs(used - 1) < 1e-9 ? " user" : " users");
        }

        return unit.Length == 0 ? Num(used) : Num(used) + " " + unit;
    }
}
