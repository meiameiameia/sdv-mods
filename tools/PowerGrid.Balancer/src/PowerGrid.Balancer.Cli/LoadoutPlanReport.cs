using System.Globalization;
using System.Text;
using PowerGrid.Balancer.Core;

internal static class LoadoutPlanReport
{
    public static void Write(string outputPath, BalanceConfig config, LoadoutPlan loadout)
    {
        Directory.CreateDirectory(outputPath);
        PlanAnalysis analysis = Analyze(config, loadout);

        File.WriteAllText(Path.Combine(outputPath, "loadout-plan.md"), RenderMarkdown(analysis));
        File.WriteAllText(Path.Combine(outputPath, "loadout-plan.csv"), RenderCsv(analysis));
        File.WriteAllText(Path.Combine(outputPath, "loadout-resource-gaps.csv"), RenderResourceGapsCsv(analysis));
    }

    public static void WriteIndex(string outputPath, IReadOnlyList<PlanSummary> summaries)
    {
        Directory.CreateDirectory(outputPath);
        File.WriteAllText(Path.Combine(outputPath, "loadout-index.md"), RenderIndexMarkdown(summaries));
        File.WriteAllText(Path.Combine(outputPath, "loadout-index.csv"), RenderIndexCsv(summaries));
    }

    public static string RenderConsole(BalanceConfig config, LoadoutPlan loadout)
    {
        return RenderMarkdown(Analyze(config, loadout));
    }

    public static PlanSummary Summarize(BalanceConfig config, LoadoutPlan loadout, string reportPath)
    {
        PlanAnalysis analysis = Analyze(config, loadout);
        string topGaps = analysis.ResourceGaps.Count == 0
            ? ""
            : string.Join("; ", analysis.ResourceGaps
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .Select(pair => $"{pair.Key} x{pair.Value}"));

        return new PlanSummary(
            analysis.Loadout.Name,
            reportPath,
            analysis.DemandEuPerTick,
            analysis.Generator,
            analysis.MinimumGeneratorCount,
            analysis.ComfortGeneratorCount,
            analysis.ComfortSpareEuPerTick,
            analysis.Cable,
            analysis.CableZonesNeeded,
            analysis.Fuel.Name,
            analysis.Fuel.UnitsPerDay,
            analysis.Fuel.ReserveUnits,
            analysis.ResourceGaps.Count,
            topGaps,
            analysis.Recommendations.FirstOrDefault() ?? "");
    }

    public static ResourceSnapshot AnalyzeResources(BalanceConfig config, LoadoutPlan loadout)
    {
        PlanAnalysis analysis = Analyze(config, loadout);
        return new ResourceSnapshot(
            analysis.Loadout.Name,
            analysis.DemandEuPerTick,
            analysis.UpfrontAndReserveCost,
            analysis.CostBySource,
            analysis.Loadout.Stockpile,
            analysis.ResourceGaps);
    }

