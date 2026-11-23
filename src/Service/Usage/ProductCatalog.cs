using System;
using System.Linq;

namespace MyPlugin.Service.Usage;

/// <summary>What a Pulse SKU name tells us, split into the parts people filter and read by.</summary>
public sealed record ProductInfo(string Family, string[] Families, string Edition, string Cloud, string RegionType, string Label);

/// <summary>
/// Reads Veeam Data Cloud SKU names (for example "Veeam Data Cloud Vault Advanced for Azure 1 TB Non-Core Region Data Center")
/// and splits them into product family, edition, cloud and Core/Non-Core.
/// Bundles belong to every family they include: Advanced and Premium also include Entra ID, and the Plus bundles also include Salesforce.
/// Reference: helpcenter.veeam.com/docs/vdc/provider/saas_license_consumption.html
/// </summary>
public static class ProductCatalog
{
    public const string M365 = "Microsoft 365";
    public const string Entra = "Microsoft Entra ID";
    public const string Salesforce = "Salesforce";
    public const string Azure = "Microsoft Azure";
    public const string Vault = "Vault";
    public const string Other = "Other";

    public static readonly string[] Families = { M365, Entra, Salesforce, Azure, Vault };

    private static readonly string[] M365Editions = { "Premium Plus", "Advanced Plus", "Premium", "Advanced", "Foundation", "Express" };

    private static bool Has(string text, string part) => text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

    public static ProductInfo Resolve(string skuName, string skuCode)
    {
        var name = skuName ?? string.Empty;
        var code = skuCode ?? string.Empty;

        if (Has(name, "Vault"))
        {
            var edition = Has(name, "Archive Retrieval") ? "Archive Retrieval"
                : Has(name, "Archive") ? "Archive"
                : Has(name, "Advanced") ? "Advanced"
                : Has(name, "Foundation") ? "Foundation"
                : string.Empty;
            var cloud = Has(name, "AWS") ? "AWS" : Has(name, "Azure") ? "Azure" : string.Empty;
            var region = Has(name, "Non-Core") || Has(name, "Non Core") ? "Non-Core"
                : Has(name, "Core") ? "Core"
                : code.EndsWith("-NR", StringComparison.OrdinalIgnoreCase) ? "Non-Core"
                : code.EndsWith("-CR", StringComparison.OrdinalIgnoreCase) ? "Core"
                : string.Empty;
            return Make(Vault, new[] { Vault }, edition, cloud, region);
        }

        if (Has(name, "Microsoft 365"))
        {
            var tail = name.Substring(name.IndexOf("Microsoft 365", StringComparison.OrdinalIgnoreCase) + "Microsoft 365".Length);
            var edition = M365Editions.FirstOrDefault(e => Has(tail, e)) ?? string.Empty;
            var families = new System.Collections.Generic.List<string> { M365 };
            if (edition is "Advanced" or "Advanced Plus" or "Premium" or "Premium Plus" || Has(tail, "Entra"))
            {
                families.Add(Entra);
            }

            if (edition is "Advanced Plus" or "Premium Plus" || Has(tail, "Salesforce"))
            {
                families.Add(Salesforce);
            }

            return Make(M365, families.ToArray(), edition, string.Empty, string.Empty);
        }

        if (Has(name, "Entra"))
        {
            return Make(Entra, new[] { Entra }, string.Empty, string.Empty, string.Empty);
        }

        if (Has(name, "Salesforce"))
        {
            return Make(Salesforce, new[] { Salesforce }, Has(name, "Advanced") ? "Advanced" : string.Empty, string.Empty, string.Empty);
        }

        if (Has(name, "Azure"))
        {
            var edition = Has(name, "Advanced") ? "Advanced" : Has(name, "Foundation") ? "Foundation" : string.Empty;
            return Make(Azure, new[] { Azure }, edition, "Azure", string.Empty);
        }

        return Make(Other, new[] { Other }, string.Empty, string.Empty, string.Empty);
    }

    private static ProductInfo Make(string family, string[] families, string edition, string cloud, string region)
    {
        var label = string.Join(" · ", new[] { family, edition, cloud, region }.Where(p => p.Length > 0));
        return new ProductInfo(family, families, edition, cloud, region, label);
    }
}
