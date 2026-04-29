using System.Globalization;
using System.Text;
using PowerGrid.Balancer.Core;

internal static class AuditReport
{
    public static void Write(string outputPath, BalanceConfig config, IReadOnlyList<SimulationResult> results)
    {
        Directory.CreateDirectory(outputPath);

        File.WriteAllText(Path.Combine(outputPath, "summary.md"), RenderSummary(config, results));
        File.WriteAllText(Path.Combine(outputPath, "scenario-results.csv"), RenderScenarioCsv(results));
        File.WriteAllText(Path.Combine(outputPath, "fuel-use.csv"), RenderFuelUseCsv(results));
        File.WriteAllText(Path.Combine(outputPath, "generator-capacity.md"), RenderGeneratorCapacityMarkdown(config));
        File.WriteAllText(Path.Combine(outputPath, "generator-capacity.csv"), RenderGeneratorCapacityCsv(config));
        File.WriteAllText(Path.Combine(outputPath, "machine-defaults.csv"), RenderMachineDefaultsCsv(config));
    }

    private static string RenderSummary(BalanceConfig config, IReadOnlyList<SimulationResult> results)
    {
        int stable = results.Count(result => result.IsStable);
        int unstable = results.Count - stable;
        double averageCoverage = results.Count == 0 ? 1 : results.Average(result => result.AveragePowerCoverage);
        double averageSpeed = results.Count == 0 ? 0 : results.Average(result => result.AverageSpeedBonus);

        List<string> lines = new()
        {
            "# PowerGrid Balance Audit",
            "",
            "This report is meant to shape balance decisions. It favors repeatable signals over one-off save testing.",
            "",
            "## Scenario Summary",
            "",
            "| Metric | Value |",
            "| --- | ---: |",
            $"| Scenarios | {results.Count} |",
            $"| Stable | {stable} |",
            $"| Unstable | {unstable} |",
            $"| Average power coverage | {Percent(averageCoverage)} |",
            $"| Average speed bonus delivered | {Percent(averageSpeed)} |",
            $"| Tick length | {config.TickMinutes} minutes |",
            $"| Ticks per day | {config.TicksPerDay} |",
            "",
            "## Balance Signals",
            ""
        };

        foreach (string signal in BuildSignals(results))
            lines.Add($"- {signal}");

        lines.Add("");
        lines.Add("## Scenario Results");
        lines.Add("");
        lines.Add("| Scenario | Verdict | Demand | Generation | Cable | Unmet EU | Coverage | Speed Bonus | Main warning |");
        lines.Add("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |");

        foreach (SimulationResult result in results.OrderBy(result => result.ScenarioName, StringComparer.OrdinalIgnoreCase))
        {
            string warning = result.Warnings.FirstOrDefault() ?? "";
            lines.Add($"| {EscapeMarkdown(result.ScenarioName)} | {(result.IsStable ? "Stable" : "Unstable")} | {result.DemandEuPerTick} | {result.GenerationEuPerTick} | {result.CableThroughputEuPerTick} | {result.TotalUnmetEu} | {Percent(result.AveragePowerCoverage)} | {Percent(result.AverageSpeedBonus)} | {EscapeMarkdown(warning)} |");
        }

        lines.Add("");
        lines.Add("## Generated Data Files");
        lines.Add("");
        lines.Add("- `scenario-results.csv`: one row per benchmark scenario.");
        lines.Add("- `fuel-use.csv`: fuel consumed and shortfall by scenario.");
        lines.Add("- `generator-capacity.csv`: how many machines each generator count can support.");
        lines.Add("- `generator-capacity.md`: readable version of the generator capacity table.");
        lines.Add("- `machine-defaults.csv`: current machine EU demand and max speed bonuses.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderScenarioCsv(IReadOnlyList<SimulationResult> results)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine(
            "Scenario",
            "Verdict",
            "Days",
            "DemandEuPerTick",
            "GenerationEuPerTick",
            "CableThroughputEuPerTick",
            "BatteryCapacityEu",
            "FinalBatteryChargeEu",
            "TotalDemandEu",
            "TotalGeneratedEu",
            "TotalConsumedEu",
            "TotalUnmetEu",
            "AveragePowerCoverage",
            "AverageSpeedBonus",
            "SurplusRatio",
            "BatteryUtilization",
            "HasThroughputBottleneck",
            "Warnings",
            "Recommendations"));

