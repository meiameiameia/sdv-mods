using System.Globalization;
using System.Text;
using PowerGrid.Balancer.Core;

internal static class ProgressionComparisonReport
{
    public static void Write(
        string outputPath,
        string baselineName,
        BalanceConfig baseline,
        string proposedName,
        BalanceConfig proposed,
        ProgressionProfile profile)
    {
        Directory.CreateDirectory(outputPath);

        IReadOnlyList<StageComparison> rows = profile.Stages
            .Select(stage => new StageComparison(
                stage.Name,
                ProgressionReport.Analyze(baseline, stage),
                ProgressionReport.Analyze(proposed, stage)))
            .ToList();

        File.WriteAllText(Path.Combine(outputPath, "progression-comparison.md"), RenderMarkdown(profile, baselineName, proposedName, rows));
        File.WriteAllText(Path.Combine(outputPath, "progression-comparison.csv"), RenderCsv(baselineName, proposedName, rows));
        File.WriteAllText(Path.Combine(outputPath, "progression-comparison-gaps.csv"), RenderGapCsv(baselineName, proposedName, rows));
    }

    private static string RenderMarkdown(
        ProgressionProfile profile,
        string baselineName,
        string proposedName,
        IReadOnlyList<StageComparison> rows)
    {
        List<string> lines = new()
        {
            $"# {profile.Name} Comparison",
            "",
            $"Baseline: `{baselineName}`",
            "",
            $"Proposed: `{proposedName}`",
            "",
            "## Headline Comparison",
            "",
            "| Stage | Demand | Generators | Cable zones | Fuel/day | Fuel window | Resource gaps | Main result |",
            "| --- | ---: | --- | --- | --- | --- | --- | --- |"
        };

        foreach (StageComparison row in rows)
        {
            lines.Add(
                $"| {Escape(row.StageName)} | {row.Proposed.DemandEuPerTick} | {row.Baseline.GeneratorCount} -> {row.Proposed.GeneratorCount} | {row.Baseline.CableZonesNeeded} -> {row.Proposed.CableZonesNeeded} | {Decimal(row.Baseline.Fuel.FuelUnitsPerDay)} -> {Decimal(row.Proposed.Fuel.FuelUnitsPerDay)} {Escape(row.Proposed.Fuel.Name)} | {Decimal(row.Baseline.Fuel.DaysSustained)} -> {Decimal(row.Proposed.Fuel.DaysSustained)} days | {row.Baseline.ResourceGaps.Count} -> {row.Proposed.ResourceGaps.Count} | {Escape(Summarize(row))} |");
        }

        lines.Add("");
        lines.Add("## Reading The Result");
        lines.Add("");
        lines.Add("- Lower generator count means less farm clutter.");
        lines.Add("- Higher fuel window means less maintenance grind.");
        lines.Add("- Lower cable zones means less forced network splitting.");
        lines.Add("- Resource gaps should shrink for early/mid stages, but mature conversion may still point to future tiers.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderCsv(string baselineName, string proposedName, IReadOnlyList<StageComparison> rows)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine(
            "Stage",
            "Baseline",
            "Proposed",
            "DemandEuPerTick",
            "BaselineGenerators",
            "ProposedGenerators",
            "GeneratorDelta",
            "BaselineCableZones",
            "ProposedCableZones",
            "CableZoneDelta",
            "BaselineFuelPerDay",
            "ProposedFuelPerDay",
            "FuelPerDayDelta",
            "BaselineFuelDays",
            "ProposedFuelDays",
            "FuelDaysDelta",
            "BaselineResourceGapTypes",
            "ProposedResourceGapTypes",
            "ResourceGapDelta",
            "Summary"));

        foreach (StageComparison row in rows)
        {
            csv.AppendLine(CsvLine(
                row.StageName,
                baselineName,
                proposedName,
                row.Proposed.DemandEuPerTick,
                row.Baseline.GeneratorCount,
                row.Proposed.GeneratorCount,
                row.Proposed.GeneratorCount - row.Baseline.GeneratorCount,
                row.Baseline.CableZonesNeeded,
                row.Proposed.CableZonesNeeded,
                row.Proposed.CableZonesNeeded - row.Baseline.CableZonesNeeded,
                Decimal(row.Baseline.Fuel.FuelUnitsPerDay),
                Decimal(row.Proposed.Fuel.FuelUnitsPerDay),
                Decimal(row.Proposed.Fuel.FuelUnitsPerDay - row.Baseline.Fuel.FuelUnitsPerDay),
                Decimal(row.Baseline.Fuel.DaysSustained),
                Decimal(row.Proposed.Fuel.DaysSustained),
                Decimal(row.Proposed.Fuel.DaysSustained - row.Baseline.Fuel.DaysSustained),
                row.Baseline.ResourceGaps.Count,
                row.Proposed.ResourceGaps.Count,
                row.Proposed.ResourceGaps.Count - row.Baseline.ResourceGaps.Count,
                Summarize(row)));
        }

        return csv.ToString();
    }

    private static string RenderGapCsv(string baselineName, string proposedName, IReadOnlyList<StageComparison> rows)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Stage", "Config", "Resource", "Gap"));

        foreach (StageComparison row in rows)
        {
            foreach ((string resource, int gap) in row.Baseline.ResourceGaps.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                csv.AppendLine(CsvLine(row.StageName, baselineName, resource, gap));

            foreach ((string resource, int gap) in row.Proposed.ResourceGaps.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                csv.AppendLine(CsvLine(row.StageName, proposedName, resource, gap));
        }

        return csv.ToString();
    }

    private static string Summarize(StageComparison row)
    {
        List<string> wins = new();

        if (row.Proposed.GeneratorCount < row.Baseline.GeneratorCount)
            wins.Add("less generator clutter");
        if (row.Proposed.CableZonesNeeded < row.Baseline.CableZonesNeeded)
            wins.Add("fewer cable zones");
        if (row.Proposed.Fuel.DaysSustained > row.Baseline.Fuel.DaysSustained)
            wins.Add("longer fuel window");
        if (row.Proposed.ResourceGaps.Count < row.Baseline.ResourceGaps.Count)
            wins.Add("fewer resource gap types");

        return wins.Count == 0 ? "no clear improvement" : string.Join("; ", wins);
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

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private sealed record StageComparison(
        string StageName,
        ProgressionReport.StageAnalysis Baseline,
        ProgressionReport.StageAnalysis Proposed);
}
