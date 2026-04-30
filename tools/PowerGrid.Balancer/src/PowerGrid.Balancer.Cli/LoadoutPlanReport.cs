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

    public static string RenderConsole(BalanceConfig config, LoadoutPlan loadout)
    {
        return RenderMarkdown(Analyze(config, loadout));
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

        int batteryCapacity = 0;
        if (!string.IsNullOrWhiteSpace(loadout.Battery) && loadout.BatteryCount > 0)
        {
            BatteryDefinition battery = Resolve(config.Batteries, loadout.Battery, "battery");
            batteryCapacity = battery.CapacityEu * loadout.BatteryCount;
        }

        FuelPlan fuel = AnalyzeFuel(config, generator, comfortGenerators, Math.Max(1, loadout.ReserveDays));

        Dictionary<string, int> upfrontCost = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string machine, int count) in loadout.PoweredMachines)
            AddExpandedRecipeCost(config, upfrontCost, machine, count);

        AddExpandedRecipeCost(config, upfrontCost, generator.Name, comfortGenerators);

        if (!string.IsNullOrWhiteSpace(loadout.Battery) && loadout.BatteryCount > 0)
            AddExpandedRecipeCost(config, upfrontCost, loadout.Battery, loadout.BatteryCount);

        if (loadout.CableCount > 0)
            AddExpandedRecipeCost(config, upfrontCost, cable.Name, loadout.CableCount);

        if (loadout.ConduitCount > 0)
            AddExpandedRecipeCost(config, upfrontCost, "Power Conduit", loadout.ConduitCount);

        foreach ((string resource, int count) in fuel.ReserveIngredientCost)
            Add(upfrontCost, resource, count);

        Dictionary<string, int> gaps = CalculateGaps(loadout.Stockpile, upfrontCost);
        List<string> recommendations = BuildRecommendations(loadout, demand, minimumGenerators, comfortGenerators, cableZones, fuel, gaps);

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
            batteryCapacity,
            fuel,
            upfrontCost,
            gaps,
            recommendations);
    }

    private static FuelPlan AnalyzeFuel(BalanceConfig config, GeneratorDefinition generator, int generatorCount, int reserveDays)
    {
        if (generatorCount <= 0 || string.IsNullOrWhiteSpace(generator.Fuel))
            return new FuelPlan("", 0, 0, reserveDays, 0, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

        FuelDefinition fuel = Resolve(config.Fuels, generator.Fuel, "fuel");
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
        LoadoutPlan loadout,
        int demand,
        int minimumGenerators,
        int comfortGenerators,
        int cableZones,
        FuelPlan fuel,
        IReadOnlyDictionary<string, int> gaps)
    {
        List<string> recommendations = new();
        if (demand <= 0)
            recommendations.Add("Add powered machines to this loadout before evaluating power.");
        if (comfortGenerators > minimumGenerators)
            recommendations.Add($"Build {comfortGenerators} generator(s) for comfort, or {minimumGenerators} if resources are tight.");
        if (cableZones > 1)
            recommendations.Add($"Split this setup into {cableZones} network zone(s), or use a stronger cable tier when available.");
        if (!string.IsNullOrWhiteSpace(fuel.Name))
            recommendations.Add($"Keep about {fuel.ReserveUnits} {fuel.Name} for a {fuel.ReserveDays}-day reserve.");
        if (gaps.Count > 0 && loadout.Stockpile.Count > 0)
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
            $"| Cable plan | {analysis.CableZonesNeeded} zone(s) using {analysis.Cable} ({analysis.CableThroughputEuPerTick} EU/tick) |",
            $"| Battery buffer | {analysis.BatteryCapacityEu} EU |",
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
        csv.AppendLine(CsvLine("Name", "DemandEuPerTick", "Generator", "MinimumGenerators", "ComfortGenerators", "ComfortSpareEuPerTick", "Cable", "CableZones", "BatteryCapacityEu", "Fuel", "FuelPerDay", "ReserveDays", "ReserveUnits", "Recommendations"));
        csv.AppendLine(CsvLine(
            analysis.Loadout.Name,
            analysis.DemandEuPerTick,
            analysis.Generator,
            analysis.MinimumGeneratorCount,
            analysis.ComfortGeneratorCount,
            analysis.ComfortSpareEuPerTick,
            analysis.Cable,
            analysis.CableZonesNeeded,
            analysis.BatteryCapacityEu,
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

    private static string FormatFuelReserve(FuelPlan fuel)
    {
        if (string.IsNullOrWhiteSpace(fuel.Name))
            return "passive";

        string ingredients = fuel.ReserveIngredientCost.Count == 0
            ? ""
            : " (" + string.Join(", ", fuel.ReserveIngredientCost.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key} x{pair.Value}")) + ")";
        return $"{Decimal(fuel.UnitsPerDay)} {fuel.Name}/day; {fuel.ReserveUnits} {fuel.Name} for {fuel.ReserveDays} days{ingredients}";
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
        int BatteryCapacityEu,
        FuelPlan Fuel,
        IReadOnlyDictionary<string, int> UpfrontAndReserveCost,
        IReadOnlyDictionary<string, int> ResourceGaps,
        IReadOnlyList<string> Recommendations);

    private sealed record FuelPlan(
        string Name,
        double UnitsPerDay,
        int ReserveUnits,
        int ReserveDays,
        int TicksPerUnit,
        IReadOnlyDictionary<string, int> ReserveIngredientCost);
}