    private static PlanAnalysis Analyze(BalanceConfig config, LoadoutPlan loadout)
    {
        int demand = 0;
        Dictionary<string, int> machineDemand = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string id, int count) in loadout.PoweredMachines)
        {
            MachineDefinition machine = Resolve(config.Machines, id, "machine");
            int lineDemand = machine.DemandEuPerTick * Math.Max(0, count);
            machineDemand[id] = lineDemand;
            demand += lineDemand;
        }

        GeneratorDefinition generator = Resolve(config.Generators, loadout.Generator, "generator");
        CableDefinition cable = Resolve(config.Cables, loadout.Cable, "cable");
        int minimumGenerators = generator.OutputEuPerTick <= 0 ? 0 : (int)Math.Ceiling((double)demand / generator.OutputEuPerTick);
        double headroom = Math.Clamp(loadout.ComfortHeadroom, 0d, 2d);
        int comfortGenerators = generator.OutputEuPerTick <= 0
            ? 0
            : Math.Max(minimumGenerators, (int)Math.Ceiling(demand * (1d + headroom) / generator.OutputEuPerTick));
        int minimumGeneration = minimumGenerators * generator.OutputEuPerTick;
        int comfortGeneration = comfortGenerators * generator.OutputEuPerTick;
        int comfortSpare = Math.Max(0, comfortGeneration - demand);
        int cableZones = cable.ThroughputEuPerTick <= 0 ? 0 : (int)Math.Ceiling((double)demand / cable.ThroughputEuPerTick);
        int plannedThroughput = cableZones * cable.ThroughputEuPerTick;
        int cableHeadroom = Math.Max(0, plannedThroughput - demand);

        int batteryCapacity = 0;
        if (!string.IsNullOrWhiteSpace(loadout.Battery) && loadout.BatteryCount > 0)
        {
            BatteryDefinition battery = Resolve(config.Batteries, loadout.Battery, "battery");
            batteryCapacity = battery.CapacityEu * loadout.BatteryCount;
        }

        double batteryFillMinutes = comfortSpare <= 0 || batteryCapacity <= 0
            ? 0d
            : (double)batteryCapacity / comfortSpare * config.TickMinutes;
        double batteryRuntimeMinutes = demand <= 0 || batteryCapacity <= 0
            ? 0d
            : (double)batteryCapacity / demand * config.TickMinutes;

        FuelPlan fuel = AnalyzeFuel(config, generator, comfortGenerators, Math.Max(1, loadout.ReserveDays), loadout.Stockpile);

        Dictionary<string, int> upfrontCost = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Dictionary<string, int>> costBySource = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string machine, int count) in loadout.PoweredMachines)
            AddExpandedRecipeCostBySource(config, upfrontCost, costBySource, "Machines", machine, count);

        AddExpandedRecipeCostBySource(config, upfrontCost, costBySource, "Generators", generator.Name, comfortGenerators);

        if (!string.IsNullOrWhiteSpace(loadout.Battery) && loadout.BatteryCount > 0)
            AddExpandedRecipeCostBySource(config, upfrontCost, costBySource, "Batteries", loadout.Battery, loadout.BatteryCount);

        if (loadout.CableCount > 0)
            AddExpandedRecipeCostBySource(config, upfrontCost, costBySource, "Cables", cable.Name, loadout.CableCount);

        if (loadout.ConduitCount > 0)
            AddExpandedRecipeCostBySource(config, upfrontCost, costBySource, "Conduits", "Power Conduit", loadout.ConduitCount);

        foreach ((string resource, int count) in fuel.ReserveIngredientCost)
            AddResourceCostBySource(upfrontCost, costBySource, "Fuel Reserve", resource, count);

        Dictionary<string, int> gaps = CalculateGaps(loadout.Stockpile, upfrontCost);
        List<string> recommendations = BuildRecommendations(config.TickMinutes, demand, generator.OutputEuPerTick, minimumGenerators, comfortGenerators, comfortSpare, cableZones, batteryCapacity, batteryFillMinutes, batteryRuntimeMinutes, fuel, gaps, loadout.Stockpile.Count > 0);

        return new PlanAnalysis(
            loadout,
            demand,
            machineDemand,
            generator.Name,
            generator.OutputEuPerTick,
            minimumGenerators,
            minimumGeneration,
            comfortGenerators,
            comfortGeneration,
            comfortSpare,
            cable.Name,
            cable.ThroughputEuPerTick,
            cableZones,
            plannedThroughput,
            cableHeadroom,
            batteryCapacity,
            batteryFillMinutes,
            batteryRuntimeMinutes,
            fuel,
            upfrontCost,
            costBySource,
            gaps,
            recommendations);
    }

    private static FuelPlan AnalyzeFuel(BalanceConfig config, GeneratorDefinition generator, int generatorCount, int reserveDays, IReadOnlyDictionary<string, int> stockpile)
    {
        IReadOnlyList<string> fuelOptions = GetFuelOptions(generator);
        if (generatorCount <= 0 || fuelOptions.Count == 0)
            return new FuelPlan("", 0, 0, reserveDays, 0, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        if (fuelOptions.Count > 1)
            return AnalyzeFlexibleFuel(config, generator, fuelOptions, generatorCount, reserveDays, stockpile);

        FuelDefinition fuel = Resolve(config.Fuels, fuelOptions[0], "fuel");
        double fuelPerDay = (double)(generatorCount * config.TicksPerDay) / Math.Max(1, fuel.TicksPerUnit);
        int reserveUnits = (int)Math.Ceiling(fuelPerDay * reserveDays);
        Dictionary<string, int> reserveIngredientCost = new(StringComparer.OrdinalIgnoreCase);

        if (config.Recipes.TryGetValue(fuel.Name, out RecipeDefinition? recipe) && recipe.Ingredients.Count > 0)
        {
            int batches = (int)Math.Ceiling((double)reserveUnits / Math.Max(1, recipe.OutputCount));
            foreach ((string ingredient, int ingredientCount) in recipe.Ingredients)
                reserveIngredientCost[ingredient] = ingredientCount * batches;
        }
        else
        {
            reserveIngredientCost[fuel.Name] = reserveUnits;
        }

        return new FuelPlan(fuel.Name, fuelPerDay, reserveUnits, reserveDays, fuel.TicksPerUnit, reserveIngredientCost);
    }

    private static FuelPlan AnalyzeFlexibleFuel(
        BalanceConfig config,
        GeneratorDefinition generator,
        IReadOnlyList<string> fuelOptions,
        int generatorCount,
        int reserveDays,
        IReadOnlyDictionary<string, int> stockpile)
    {
        FuelDefinition primaryFuel = Resolve(config.Fuels, fuelOptions[0], "fuel");
        int ticksNeededPerDay = generatorCount * config.TicksPerDay;
        int reserveTicks = ticksNeededPerDay * reserveDays;
        Dictionary<string, int> reserveIngredientCost = AllocateFlexibleFuelReserve(config, fuelOptions, reserveTicks, stockpile);

        double primaryEquivalentPerDay = (double)ticksNeededPerDay / Math.Max(1, primaryFuel.TicksPerUnit);
        int primaryEquivalentReserve = (int)Math.Ceiling((double)reserveTicks / Math.Max(1, primaryFuel.TicksPerUnit));
        string name = $"{generator.Name} fuel ({primaryFuel.Name}-equivalent)";

        return new FuelPlan(name, primaryEquivalentPerDay, primaryEquivalentReserve, reserveDays, primaryFuel.TicksPerUnit, reserveIngredientCost);
    }

    private static Dictionary<string, int> AllocateFlexibleFuelReserve(
        BalanceConfig config,
        IReadOnlyList<string> fuelOptions,
        int reserveTicks,
        IReadOnlyDictionary<string, int> stockpile)
    {
        Dictionary<string, int> cost = new(StringComparer.OrdinalIgnoreCase);
        int remainingTicks = Math.Max(0, reserveTicks);

        foreach (string fuelId in fuelOptions)
        {
            if (remainingTicks <= 0)
                break;

            FuelDefinition fuel = Resolve(config.Fuels, fuelId, "fuel");
            stockpile.TryGetValue(fuel.Name, out int availableUnits);
            int unitsNeeded = (int)Math.Ceiling((double)remainingTicks / Math.Max(1, fuel.TicksPerUnit));
            int unitsUsed = Math.Min(Math.Max(0, availableUnits), unitsNeeded);
            if (unitsUsed <= 0)
                continue;

            cost[fuel.Name] = unitsUsed;
            remainingTicks -= unitsUsed * Math.Max(1, fuel.TicksPerUnit);
        }

        if (remainingTicks > 0)
        {
            FuelDefinition fallback = Resolve(config.Fuels, fuelOptions[0], "fuel");
            int missingUnits = (int)Math.Ceiling((double)remainingTicks / Math.Max(1, fallback.TicksPerUnit));
            cost.TryGetValue(fallback.Name, out int current);
            cost[fallback.Name] = current + missingUnits;
        }

        return cost;
    }

    private static Dictionary<string, int> CalculateGaps(
        IReadOnlyDictionary<string, int> stockpile,
        IReadOnlyDictionary<string, int> upfrontCost)
    {
        Dictionary<string, int> gaps = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string resource, int need) in upfrontCost)
        {
            stockpile.TryGetValue(resource, out int available);
            int gap = need - available;
            if (gap > 0)
                gaps[resource] = gap;
        }

        return gaps;
    }

    private static List<string> BuildRecommendations(
        int tickMinutes,
        int demand,
        int generatorOutput,
        int minimumGenerators,
        int comfortGenerators,
        int comfortSpare,
        int cableZones,
        int batteryCapacity,
        double batteryFillMinutes,
        double batteryRuntimeMinutes,
        FuelPlan fuel,
        IReadOnlyDictionary<string, int> gaps,
        bool hasStockpile)
    {
        List<string> recommendations = new();
        if (demand <= 0)
            recommendations.Add("Add powered machines to this loadout before evaluating power.");
        if (comfortGenerators > minimumGenerators)
            recommendations.Add($"Build {comfortGenerators} generator(s) for comfort, or {minimumGenerators} if resources are tight.");
        if (comfortSpare > 0 && batteryCapacity <= 0)
            recommendations.Add("Add battery storage if you want to capture surplus instead of wasting spare generation.");
        if (comfortSpare > 0 && batteryCapacity > 0 && batteryFillMinutes < tickMinutes * 120)
            recommendations.Add($"Battery storage fills after about {FormatDuration(batteryFillMinutes)} at comfort output; after that, extra generation is mostly waste unless demand rises.");
        if (comfortSpare >= generatorOutput && minimumGenerators > 0)
            recommendations.Add("Comfort generation has at least one full generator of spare output; consider fewer generators unless this setup is meant to grow soon.");
        if (batteryRuntimeMinutes > 0 && batteryRuntimeMinutes < 60)
            recommendations.Add($"Battery buffer only covers about {FormatDuration(batteryRuntimeMinutes)} at full demand, so it is a small smoothing buffer rather than a long reserve.");
        if (cableZones > 1)
            recommendations.Add($"Split this setup into {cableZones} network zone(s), or use a stronger cable tier when available.");
        if (!string.IsNullOrWhiteSpace(fuel.Name))
            recommendations.Add($"Keep about {fuel.ReserveUnits} {fuel.Name} for a {fuel.ReserveDays}-day reserve.");
        if (gaps.Count > 0 && hasStockpile)
            recommendations.Add("Current stockpile is short on: " + string.Join(", ", gaps.OrderByDescending(pair => pair.Value).Take(5).Select(pair => $"{pair.Key} x{pair.Value}")));
        if (recommendations.Count == 0)
            recommendations.Add("This loadout looks comfortable under the configured assumptions.");

        return recommendations;
    }

    private static string RenderMarkdown(PlanAnalysis analysis)
    {
        List<string> lines = new()
        {
            $"# {analysis.Loadout.Name}",
            "",
            analysis.Loadout.Description,
            "",
            "## Power Plan",
            "",
            $"| Demand | {analysis.DemandEuPerTick} EU/tick |",
            $"| Minimum generation | {analysis.MinimumGeneratorCount}x {analysis.Generator} ({analysis.MinimumGenerationEuPerTick} EU/tick) |",
            $"| Comfort generation | {analysis.ComfortGeneratorCount}x {analysis.Generator} ({analysis.ComfortGenerationEuPerTick} EU/tick, +{analysis.ComfortSpareEuPerTick} spare) |",
            $"| Cable plan | {analysis.CableZonesNeeded} zone(s) using {analysis.Cable} ({analysis.CableThroughputEuPerTick} EU/tick each, {analysis.PlannedThroughputEuPerTick} EU/tick planned throughput, +{analysis.CableHeadroomEuPerTick} headroom) |",
            $"| Battery buffer | {analysis.BatteryCapacityEu} EU |",
            $"| Battery fill estimate | {FormatDurationOrNone(analysis.BatteryFillMinutes)} at comfort surplus |",
            $"| Battery runtime estimate | {FormatDurationOrNone(analysis.BatteryRuntimeMinutes)} at full demand |",
            $"| Fuel reserve | {FormatFuelReserve(analysis.Fuel)} |",
            "",
            "## Machines",
            "",
            "| Machine | Count | Demand |",
            "| --- | ---: | ---: |"
        };

        foreach ((string machine, int count) in analysis.Loadout.PoweredMachines.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            lines.Add($"| {EscapeMarkdown(machine)} | {count} | {MachineDemand(analysis, machine)} EU/tick |");

        lines.Add("");
        lines.Add("## Resource Check");
        lines.Add("");
        lines.Add("| Resource | Needed | Stockpile | Gap |");
        lines.Add("| --- | ---: | ---: | ---: |");

        foreach ((string resource, int need) in analysis.UpfrontAndReserveCost.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            analysis.Loadout.Stockpile.TryGetValue(resource, out int stock);
            analysis.ResourceGaps.TryGetValue(resource, out int gap);
            lines.Add($"| {EscapeMarkdown(resource)} | {need} | {stock} | {gap} |");
        }

        lines.Add("");
        lines.Add("## Recommendations");
        lines.Add("");
        foreach (string recommendation in analysis.Recommendations)
            lines.Add($"- {recommendation}");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderCsv(PlanAnalysis analysis)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Name", "DemandEuPerTick", "Generator", "MinimumGenerators", "ComfortGenerators", "ComfortSpareEuPerTick", "Cable", "CableZones", "PlannedThroughputEuPerTick", "CableHeadroomEuPerTick", "BatteryCapacityEu", "BatteryFillMinutes", "BatteryRuntimeMinutes", "Fuel", "FuelPerDay", "ReserveDays", "ReserveUnits", "Recommendations"));
        csv.AppendLine(CsvLine(
            analysis.Loadout.Name,
            analysis.DemandEuPerTick,
            analysis.Generator,
            analysis.MinimumGeneratorCount,
            analysis.ComfortGeneratorCount,
            analysis.ComfortSpareEuPerTick,
            analysis.Cable,
            analysis.CableZonesNeeded,
            analysis.PlannedThroughputEuPerTick,
            analysis.CableHeadroomEuPerTick,
            analysis.BatteryCapacityEu,
            Decimal(analysis.BatteryFillMinutes),
            Decimal(analysis.BatteryRuntimeMinutes),
            analysis.Fuel.Name,
            Decimal(analysis.Fuel.UnitsPerDay),
            analysis.Fuel.ReserveDays,
            analysis.Fuel.ReserveUnits,
            string.Join(" | ", analysis.Recommendations)));
        return csv.ToString();
    }

    private static string RenderResourceGapsCsv(PlanAnalysis analysis)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Resource", "Needed", "Stockpile", "Gap"));
        foreach ((string resource, int need) in analysis.UpfrontAndReserveCost.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            analysis.Loadout.Stockpile.TryGetValue(resource, out int stock);
            analysis.ResourceGaps.TryGetValue(resource, out int gap);
            csv.AppendLine(CsvLine(resource, need, stock, gap));
        }

        return csv.ToString();
    }

    private static string RenderIndexMarkdown(IReadOnlyList<PlanSummary> summaries)
    {
        List<string> lines = new()
        {
            "# PowerGrid Loadout Plans",
            "",
            "| Loadout | Demand | Generation | Cable zones | Fuel reserve | Resource gaps | Main recommendation |",
            "| --- | ---: | --- | ---: | --- | --- | --- |"
        };

        foreach (PlanSummary summary in summaries)
        {
            string fuel = string.IsNullOrWhiteSpace(summary.Fuel)
                ? "passive"
                : $"{Decimal(summary.FuelPerDay)} {summary.Fuel}/day; {summary.ReserveUnits} reserve";
            string gaps = summary.ResourceGapTypes <= 0 ? "none" : summary.TopResourceGaps;
            lines.Add($"| [{EscapeMarkdown(summary.Name)}]({summary.ReportPath.Replace("\\", "/", StringComparison.Ordinal)}/loadout-plan.md) | {summary.DemandEuPerTick} | {summary.MinimumGenerators}-{summary.ComfortGenerators}x {EscapeMarkdown(summary.Generator)} (+{summary.ComfortSpareEuPerTick} spare) | {summary.CableZones} | {EscapeMarkdown(fuel)} | {EscapeMarkdown(gaps)} | {EscapeMarkdown(summary.MainRecommendation)} |");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderIndexCsv(IReadOnlyList<PlanSummary> summaries)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Name", "ReportPath", "DemandEuPerTick", "Generator", "MinimumGenerators", "ComfortGenerators", "ComfortSpareEuPerTick", "Cable", "CableZones", "Fuel", "FuelPerDay", "ReserveUnits", "ResourceGapTypes", "TopResourceGaps", "MainRecommendation"));
        foreach (PlanSummary summary in summaries)
        {
            csv.AppendLine(CsvLine(
                summary.Name,
                summary.ReportPath,
                summary.DemandEuPerTick,
                summary.Generator,
                summary.MinimumGenerators,
                summary.ComfortGenerators,
                summary.ComfortSpareEuPerTick,
                summary.Cable,
                summary.CableZones,
                summary.Fuel,
                Decimal(summary.FuelPerDay),
                summary.ReserveUnits,
                summary.ResourceGapTypes,
                summary.TopResourceGaps,
                summary.MainRecommendation));
        }

        return csv.ToString();
    }

    private static string FormatFuelReserve(FuelPlan fuel)
    {
        if (string.IsNullOrWhiteSpace(fuel.Name))
            return "passive";

        string ingredients = fuel.ReserveIngredientCost.Count == 0
            ? ""
            : " (" + string.Join(", ", fuel.ReserveIngredientCost.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key} x{pair.Value}")) + ")";
        return $"{Decimal(fuel.UnitsPerDay)} {fuel.Name}/day; {fuel.ReserveUnits} {fuel.Name} for {fuel.ReserveDays} days{ingredients}";
    }

    private static string FormatDurationOrNone(double minutes)
    {
        return minutes <= 0 ? "none" : FormatDuration(minutes);
    }

    private static string FormatDuration(double minutes)
    {
        if (minutes < 60)
            return $"{Decimal(minutes)}m";

        double hours = minutes / 60d;
        if (hours < 24)
            return $"{Decimal(hours)}h";

        return $"{Decimal(hours / 24d)}d";
    }

    private static int MachineDemand(PlanAnalysis analysis, string machine)
    {
        return analysis.MachineDemandEuPerTick.TryGetValue(machine, out int demand)
            ? demand
            : 0;
    }

    private static void AddExpandedRecipeCost(BalanceConfig config, Dictionary<string, int> cost, string item, int count)
    {
        if (count <= 0)
            return;

        AddExpandedRecipeCost(config, cost, item, count, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static void AddExpandedRecipeCostBySource(
        BalanceConfig config,
        Dictionary<string, int> totalCost,
        Dictionary<string, Dictionary<string, int>> costBySource,
        string source,
        string item,
        int count)
    {
        Dictionary<string, int> sourceCost = new(StringComparer.OrdinalIgnoreCase);
        AddExpandedRecipeCost(config, sourceCost, item, count);
        foreach ((string resource, int amount) in sourceCost)
            AddResourceCostBySource(totalCost, costBySource, source, resource, amount);
    }

    private static void AddResourceCostBySource(
        Dictionary<string, int> totalCost,
        Dictionary<string, Dictionary<string, int>> costBySource,
        string source,
        string resource,
        int amount)
    {
        Add(totalCost, resource, amount);
        if (!costBySource.TryGetValue(source, out Dictionary<string, int>? sourceCost))
        {
            sourceCost = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            costBySource[source] = sourceCost;
        }

        Add(sourceCost, resource, amount);
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

    private sealed record PlanAnalysis(
        LoadoutPlan Loadout,
        int DemandEuPerTick,
        IReadOnlyDictionary<string, int> MachineDemandEuPerTick,
        string Generator,
        int GeneratorOutputEuPerTick,
        int MinimumGeneratorCount,
        int MinimumGenerationEuPerTick,
        int ComfortGeneratorCount,
        int ComfortGenerationEuPerTick,
        int ComfortSpareEuPerTick,
        string Cable,
        int CableThroughputEuPerTick,
        int CableZonesNeeded,
        int PlannedThroughputEuPerTick,
        int CableHeadroomEuPerTick,
        int BatteryCapacityEu,
        double BatteryFillMinutes,
        double BatteryRuntimeMinutes,
        FuelPlan Fuel,
        IReadOnlyDictionary<string, int> UpfrontAndReserveCost,
        IReadOnlyDictionary<string, Dictionary<string, int>> CostBySource,
        IReadOnlyDictionary<string, int> ResourceGaps,
        IReadOnlyList<string> Recommendations);

    private sealed record FuelPlan(
        string Name,
        double UnitsPerDay,
        int ReserveUnits,
        int ReserveDays,
        int TicksPerUnit,
        IReadOnlyDictionary<string, int> ReserveIngredientCost);

    public sealed record PlanSummary(
        string Name,
        string ReportPath,
        int DemandEuPerTick,
        string Generator,
        int MinimumGenerators,
        int ComfortGenerators,
        int ComfortSpareEuPerTick,
        string Cable,
        int CableZones,
        string Fuel,
        double FuelPerDay,
        int ReserveUnits,
        int ResourceGapTypes,
        string TopResourceGaps,
        string MainRecommendation);

    public sealed record ResourceSnapshot(
        string LoadoutName,
        int DemandEuPerTick,
        IReadOnlyDictionary<string, int> Needed,
        IReadOnlyDictionary<string, Dictionary<string, int>> CostBySource,
        IReadOnlyDictionary<string, int> Stockpile,
        IReadOnlyDictionary<string, int> Gaps);
}
