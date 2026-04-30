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
        File.WriteAllText(Path.Combine(outputPath, "progression-plan.md"), RenderPlan(config, rows));
        File.WriteAllText(Path.Combine(outputPath, "progression-plan.csv"), RenderPlanCsv(config, rows));
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
        IReadOnlyList<string> fuelOptions = GetFuelOptions(generator);
        if (generatorCount <= 0 || fuelOptions.Count == 0)
            return new FuelAnalysis("", 0, 0, 0, 0, Array.Empty<ResourceNeed>());

        if (fuelOptions.Count > 1)
            return AnalyzeFlexibleFuel(config, generator, fuelOptions, generatorCount, stockAfterUpfrontCost);

        FuelDefinition fuel = Resolve(config.Fuels, fuelOptions[0], "fuel");
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

    private static FuelAnalysis AnalyzeFlexibleFuel(
        BalanceConfig config,
        GeneratorDefinition generator,
        IReadOnlyList<string> fuelOptions,
        int generatorCount,
        IReadOnlyDictionary<string, int> stockAfterUpfrontCost)
    {
        FuelDefinition primaryFuel = Resolve(config.Fuels, fuelOptions[0], "fuel");
        int ticksNeededPerDay = generatorCount * config.TicksPerDay;
        double primaryEquivalentPerDay = (double)ticksNeededPerDay / Math.Max(1, primaryFuel.TicksPerUnit);
        int totalTicksAvailable = 0;
        double directPrimaryEquivalent = 0;
        List<ResourceNeed> fuelRecipeNeeds = new();

        foreach (string fuelId in fuelOptions)
        {
            FuelDefinition fuel = Resolve(config.Fuels, fuelId, "fuel");
            stockAfterUpfrontCost.TryGetValue(fuel.Name, out int units);
            int ticks = Math.Max(0, units) * Math.Max(1, fuel.TicksPerUnit);
            totalTicksAvailable += ticks;
            directPrimaryEquivalent += (double)ticks / Math.Max(1, primaryFuel.TicksPerUnit);
            fuelRecipeNeeds.Add(new ResourceNeed(fuel.Name, Math.Max(0, units)));
        }

        double daysSustained = ticksNeededPerDay <= 0 ? 0 : (double)totalTicksAvailable / ticksNeededPerDay;
        string name = $"{generator.Name} fuel ({primaryFuel.Name}-equivalent)";

        return new FuelAnalysis(name, primaryEquivalentPerDay, directPrimaryEquivalent, 0, daysSustained, fuelRecipeNeeds);
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

    private static string RenderPlan(BalanceConfig config, IReadOnlyList<StageAnalysis> rows)
    {
        List<string> lines = new()
        {
            "# PowerGrid Setup Plan",
            "",
            "A planning view for each progression checkpoint. Minimum generators cover the exact demand; comfort generators add about 15% headroom so a setup has room for a few extra machines or weather/fuel mistakes.",
            "",
            "| Stage | Demand | Minimum setup | Comfort setup | Cable plan | Fuel reserve target | Pressure | Recommendation |",
            "| --- | ---: | --- | --- | --- | --- | --- | --- |"
        };

        foreach (StageAnalysis row in rows)
        {
            PlanRow plan = BuildPlanRow(config, row);
            lines.Add($"| {EscapeMarkdown(row.Stage.Name)} | {row.DemandEuPerTick} EU/tick | {EscapeMarkdown(plan.MinimumSetup)} | {EscapeMarkdown(plan.ComfortSetup)} | {EscapeMarkdown(plan.CablePlan)} | {EscapeMarkdown(plan.FuelReserveTarget)} | {EscapeMarkdown(plan.Pressure)} | {EscapeMarkdown(plan.Recommendation)} |");
        }

        lines.Add("");
        lines.Add("## How To Use This");
        lines.Add("");
        lines.Add("- Minimum setup is useful for checking if a balance target is technically possible.");
        lines.Add("- Comfort setup is the better default for player-facing recommendations.");
        lines.Add("- Fuel reserve targets show how much fuel a player should keep around for a stable week or two.");
        lines.Add("- Pressure calls out whether the stage is blocked by resources, clutter, cable splitting, or fuel grind.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderPlanCsv(BalanceConfig config, IReadOnlyList<StageAnalysis> rows)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Stage", "DemandEuPerTick", "Generator", "MinimumGenerators", "ComfortGenerators", "ComfortSpareEuPerTick", "Cable", "CableZones", "Fuel", "MinimumFuelPerDay", "SevenDayFuelReserve", "FourteenDayFuelReserve", "Pressure", "Recommendation"));

        foreach (StageAnalysis row in rows)
        {
            PlanRow plan = BuildPlanRow(config, row);
            csv.AppendLine(CsvLine(
                row.Stage.Name,
                row.DemandEuPerTick,
                row.Generator,
                row.GeneratorCount,
                plan.ComfortGeneratorCount,
                plan.ComfortSpareEuPerTick,
                row.Cable,
                row.CableZonesNeeded,
                row.Fuel.Name,
                Decimal(plan.MinimumFuelPerDay),
                Decimal(plan.SevenDayFuelReserve),
                Decimal(plan.FourteenDayFuelReserve),
                plan.Pressure,
                plan.Recommendation));
        }

        return csv.ToString();
    }

    private static PlanRow BuildPlanRow(BalanceConfig config, StageAnalysis row)
    {
        GeneratorDefinition generator = Resolve(config.Generators, row.Generator, "generator");
        int comfortGeneratorCount = row.GeneratorCount;
        if (row.DemandEuPerTick > 0 && generator.OutputEuPerTick > 0)
        {
            comfortGeneratorCount = Math.Max(
                row.GeneratorCount,
                (int)Math.Ceiling(row.DemandEuPerTick * 1.15d / generator.OutputEuPerTick));
        }

        int comfortGeneration = comfortGeneratorCount * generator.OutputEuPerTick;
        int comfortSpare = Math.Max(0, comfortGeneration - row.DemandEuPerTick);
        double minimumFuelPerDay = row.Fuel.FuelUnitsPerDay;
        double comfortFuelPerDay = CalculateFuelPerDay(config, generator, comfortGeneratorCount);
        double sevenDayFuel = comfortFuelPerDay * 7;
        double fourteenDayFuel = comfortFuelPerDay * 14;

        string fuelReserve = string.IsNullOrWhiteSpace(row.Fuel.Name)
            ? "passive"
            : $"{Decimal(sevenDayFuel)} {row.Fuel.Name} for 7 days, {Decimal(fourteenDayFuel)} for 14 days";

        string pressure = SummarizePressure(row);
        string recommendation = Recommend(row, comfortGeneratorCount, comfortSpare);

        return new PlanRow(
            ComfortGeneratorCount: comfortGeneratorCount,
            ComfortSpareEuPerTick: comfortSpare,
            MinimumFuelPerDay: minimumFuelPerDay,
            SevenDayFuelReserve: sevenDayFuel,
            FourteenDayFuelReserve: fourteenDayFuel,
            MinimumSetup: $"{row.GeneratorCount}x {row.Generator} ({row.GenerationEuPerTick} EU/tick)",
            ComfortSetup: $"{comfortGeneratorCount}x {row.Generator} ({comfortGeneration} EU/tick, +{comfortSpare} spare)",
            CablePlan: $"{row.CableZonesNeeded} zone(s) using {row.Cable} ({row.CableThroughputEuPerTick} EU/tick)",
            FuelReserveTarget: fuelReserve,
            Pressure: pressure,
            Recommendation: recommendation);
    }

    private static double CalculateFuelPerDay(BalanceConfig config, GeneratorDefinition generator, int generatorCount)
    {
        IReadOnlyList<string> fuelOptions = GetFuelOptions(generator);
        if (generatorCount <= 0 || fuelOptions.Count == 0)
            return 0;

        FuelDefinition fuel = Resolve(config.Fuels, fuelOptions[0], "fuel");
        return (double)(generatorCount * config.TicksPerDay) / Math.Max(1, fuel.TicksPerUnit);
    }

    private static string SummarizePressure(StageAnalysis row)
    {
        List<string> pressure = new();
        if (row.ResourceGaps.Count > 0)
            pressure.Add("resources: " + string.Join("; ", row.ResourceGaps
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .Select(pair => $"{pair.Key} {pair.Value} short")));
        if (row.GeneratorCount > 12)
            pressure.Add("generator clutter");
        if (row.CableZonesNeeded > 1)
            pressure.Add($"{row.CableZonesNeeded} cable zones");
        if (row.Fuel.FuelUnitsPerDay > 50)
            pressure.Add($"{Decimal(row.Fuel.FuelUnitsPerDay)} {row.Fuel.Name}/day");

        return pressure.Count == 0 ? "low" : string.Join("; ", pressure);
    }

    private static string Recommend(StageAnalysis row, int comfortGeneratorCount, int comfortSpare)
    {
        if (row.GeneratorCount > 20 || row.Fuel.FuelUnitsPerDay > 80)
            return "Needs a future tier before this feels good.";
        if (row.ResourceGaps.Count > 0)
            return "Check recipes or expected stockpile before shipping this stage.";
        if (row.CableZonesNeeded > 2)
            return "Distribution pressure is high; consider stronger cables or conduits.";
        if (comfortGeneratorCount > row.GeneratorCount)
            return $"Plan for one buffer generator if players want comfort (+{comfortSpare} EU/tick).";
        return "Good default target.";
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

    private static IReadOnlyList<string> GetFuelOptions(GeneratorDefinition generator)
    {
        if (generator.FuelOptions.Count > 0)
            return generator.FuelOptions.Where(fuel => !string.IsNullOrWhiteSpace(fuel)).ToList();

        return string.IsNullOrWhiteSpace(generator.Fuel)
            ? Array.Empty<string>()
            : new[] { generator.Fuel };
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

    private sealed record PlanRow(
        int ComfortGeneratorCount,
        int ComfortSpareEuPerTick,
        double MinimumFuelPerDay,
        double SevenDayFuelReserve,
        double FourteenDayFuelReserve,
        string MinimumSetup,
        string ComfortSetup,
        string CablePlan,
        string FuelReserveTarget,
        string Pressure,
        string Recommendation);
}
