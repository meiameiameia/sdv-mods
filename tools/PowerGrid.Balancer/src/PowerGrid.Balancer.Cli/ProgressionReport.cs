using System.Globalization;
using System.Text;
using PowerGrid.Balancer.Core;

internal static class ProgressionReport
{
    public static void Write(string outputPath, BalanceConfig config, ProgressionProfile profile)
    {
        Directory.CreateDirectory(outputPath);

        IReadOnlyList<StageAnalysis> rows = profile.Stages
            .Select(stage => Analyze(config, stage))
            .ToList();

        File.WriteAllText(Path.Combine(outputPath, "progression-summary.md"), RenderSummary(profile, rows));
        File.WriteAllText(Path.Combine(outputPath, "progression-stages.csv"), RenderStagesCsv(rows));
        File.WriteAllText(Path.Combine(outputPath, "progression-resource-gaps.csv"), RenderResourceGapsCsv(rows));
        File.WriteAllText(Path.Combine(outputPath, "progression-fuel.csv"), RenderFuelCsv(rows));
    }

    public static StageAnalysis Analyze(BalanceConfig config, ProgressionStage stage)
    {
        int demand = 0;
        foreach ((string id, int count) in stage.PoweredMachines)
        {
            MachineDefinition machine = Resolve(config.Machines, id, "machine");
            demand += machine.DemandEuPerTick * Math.Max(0, count);
        }

        GeneratorDefinition generator = Resolve(config.Generators, stage.Generator, "generator");
        CableDefinition cable = Resolve(config.Cables, stage.Cable, "cable");
        int generatorCount = generator.OutputEuPerTick <= 0 ? 0 : (int)Math.Ceiling((double)demand / generator.OutputEuPerTick);
        int cableZones = cable.ThroughputEuPerTick <= 0 ? 0 : (int)Math.Ceiling((double)demand / cable.ThroughputEuPerTick);

        Dictionary<string, int> upfrontCost = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string machine, int count) in stage.PoweredMachines)
            AddExpandedRecipeCost(config, upfrontCost, machine, count);

        AddExpandedRecipeCost(config, upfrontCost, generator.Name, generatorCount);

        if (!string.IsNullOrWhiteSpace(stage.Battery) && stage.BatteryCount > 0)
            AddExpandedRecipeCost(config, upfrontCost, stage.Battery, stage.BatteryCount);

        if (stage.CableCount > 0)
            AddExpandedRecipeCost(config, upfrontCost, cable.Name, stage.CableCount);

        if (stage.ConduitCount > 0)
            AddExpandedRecipeCost(config, upfrontCost, "Power Conduit", stage.ConduitCount);

        Dictionary<string, int> remaining = new(stage.Stockpile, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> gaps = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string resource, int need) in upfrontCost)
        {
            remaining.TryGetValue(resource, out int available);
            int gap = need - available;
            if (gap > 0)
                gaps[resource] = gap;
            remaining[resource] = Math.Max(0, available - need);
        }

        FuelAnalysis fuel = AnalyzeFuel(config, generator, generatorCount, remaining);
        List<string> signals = BuildSignals(stage, demand, generator, generatorCount, cable, cableZones, gaps, fuel);

        return new StageAnalysis(
            stage,
            demand,
            generator.Name,
            generatorCount,
            generator.OutputEuPerTick * generatorCount,
            cable.Name,
            cable.ThroughputEuPerTick,
            cableZones,
            upfrontCost,
            gaps,
            fuel,
            signals);
    }

    private static FuelAnalysis AnalyzeFuel(
        BalanceConfig config,
        GeneratorDefinition generator,
        int generatorCount,
        IReadOnlyDictionary<string, int> stockAfterUpfrontCost)
    {
        if (generatorCount <= 0 || string.IsNullOrWhiteSpace(generator.Fuel))
            return new FuelAnalysis("", 0, 0, 0, 0, Array.Empty<ResourceNeed>());

        FuelDefinition fuel = Resolve(config.Fuels, generator.Fuel, "fuel");
        double unitsPerDay = (double)(generatorCount * config.TicksPerDay) / Math.Max(1, fuel.TicksPerUnit);

        stockAfterUpfrontCost.TryGetValue(fuel.Name, out int directFuel);
        double craftableFuel = 0;
        List<ResourceNeed> fuelRecipeNeeds = new();

        if (config.Recipes.TryGetValue(fuel.Name, out RecipeDefinition? recipe) && recipe.Ingredients.Count > 0)
        {
            int maxCraftBatches = int.MaxValue;
            foreach ((string resource, int ingredientCount) in recipe.Ingredients)
            {
                stockAfterUpfrontCost.TryGetValue(resource, out int available);
                int batches = ingredientCount <= 0 ? 0 : available / ingredientCount;
                maxCraftBatches = Math.Min(maxCraftBatches, batches);
                fuelRecipeNeeds.Add(new ResourceNeed(resource, ingredientCount * Math.Max(0, maxCraftBatches)));
            }

            if (maxCraftBatches != int.MaxValue)
                craftableFuel = maxCraftBatches * Math.Max(1, recipe.OutputCount);
        }

        double totalFuel = directFuel + craftableFuel;
        double daysSustained = unitsPerDay <= 0 ? 0 : totalFuel / unitsPerDay;

        return new FuelAnalysis(fuel.Name, unitsPerDay, directFuel, craftableFuel, daysSustained, fuelRecipeNeeds);
    }

    private static List<string> BuildSignals(
        ProgressionStage stage,
        int demand,
        GeneratorDefinition generator,
        int generatorCount,
        CableDefinition cable,
        int cableZones,
        IReadOnlyDictionary<string, int> gaps,
        FuelAnalysis fuel)
    {
        List<string> signals = new();

        if (gaps.Count > 0)
            signals.Add($"Missing {gaps.Count} resource type(s) for upfront conversion.");
        if (generatorCount > 12)
            signals.Add($"{generatorCount} {generator.Name}s is likely too much farm clutter for one stage.");
        if (cableZones > 1)
            signals.Add($"{stage.Cable} needs {cableZones} zones for {demand} EU/tick demand.");
        if (fuel.FuelUnitsPerDay > 0 && fuel.DaysSustained < 7)
            signals.Add($"{fuel.Name} lasts only {Decimal(fuel.DaysSustained)} day(s) after upfront costs.");
        if (fuel.FuelUnitsPerDay > 50)
            signals.Add($"{fuel.Name} burn is {Decimal(fuel.FuelUnitsPerDay)} per day; check grind pressure.");
        if (signals.Count == 0)
            signals.Add("Looks reasonable under current assumptions.");

        return signals;
    }

    private static string RenderSummary(ProgressionProfile profile, IReadOnlyList<StageAnalysis> rows)
    {
        List<string> lines = new()
        {
            $"# {profile.Name}",
            "",
            profile.Description,
            "",
            "## Progression Ladder",
            "",
            "| Stage | Date | Goal | Demand | Generator plan | Cable zones | Fuel/day | Fuel window | Resource gaps | Main signal |",
            "| --- | --- | --- | ---: | --- | ---: | ---: | ---: | ---: | --- |"
        };

        foreach (StageAnalysis row in rows)
        {
            lines.Add($"| {EscapeMarkdown(row.Stage.Name)} | {row.Stage.Season} {row.Stage.Day}, Y{row.Stage.Year} | {EscapeMarkdown(row.Stage.Goal)} | {row.DemandEuPerTick} | {row.GeneratorCount}x {EscapeMarkdown(row.Generator)} ({row.GenerationEuPerTick} EU/tick) | {row.CableZonesNeeded} | {Decimal(row.Fuel.FuelUnitsPerDay)} {EscapeMarkdown(row.Fuel.Name)} | {Decimal(row.Fuel.DaysSustained)} days | {row.ResourceGaps.Count} | {EscapeMarkdown(row.Signals.FirstOrDefault() ?? "")} |");
        }

        lines.Add("");
        lines.Add("## Read This First");
        lines.Add("");
        lines.Add("- Stages are balance checkpoints, not promises that every player will own exactly these resources.");
        lines.Add("- Resource gaps mean the current recipe/config would make that stage hard to adopt immediately.");
        lines.Add("- Low fuel windows point to maintenance grind. Those are especially important for PowerGrid.");
        lines.Add("- High cable zone counts point to distribution pressure: maybe intentional planning, maybe too much friction.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderStagesCsv(IReadOnlyList<StageAnalysis> rows)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Stage", "Year", "Season", "Day", "DemandEuPerTick", "Generator", "GeneratorCount", "GenerationEuPerTick", "Cable", "CableThroughput", "CableZonesNeeded", "Fuel", "FuelUnitsPerDay", "FuelDaysSustained", "ResourceGapTypes", "Signals"));

        foreach (StageAnalysis row in rows)
        {
            csv.AppendLine(CsvLine(
                row.Stage.Name,
                row.Stage.Year,
                row.Stage.Season,
                row.Stage.Day,
                row.DemandEuPerTick,
                row.Generator,
                row.GeneratorCount,
                row.GenerationEuPerTick,
                row.Cable,
                row.CableThroughputEuPerTick,
                row.CableZonesNeeded,
                row.Fuel.Name,
                Decimal(row.Fuel.FuelUnitsPerDay),
                Decimal(row.Fuel.DaysSustained),
                row.ResourceGaps.Count,
                string.Join(" | ", row.Signals)));
        }

        return csv.ToString();
    }

    private static string RenderResourceGapsCsv(IReadOnlyList<StageAnalysis> rows)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Stage", "Resource", "UpfrontNeed", "Stockpile", "Gap"));

        foreach (StageAnalysis row in rows)
        {
            foreach ((string resource, int need) in row.UpfrontCost.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                row.Stage.Stockpile.TryGetValue(resource, out int stock);
                row.ResourceGaps.TryGetValue(resource, out int gap);
                csv.AppendLine(CsvLine(row.Stage.Name, resource, need, stock, gap));
            }
        }

        return csv.ToString();
    }

    private static string RenderFuelCsv(IReadOnlyList<StageAnalysis> rows)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Stage", "Fuel", "FuelUnitsPerDay", "DirectFuelAfterUpfrontCost", "CraftableFuelFromStockpile", "DaysSustained"));

        foreach (StageAnalysis row in rows.Where(row => !string.IsNullOrWhiteSpace(row.Fuel.Name)))
        {
            csv.AppendLine(CsvLine(
                row.Stage.Name,
                row.Fuel.Name,
                Decimal(row.Fuel.FuelUnitsPerDay),
                Decimal(row.Fuel.DirectFuelUnits),
                Decimal(row.Fuel.CraftableFuelUnits),
                Decimal(row.Fuel.DaysSustained)));
        }

        return csv.ToString();
    }

    private static void AddExpandedRecipeCost(BalanceConfig config, Dictionary<string, int> cost, string item, int count)
    {
        if (count <= 0)
            return;

        AddExpandedRecipeCost(config, cost, item, count, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static void AddExpandedRecipeCost(BalanceConfig config, Dictionary<string, int> cost, string item, int count, HashSet<string> seen)
    {
        if (!config.Recipes.TryGetValue(item, out RecipeDefinition? recipe) || seen.Contains(item))
        {
            Add(cost, item, count);
            return;
        }

        seen.Add(item);
        int batches = (int)Math.Ceiling((double)count / Math.Max(1, recipe.OutputCount));
        foreach ((string ingredient, int ingredientCount) in recipe.Ingredients)
            AddExpandedRecipeCost(config, cost, ingredient, ingredientCount * batches, seen);
        seen.Remove(item);
    }

    private static T Resolve<T>(Dictionary<string, T> values, string id, string label)
    {
        return values.TryGetValue(id, out T? value)
            ? value
            : throw new InvalidOperationException($"Unknown {label} '{id}'.");
    }

    private static void Add(Dictionary<string, int> values, string key, int amount)
    {
        values.TryGetValue(key, out int current);
        values[key] = current + amount;
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

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    public sealed record StageAnalysis(
        ProgressionStage Stage,
        int DemandEuPerTick,
        string Generator,
        int GeneratorCount,
        int GenerationEuPerTick,
        string Cable,
        int CableThroughputEuPerTick,
        int CableZonesNeeded,
        IReadOnlyDictionary<string, int> UpfrontCost,
        IReadOnlyDictionary<string, int> ResourceGaps,
        FuelAnalysis Fuel,
        IReadOnlyList<string> Signals);

    public sealed record FuelAnalysis(
        string Name,
        double FuelUnitsPerDay,
        double DirectFuelUnits,
        double CraftableFuelUnits,
        double DaysSustained,
        IReadOnlyList<ResourceNeed> RecipeNeeds);

    public sealed record ResourceNeed(string Resource, int Count);
}
