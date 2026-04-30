using System.Globalization;
using System.Text;
using PowerGrid.Balancer.Core;

internal static class ResourceSustainabilityComparisonReport
{
    public static void Write(
        string outputPath,
        IReadOnlyDictionary<string, BalanceConfig> configs,
        IReadOnlyList<LoadoutPlan> loadouts,
        ResourceBudgetProfile profile)
    {
        Directory.CreateDirectory(outputPath);
        IReadOnlyList<SustainabilityComparisonRow> rows = Analyze(configs, loadouts, profile);

        File.WriteAllText(Path.Combine(outputPath, "resource-sustainability-comparison.md"), RenderMarkdown(rows));
        File.WriteAllText(Path.Combine(outputPath, "resource-sustainability-comparison.csv"), RenderCsv(rows));
    }

    private static IReadOnlyList<SustainabilityComparisonRow> Analyze(
        IReadOnlyDictionary<string, BalanceConfig> configs,
        IReadOnlyList<LoadoutPlan> loadouts,
        ResourceBudgetProfile profile)
    {
        List<SustainabilityComparisonRow> rows = new();

        foreach ((string configName, BalanceConfig config) in configs)
        {
            IReadOnlyList<ResourceSustainabilityReport.SustainabilityRow> sustainability = ResourceSustainabilityReport.Analyze(config, loadouts, profile);
            foreach (ResourceSustainabilityReport.SustainabilityRow row in sustainability.Where(row => row.Signal is "Severe" or "Watch" or "Unbudgeted"))
            {
                rows.Add(new SustainabilityComparisonRow(
                    configName,
                    row.Loadout,
                    row.Resource,
                    row.Signal,
                    row.SetupGap,
                    row.WeeklyNeed,
                    row.WeeklyAllowed,
                    row.WeeklyGap,
                    row.WeeklyRatio));
            }
        }

        return rows
            .OrderBy(row => row.Config, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Loadout, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(row => row.WeeklyGap)
            .ThenByDescending(row => row.SetupGap)
            .ThenBy(row => row.Resource, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string RenderMarkdown(IReadOnlyList<SustainabilityComparisonRow> rows)
    {
        List<string> lines = new()
        {
            "# Resource Sustainability Comparison",
            "",
            "Compares candidate balance configs against the same loadouts and safe resource budget assumptions.",
            "",
            "| Config | Loadout | Resource | Signal | Setup gap | Weekly need | Weekly budget | Weekly gap | Share used |",
            "| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: |"
        };

        foreach (SustainabilityComparisonRow row in rows)
        {
            lines.Add($"| {EscapeMarkdown(row.Config)} | {EscapeMarkdown(row.Loadout)} | {EscapeMarkdown(row.Resource)} | {row.Signal} | {row.SetupGap} | {row.WeeklyNeed} | {Decimal(row.WeeklyAllowed)} | {row.WeeklyGap} | {FormatRatio(row.WeeklyRatio)} |");
        }

        lines.Add("");
        lines.Add("## Fuel Sustainability Snapshot");
        lines.Add("");
        lines.Add("| Config | Resource | Weekly need | Weekly budget | Weekly gap | Worst fuel loadout |");
        lines.Add("| --- | --- | ---: | ---: | ---: | --- |");

        foreach (SustainabilityFuelSummaryRow row in BuildFuelSummaryRows(rows))
        {
            lines.Add($"| {EscapeMarkdown(row.Config)} | {EscapeMarkdown(row.Resource)} | {row.WeeklyNeed} | {Decimal(row.WeeklyAllowed)} | {row.WeeklyGap} | {EscapeMarkdown(row.WorstLoadout)} |");
        }

        lines.Add("");
        lines.Add("Use this report to decide which candidates deserve in-game testing. It measures sustainability pressure, not fun by itself.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderCsv(IReadOnlyList<SustainabilityComparisonRow> rows)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Config", "Loadout", "Resource", "Signal", "SetupGap", "WeeklyNeed", "WeeklyAllowed", "WeeklyGap", "WeeklyRatio"));

        foreach (SustainabilityComparisonRow row in rows)
        {
            csv.AppendLine(CsvLine(
                row.Config,
                row.Loadout,
                row.Resource,
                row.Signal,
                row.SetupGap,
                row.WeeklyNeed,
                Decimal(row.WeeklyAllowed),
                row.WeeklyGap,
                double.IsPositiveInfinity(row.WeeklyRatio) ? "Infinity" : Decimal(row.WeeklyRatio)));
        }

        return csv.ToString();
    }

    private static IReadOnlyList<SustainabilityFuelSummaryRow> BuildFuelSummaryRows(IReadOnlyList<SustainabilityComparisonRow> rows)
    {
        Dictionary<string, MutableFuelSummaryRow> totals = new(StringComparer.OrdinalIgnoreCase);
        foreach (SustainabilityComparisonRow row in rows.Where(row => row.WeeklyNeed > 0))
        {
            string key = $"{row.Config}\u001f{row.Resource}";
            if (!totals.TryGetValue(key, out MutableFuelSummaryRow? total))
            {
                total = new MutableFuelSummaryRow(row.Config, row.Resource);
                totals[key] = total;
            }

            total.WeeklyNeed += row.WeeklyNeed;
            total.WeeklyAllowed += row.WeeklyAllowed;
            total.WeeklyGap += row.WeeklyGap;
            if (row.WeeklyGap > total.WorstWeeklyGap)
            {
                total.WorstWeeklyGap = row.WeeklyGap;
                total.WorstLoadout = row.Loadout;
            }
        }

        return totals.Values
            .Select(row => new SustainabilityFuelSummaryRow(row.Config, row.Resource, row.WeeklyNeed, row.WeeklyAllowed, row.WeeklyGap, row.WorstLoadout))
            .OrderBy(row => row.Config, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(row => row.WeeklyGap)
            .ThenBy(row => row.Resource, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private static string Decimal(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatRatio(double ratio)
    {
        return double.IsPositiveInfinity(ratio)
            ? "unbudgeted"
            : ratio.ToString("P0", CultureInfo.InvariantCulture);
    }

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private sealed record SustainabilityComparisonRow(
        string Config,
        string Loadout,
        string Resource,
        string Signal,
        int SetupGap,
        int WeeklyNeed,
        double WeeklyAllowed,
        int WeeklyGap,
        double WeeklyRatio);

    private sealed record SustainabilityFuelSummaryRow(
        string Config,
        string Resource,
        int WeeklyNeed,
        double WeeklyAllowed,
        int WeeklyGap,
        string WorstLoadout);

    private sealed class MutableFuelSummaryRow(string config, string resource)
    {
        public string Config { get; } = config;
        public string Resource { get; } = resource;
        public int WeeklyNeed { get; set; }
        public double WeeklyAllowed { get; set; }
        public int WeeklyGap { get; set; }
        public int WorstWeeklyGap { get; set; }
        public string WorstLoadout { get; set; } = "-";
    }
}
