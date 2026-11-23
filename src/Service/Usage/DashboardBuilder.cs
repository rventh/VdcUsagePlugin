using System;
using System.Collections.Generic;
using System.Linq;

namespace MyPlugin.Service.Usage;

public static class DashboardBuilder
{
    private static double R(double v) => Math.Round(v, 2);

    public static object Build(IReadOnlyList<UsageRow> rows)
    {
        var months = rows.Select(r => r.Month).Distinct(StringComparer.Ordinal).OrderBy(m => m, StringComparer.Ordinal).ToList();
        var total = rows.Sum(r => r.Points);

        var byMonth = months.Select(m => new { month = m, points = R(rows.Where(r => r.Month == m).Sum(r => r.Points)) }).ToList();

        var tenantTotals = rows
            .GroupBy(r => r.TenantUid)
            .Select(g => new
            {
                tenantUid = g.Key,
                tenantName = g.First().TenantName,
                points = R(g.Sum(r => r.Points)),
            })
            .OrderByDescending(t => t.points)
            .ToList();

        var topTenantUids = tenantTotals.Take(8).Select(t => t.tenantUid).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // per-tenant trend: one series per top tenant, one value per month
        var tenantTrend = tenantTotals.Take(8).Select(t => new
        {
            t.tenantUid,
            t.tenantName,
            values = months.Select(m => R(rows.Where(r => r.TenantUid == t.tenantUid && r.Month == m).Sum(r => r.Points))).ToList(),
        }).ToList();

        var familyTotals = rows
            .GroupBy(r => r.Family)
            .Select(g => new { family = g.Key, points = R(g.Sum(r => r.Points)) })
            .OrderByDescending(f => f.points)
            .ToList();

        var skuTotals = rows
            .GroupBy(r => r.SkuCode)
            .Select(g => new { skuCode = g.Key, skuName = g.First().SkuName, family = g.First().Family, points = R(g.Sum(r => r.Points)) })
            .OrderByDescending(s => s.points)
            .ToList();

        // per-tenant product split (by SKU) for the top tenants
        var tenantProducts = tenantTotals.Take(8).Select(t => new
        {
            t.tenantUid,
            t.tenantName,
            items = rows.Where(r => r.TenantUid == t.tenantUid)
                .GroupBy(r => r.SkuName)
                .Select(g => new { skuName = g.Key, points = R(g.Sum(r => r.Points)) })
                .OrderByDescending(i => i.points)
                .ToList(),
        }).ToList();

        // latest month vs previous month
        var latest = months.LastOrDefault();
        var previous = months.Count > 1 ? months[^2] : null;
        var latestPoints = latest == null ? 0 : rows.Where(r => r.Month == latest).Sum(r => r.Points);
        var previousPoints = previous == null ? (double?)null : rows.Where(r => r.Month == previous).Sum(r => r.Points);

        return new
        {
            totalPoints = R(total),
            latestMonth = latest,
            latestPoints = R(latestPoints),
            previousMonth = previous,
            previousPoints = previousPoints.HasValue ? R(previousPoints.Value) : (double?)null,
            tenantCount = tenantTotals.Count,
            skuCount = skuTotals.Count,
            months,
            byMonth,
            tenantTotals,
            tenantTrend,
            familyTotals,
            skuTotals,
            tenantProducts,
        };
    }
}