        foreach (SimulationResult result in results.OrderBy(result => result.ScenarioName, StringComparer.OrdinalIgnoreCase))
        {
            csv.AppendLine(CsvLine(
                result.ScenarioName,
                result.IsStable ? "Stable" : "Unstable",
                result.Days,
                result.DemandEuPerTick,
                result.GenerationEuPerTick,
                result.CableThroughputEuPerTick,
                result.BatteryCapacityEu,
                result.FinalBatteryChargeEu,
                result.TotalDemandEu,
                result.TotalGeneratedEu,
                result.TotalConsumedEu,
                result.TotalUnmetEu,
                Decimal(result.AveragePowerCoverage),
                Decimal(result.AverageSpeedBonus),
                Decimal(result.SurplusRatio),
                Decimal(result.BatteryUtilization),
                result.HasThroughputBottleneck,
                string.Join(" | ", result.Warnings),
                string.Join(" | ", result.Recommendations)));
        }

        return csv.ToString();
    }

    private static string RenderFuelUseCsv(IReadOnlyList<SimulationResult> results)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine(
            "Scenario",
            "Fuel",
            "AvailableUnits",
            "ConsumedUnits",
            "GeneratorTicksUsed",
            "GeneratorTicksShort",
            "ConsumedUnitsPerDay",
            "RanShort"));

        foreach (SimulationResult result in results.OrderBy(result => result.ScenarioName, StringComparer.OrdinalIgnoreCase))
        {
            foreach (FuelUseResult fuel in result.FuelUse.OrderBy(fuel => fuel.Fuel, StringComparer.OrdinalIgnoreCase))
            {
                double consumedPerDay = result.Days <= 0 ? fuel.UnitsConsumed : (double)fuel.UnitsConsumed / result.Days;
                csv.AppendLine(CsvLine(
                    result.ScenarioName,
                    fuel.Fuel,
                    fuel.UnitsAvailable,
                    fuel.UnitsConsumed,
                    fuel.TicksConsumed,
                    fuel.TicksShort,
                    Decimal(consumedPerDay),
                    fuel.TicksShort > 0));
            }
        }

        return csv.ToString();
    }

    private static string RenderGeneratorCapacityMarkdown(BalanceConfig config)
    {
        List<string> lines = new()
        {
            "# Generator Capacity",
            "",
            "Counts show the maximum number of one machine type that can be fully powered by that generator stack under clear/base output.",
            "",
            "| Generator | Count | Output | Machine | Max machines | Demand used | Fuel/day | Smallest cable | Notes |",
            "| --- | ---: | ---: | --- | ---: | ---: | ---: | --- | --- |"
        };

        foreach (CapacityRow row in BuildCapacityRows(config))
        {
            lines.Add($"| {EscapeMarkdown(row.Generator)} | {row.GeneratorCount} | {row.OutputEuPerTick} | {EscapeMarkdown(row.Machine)} | {row.MaxMachines} | {row.DemandUsedEuPerTick} | {Decimal(row.FuelUnitsPerDay)} | {EscapeMarkdown(row.SmallestCable)} | {EscapeMarkdown(row.Notes)} |");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderGeneratorCapacityCsv(BalanceConfig config)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine(
            "Generator",
            "GeneratorCount",
            "OutputEuPerTick",
            "Machine",
            "MachineDemandEuPerTick",
            "MachineMaxSpeedBonus",
            "MaxMachines",
            "DemandUsedEuPerTick",
            "SurplusEuPerTick",
            "Fuel",
            "FuelUnitsPerDay",
            "SmallestCable",
            "Notes"));

        foreach (CapacityRow row in BuildCapacityRows(config))
        {
            csv.AppendLine(CsvLine(
                row.Generator,
                row.GeneratorCount,
                row.OutputEuPerTick,
                row.Machine,
                row.MachineDemandEuPerTick,
                Decimal(row.MachineMaxSpeedBonus),
                row.MaxMachines,
                row.DemandUsedEuPerTick,
                row.SurplusEuPerTick,
                row.Fuel,
                Decimal(row.FuelUnitsPerDay),
                row.SmallestCable,
                row.Notes));
        }

        return csv.ToString();
    }

    private static string RenderMachineDefaultsCsv(BalanceConfig config)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Machine", "DemandEuPerTick", "MaxSpeedBonus", "Notes"));

        foreach (MachineDefinition machine in config.Machines.Values.OrderBy(machine => machine.Name, StringComparer.OrdinalIgnoreCase))
            csv.AppendLine(CsvLine(machine.Name, machine.DemandEuPerTick, Decimal(machine.MaxSpeedBonus), machine.Notes));

        return csv.ToString();
    }

    private static IReadOnlyList<string> BuildSignals(IReadOnlyList<SimulationResult> results)
    {
        List<string> signals = new();

        foreach (SimulationResult result in results.Where(result => result.TotalUnmetEu > 0).OrderByDescending(result => result.TotalUnmetEu))
            signals.Add($"{result.ScenarioName} is unstable with {result.TotalUnmetEu} EU unmet; inspect its warnings before changing defaults.");

        foreach (SimulationResult result in results.Where(result => result.IsStable && result.SurplusRatio is >= 0 and < 0.10f))
            signals.Add($"{result.ScenarioName} is stable but tight, with less than 10% generation surplus.");

        foreach (SimulationResult result in results.Where(result => result.HasThroughputBottleneck))
            signals.Add($"{result.ScenarioName} hits cable throughput limits, which is useful for checking cable tier progression.");

        foreach (SimulationResult result in results.Where(result => result.FuelUse.Any(fuel => fuel.TicksShort > 0)))
            signals.Add($"{result.ScenarioName} runs out of fuel, so it can catch recipes or fuel values that make early power too annoying.");

        if (signals.Count == 0)
            signals.Add("No obvious balance stress signals were found in the current benchmark set.");

        return signals;
    }

    private static IReadOnlyList<CapacityRow> BuildCapacityRows(BalanceConfig config)
    {
        List<CapacityRow> rows = new();

        foreach (GeneratorDefinition generator in config.Generators.Values.OrderBy(generator => generator.Name, StringComparer.OrdinalIgnoreCase))
        {
            for (int generatorCount = 1; generatorCount <= 8; generatorCount++)
            {
                int output = generator.OutputEuPerTick * generatorCount;
                double fuelUnitsPerDay = CalculateFuelUnitsPerDay(config, generator, generatorCount);

                foreach (MachineDefinition machine in config.Machines.Values.OrderBy(machine => machine.Name, StringComparer.OrdinalIgnoreCase))
                {
                    int maxMachines = machine.DemandEuPerTick <= 0 ? 0 : output / machine.DemandEuPerTick;
                    int demandUsed = maxMachines * machine.DemandEuPerTick;
                    int surplus = output - demandUsed;
                    string smallestCable = FindSmallestCable(config, Math.Max(output, demandUsed));
                    string notes = BuildCapacityNotes(generator, maxMachines, smallestCable);

                    rows.Add(new CapacityRow(
                        generator.Name,
                        generatorCount,
                        output,
                        machine.Name,
                        machine.DemandEuPerTick,
                        machine.MaxSpeedBonus,
                        maxMachines,
                        demandUsed,
                        surplus,
                        generator.Fuel ?? "",
                        fuelUnitsPerDay,
                        smallestCable,
                        notes));
                }
            }
        }

        return rows;
    }

    private static double CalculateFuelUnitsPerDay(BalanceConfig config, GeneratorDefinition generator, int generatorCount)
    {
        if (string.IsNullOrWhiteSpace(generator.Fuel))
            return 0;

        if (!config.Fuels.TryGetValue(generator.Fuel, out FuelDefinition? fuel))
            return 0;

        return (double)(Math.Max(1, config.TicksPerDay) * generatorCount) / Math.Max(1, fuel.TicksPerUnit);
    }

    private static string FindSmallestCable(BalanceConfig config, int requiredThroughput)
    {
        CableDefinition? cable = config.Cables.Values
            .OrderBy(cable => cable.ThroughputEuPerTick)
            .FirstOrDefault(cable => cable.ThroughputEuPerTick >= requiredThroughput);

        return cable?.Name ?? "split networks";
    }

    private static string BuildCapacityNotes(GeneratorDefinition generator, int maxMachines, string smallestCable)
    {
        List<string> notes = new();

        if (maxMachines == 0)
            notes.Add("cannot fully power one machine");
        if (generator.WeatherAdjusted)
            notes.Add("weather-adjusted");
        if (smallestCable == "split networks")
            notes.Add("above one cable cap");

        return string.Join("; ", notes);
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
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static string Percent(double value)
    {
        return value.ToString("0.0%", CultureInfo.InvariantCulture);
    }

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private sealed record CapacityRow(
        string Generator,
        int GeneratorCount,
        int OutputEuPerTick,
        string Machine,
        int MachineDemandEuPerTick,
        float MachineMaxSpeedBonus,
        int MaxMachines,
        int DemandUsedEuPerTick,
        int SurplusEuPerTick,
        string Fuel,
        double FuelUnitsPerDay,
        string SmallestCable,
        string Notes);
}
