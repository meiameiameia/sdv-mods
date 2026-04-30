using System.Globalization;
using System.Text;
using PowerGrid.Balancer.Core;

internal static class ResourcePressureComparisonReport
{
    public static void Write(
        string outputPath,
        IReadOnlyDictionary<string, BalanceConfig> configs,
        IReadOnlyList<LoadoutPlan> loadouts)
    {
        Directory.CreateDirectory(outputPath);
        IReadOnlyList<ResourceComparisonRow> rows = Analyze(configs, loadouts);

        File.WriteAllText(Path.Combine(outputPath, "resource-pressure-comparison.md"), RenderMarkdown(rows));
        File.WriteAllText(Path.Combine(outputPath, "resource-pressure-comparison.csv"), RenderCsv(rows));
    }

    private static IReadOnlyList<ResourceComparisonRow> Analyze(
        IReadOnlyDictionary<string, BalanceConfig> configs,
        IReadOnlyList<LoadoutPlan> loadouts)
    {
        List<ResourceComparisonRow> rows = new();

        foreach ((string configName, BalanceConfig config) in configs)
        {
            IReadOnlyList<ResourcePressureReport.ResourcePressure> pressure = ResourcePressureReport.Analyze(config, loadouts);
            foreach (ResourcePressureReport.ResourcePressure resource in pressure.Where(resource => resource.TotalGap > 0))
            {
                rows.Add(new ResourceComparisonRow(
                    configName,
                    resource.Resource,
                    resource.Signal,
                    resource.TotalNeeded,
                    resource.TotalStockpile,
                    resource.TotalGap,
                    resource.PrimarySource,
                    resource.PrimarySourceAmount,
                    resource.BlockedLoadoutCount,
                    resource.TotalLoadouts,
                    resource.WorstLoadout));
            }
        }

        return rows
            .OrderBy(row => row.Config, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(row => row.TotalGap)
            .ThenBy(row => row.Resource, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string RenderMarkdown(IReadOnlyList<ResourceComparisonRow> rows)
    {
        List<string> lines = new()
        {
            "# Resource Pressure Comparison",
            "",
            "Compares resource bottlenecks across candidate balance configs using the same loadout suite.",
            "",
            "| Config | Resource | Signal | Needed | Gap | Main source | Blocked | Worst loadout |",
            "| --- | --- | --- | ---: | ---: | --- | ---: | --- |"
        };

        foreach (ResourceComparisonRow row in rows)
        {
            lines.Add($"| {EscapeMarkdown(row.Config)} | {EscapeMarkdown(row.Resource)} | {row.Signal} | {row.TotalNeeded} | {row.TotalGap} | {EscapeMarkdown(row.PrimarySource)} | {row.BlockedLoadoutCount}/{row.TotalLoadouts} | {EscapeMarkdown(row.WorstLoadout)} |");
        }

        lines.Add("");
        lines.Add("## Fuel Pressure Snapshot");
        lines.Add("");
        lines.Add("| Config | Sap gap | Coal gap |");
        lines.Add("| --- | ---: | ---: |");

        foreach (string config in rows.Select(row => row.Config).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            int sapGap = rows.FirstOrDefault(row => row.Config == config && row.Resource.Equals("Sap", StringComparison.OrdinalIgnoreCase))?.TotalGap ?? 0;
            int coalGap = rows.FirstOrDefault(row => row.Config == config && row.Resource.Equals("Coal", StringComparison.OrdinalIgnoreCase))?.TotalGap ?? 0;
            lines.Add($"| {EscapeMarkdown(config)} | {sapGap} | {coalGap} |");
        }

        lines.Add("");
        lines.Add("Use this report to pick candidates for in-game testing. It is a pressure comparison, not proof that a config feels good.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderCsv(IReadOnlyList<ResourceComparisonRow> rows)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Config", "Resource", "Signal", "TotalNeeded", "TotalStockpile", "TotalGap", "PrimarySource", "PrimarySourceAmount", "BlockedLoadouts", "TotalLoadouts", "WorstLoadout"));

        foreach (ResourceComparisonRow row in rows)
        {
            csv.AppendLine(CsvLine(
                row.Config,
                row.Resource,
                row.Signal,
                row.TotalNeeded,
                row.TotalStockpile,
                row.TotalGap,
                row.PrimarySource,
                row.PrimarySourceAmount,
                row.BlockedLoadoutCount,
                row.TotalLoadouts,
                row.WorstLoadout));
        }

        return csv.ToString();
    }

    private static string CsvLine(params object?[] values)
    {
        return string.Join(",", values.Select(CsvValue));
    }

    private static string CsvValue(object? value)
    {
        string text = value switch
        {
            null => "",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
            _ => value.ToString() ?? ""
        };

        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
            return $"\"{text.Replace("\"", "\"\"")}\"";

        return text;
    }

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private sealed record ResourceComparisonRow(
        string Config,
        string Resource,
        string Signal,
        int TotalNeeded,
        int TotalStockpile,
        int TotalGap,
        string PrimarySource,
        int PrimarySourceAmount,
        int BlockedLoadoutCount,
        int TotalLoadouts,
        string WorstLoadout);
}
