using MyPlugin.Service.Usage;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;

namespace MyPlugin.Service.Export;

public static class CsvExport
{
    public static byte[] Build(IReadOnlyList<UsageRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("Month,Tenant,Tenant ID,VSPC company,Product,Edition,Cloud,Region type,Also counts as,SKU code,SKU name,Unit,Used units,Total units,PPU,Points used,Min commit,Location\r\n");
        foreach (var r in rows)
        {
            sb.Append(string.Join(",", new[]
            {
                Text(MonthFormat.Label(r.Month)), Text(r.TenantName), Text(r.TenantUid), Text(r.CompanyName), Text(r.Family), Text(r.Edition), Text(r.Cloud), Text(r.RegionType),
                Text(string.Join("; ", r.Families.Where(f => f != r.Family))), Text(r.SkuCode), Text(r.SkuName), Text(r.UnitType),
                Num(r.UsedUnits), Num(r.TotalUnits), Num(r.Ppu), Num(r.Points), Num(r.MinCommitQty), Text(r.Location),
            }));
            sb.Append("\r\n");
        }

        var preamble = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result, 0);
        body.CopyTo(result, preamble.Length);
        return result;
    }

    private static string Num(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>Quotes a text cell and neutralises spreadsheet formulas (=, +, -, @) coming from tenant or product names.</summary>
    private static string Text(string value)
    {
        value ??= string.Empty;
        if (value.Length > 0 && (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'))
        {
            value = "'" + value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
