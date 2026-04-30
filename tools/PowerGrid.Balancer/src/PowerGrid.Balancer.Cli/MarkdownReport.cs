using System.Globalization;
using PowerGrid.Balancer.Core;

internal static class MarkdownReport
{
    public static string Render(SimulationResult result)
    {
        List<string> lines = new()
        {
            $"# {result.ScenarioName}",
            "",
            $"Verdict: **{(result.IsStable ? "Stable" : "Unstable")}**",
            "",
            "## Core Metrics",
            "",
            "| Metric | Value |",
            "| --- | ---: |",
            $"| Days simulated | {result.Days} |",
            $"| Ticks simulated | {result.TicksSimulated} |",
            $"| Demand | {result.DemandEuPerTick} EU/tick |",
            $"| Generation | {result.GenerationEuPerTick} EU/tick |",
            $"| Cable throughput | {result.CableThroughputEuPerTick} EU/tick |",
            $"| Battery capacity | {result.BatteryCapacityEu} EU |",
            $"| Final battery charge | {result.FinalBatteryChargeEu} EU |",
            $"| Total demand | {result.TotalDemandEu} EU |",
            $"| Total generated | {result.TotalGeneratedEu} EU |",
            $"| Total consumed | {result.TotalConsumedEu} EU |",
            $"| Unmet demand | {result.TotalUnmetEu} EU |",
            $"| Average power coverage | {Percent(result.AveragePowerCoverage)} |",
            $"| Average speed bonus delivered | {Percent(result.AverageSpeedBonus)} |",
            $"| Surplus ratio | {SignedPercent(result.SurplusRatio)} |",
            $"| Battery utilization | {result.BatteryUtilization.ToString("0.00", CultureInfo.InvariantCulture)}x capacity |",
            ""
        };

        if (result.FuelUse.Count > 0)
        {
            lines.Add("## Fuel");
            lines.Add("");
            lines.Add("| Fuel | Available | Consumed | Generator-ticks used | Generator-ticks short |");
            lines.Add("| --- | ---: | ---: | ---: | ---: |");
            foreach (FuelUseResult fuel in result.FuelUse)
                lines.Add($"| {fuel.Fuel} | {fuel.UnitsAvailable} | {fuel.UnitsConsumed} | {fuel.TicksConsumed} | {fuel.TicksShort} |");
            lines.Add("");
        }

        AppendList(lines, "Warnings", result.Warnings);
        AppendList(lines, "Recommendations", result.Recommendations);

        return string.Join(Environment.NewLine, lines);
    }

    public static string RenderBatch(IReadOnlyList<SimulationResult> results)
    {
        List<string> lines = new()
        {
            "# PowerGrid Balance Batch",
            "",
            "| Scenario | Verdict | Demand | Generation | Unmet EU | Coverage | Speed Bonus | Warnings |",
            "| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |"
        };

        foreach (SimulationResult result in results)
        {
            lines.Add(
                $"| {result.ScenarioName} | {(result.IsStable ? "Stable" : "Unstable")} | {result.DemandEuPerTick} | {result.GenerationEuPerTick} | {result.TotalUnmetEu} | {Percent(result.AveragePowerCoverage)} | {Percent(result.AverageSpeedBonus)} | {result.Warnings.Count} |");
        }

        lines.Add("");
        lines.Add("## Unstable Scenarios");
        lines.Add("");

        List<SimulationResult> unstable = results.Where(result => !result.IsStable).ToList();
        if (unstable.Count == 0)
        {
            lines.Add("- None.");
        }
        else
        {
            foreach (SimulationResult result in unstable)
                lines.Add($"- {result.ScenarioName}: {result.TotalUnmetEu} EU unmet.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void AppendList(List<string> lines, string title, IReadOnlyList<string> values)
    {
        lines.Add($"## {title}");
        lines.Add("");

        if (values.Count == 0)
        {
            lines.Add("- None.");
            lines.Add("");
            return;
        }

        foreach (string value in values)
            lines.Add($"- {value}");

        lines.Add("");
    }

    private static string Percent(float value)
    {
        return value.ToString("0.0%", CultureInfo.InvariantCulture);
    }

    private static string SignedPercent(float value)
    {
        return value.ToString("+0.0%;-0.0%;0.0%", CultureInfo.InvariantCulture);
    }
}
