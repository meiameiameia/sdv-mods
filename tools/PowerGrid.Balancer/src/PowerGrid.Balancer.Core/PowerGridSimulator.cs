namespace PowerGrid.Balancer.Core;

public sealed class PowerGridSimulator
{
    public SimulationResult Simulate(BalanceConfig config, Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(scenario);

        int ticks = Math.Max(1, scenario.Days) * Math.Max(1, config.TicksPerDay);
        int demandPerTick = CalculateDemandPerTick(config, scenario);
        int rawGenerationPerTick = CalculateGenerationPerTick(config, scenario);
        int cableThroughput = ResolveCable(config, scenario.Cable).ThroughputEuPerTick;
        int batteryCapacity = CalculateBatteryCapacity(config, scenario);
        int batteryCharge = Clamp((int)MathF.Round(batteryCapacity * scenario.InitialBatteryChargePercent), 0, batteryCapacity);

        Dictionary<string, FuelRuntime> fuels = BuildFuelRuntime(config, scenario);

        int totalDemand = 0;
        int totalGenerated = 0;
        int totalConsumed = 0;
        int totalUnmet = 0;
        int totalWasted = 0;
        int totalThroughputLimited = 0;
        int totalBatteryOverflow = 0;
        int totalBatteryCharged = 0;
        int totalBatteryDrained = 0;
        float coverageSum = 0f;

        for (int tick = 0; tick < ticks; tick++)
        {
            if (tick > 0 && tick % config.TicksPerDay == 0 && batteryCharge > 0 && config.BatteryDailyLeakPercent > 0)
                batteryCharge = Math.Max(0, batteryCharge - (int)MathF.Ceiling(batteryCharge * (config.BatteryDailyLeakPercent / 100f)));

            int generated = GenerateForTick(config, scenario, fuels);
            int generationAfterThroughput = Math.Min(generated, cableThroughput);
            int throughputLimited = Math.Max(0, generated - generationAfterThroughput);
            int demand = Math.Min(demandPerTick, cableThroughput);

            totalDemand += demandPerTick;
            totalGenerated += generated;
            totalThroughputLimited += throughputLimited;

            if (generationAfterThroughput >= demand)
            {
                int consumed = demand;
                int excess = generationAfterThroughput - demand;
                int charged = Math.Min(excess, batteryCapacity - batteryCharge);
                int batteryOverflow = Math.Max(0, excess - charged);
                int unmetFromThroughput = Math.Max(0, demandPerTick - consumed);
                batteryCharge += charged;

                totalConsumed += consumed;
                totalUnmet += unmetFromThroughput;
                totalWasted += throughputLimited + batteryOverflow;
                totalBatteryOverflow += batteryOverflow;
                totalBatteryCharged += charged;
                coverageSum += demandPerTick <= 0 ? 1f : (float)consumed / demandPerTick;
                continue;
            }

            int deficit = demand - generationAfterThroughput;
            int drained = Math.Min(deficit, batteryCharge);
            batteryCharge -= drained;

            int supplied = generationAfterThroughput + drained;
            int unmet = Math.Max(0, demandPerTick - supplied);

            totalConsumed += supplied;
            totalBatteryDrained += drained;
            totalWasted += throughputLimited;
            totalUnmet += unmet;
            coverageSum += demandPerTick <= 0 ? 1f : Math.Clamp((float)supplied / demandPerTick, 0f, 1f);
        }

        float averageCoverage = ticks <= 0 ? 1f : coverageSum / ticks;
        float weightedMaxBonus = CalculateWeightedMaxSpeedBonus(config, scenario);
        bool throughputBottleneck = demandPerTick > cableThroughput || rawGenerationPerTick > cableThroughput;

        List<FuelUseResult> fuelUse = fuels.Values
            .Select(fuel => new FuelUseResult
            {
                Fuel = fuel.Id,
                UnitsAvailable = fuel.UnitsAvailable,
                UnitsConsumed = fuel.UnitsConsumed,
                TicksConsumed = fuel.TicksConsumed,
                TicksShort = fuel.TicksShort
            })
            .OrderBy(result => result.Fuel, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int usefulGenerated = Math.Clamp(totalConsumed - totalBatteryDrained + totalBatteryCharged, 0, totalGenerated);
        int storableSurplus = totalBatteryCharged + totalBatteryOverflow;

        List<string> warnings = BuildWarnings(demandPerTick, rawGenerationPerTick, cableThroughput, batteryCapacity, totalUnmet, totalWasted, throughputBottleneck, fuelUse);
        List<string> recommendations = BuildRecommendations(demandPerTick, rawGenerationPerTick, cableThroughput, batteryCapacity, totalUnmet, totalWasted, throughputBottleneck, fuelUse);

        return new SimulationResult
        {
            ScenarioName = scenario.Name,
            Days = scenario.Days,
            TicksSimulated = ticks,
            DemandEuPerTick = demandPerTick,
            GenerationEuPerTick = rawGenerationPerTick,
            CableThroughputEuPerTick = cableThroughput,
            BatteryCapacityEu = batteryCapacity,
            FinalBatteryChargeEu = batteryCharge,
            TotalDemandEu = totalDemand,
            TotalGeneratedEu = totalGenerated,
            TotalConsumedEu = totalConsumed,
            TotalUnmetEu = totalUnmet,
            TotalWastedEu = totalWasted,
            TotalThroughputLimitedEu = totalThroughputLimited,
            TotalBatteryOverflowEu = totalBatteryOverflow,
            TotalBatteryChargedEu = totalBatteryCharged,
            TotalBatteryDrainedEu = totalBatteryDrained,
            AveragePowerCoverage = averageCoverage,
            AverageSpeedBonus = weightedMaxBonus * averageCoverage,
            SurplusRatio = totalDemand <= 0 ? 0f : (float)(totalGenerated - totalDemand) / totalDemand,
            GenerationUseRate = totalGenerated <= 0 ? 1f : (float)usefulGenerated / totalGenerated,
            SurplusCaptureRate = storableSurplus <= 0 ? 0f : (float)totalBatteryCharged / storableSurplus,
            BatteryUtilization = batteryCapacity <= 0 ? 0f : (float)totalBatteryDrained / batteryCapacity,
            HasThroughputBottleneck = throughputBottleneck,
            FuelUse = fuelUse,
            Warnings = warnings,
            Recommendations = recommendations
        };
    }

    private static int CalculateDemandPerTick(BalanceConfig config, Scenario scenario)
    {
        double demand = 0;
        foreach (MachineLoad load in scenario.Machines)
        {
            MachineDefinition machine = ResolveMachine(config, load.Id);
            demand += machine.DemandEuPerTick * Math.Max(0, load.Count) * Math.Clamp(load.ActiveFraction, 0f, 1f);
        }

        return (int)Math.Ceiling(demand);
    }

    private static int CalculateGenerationPerTick(BalanceConfig config, Scenario scenario)
    {
        double output = 0;
        foreach (GeneratorLoad load in scenario.Generators)
        {
            GeneratorDefinition generator = ResolveGenerator(config, load.Id);
            output += generator.OutputEuPerTick * Math.Max(0, load.Count) * WeatherMultiplier(generator, scenario.Weather);
        }

        return (int)Math.Round(output);
    }

    private static int CalculateBatteryCapacity(BalanceConfig config, Scenario scenario)
    {
        int capacity = 0;
        foreach (BatteryLoad load in scenario.Batteries)
        {
            BatteryDefinition battery = ResolveBattery(config, load.Id);
            capacity += battery.CapacityEu * Math.Max(0, load.Count);
        }

        return capacity;
    }

    private static float CalculateWeightedMaxSpeedBonus(BalanceConfig config, Scenario scenario)
    {
        double weightedBonus = 0;
        double totalDemand = 0;

        foreach (MachineLoad load in scenario.Machines)
        {
            MachineDefinition machine = ResolveMachine(config, load.Id);
            double demand = machine.DemandEuPerTick * Math.Max(0, load.Count) * Math.Clamp(load.ActiveFraction, 0f, 1f);
            weightedBonus += demand * machine.MaxSpeedBonus;
            totalDemand += demand;
        }

        return totalDemand <= 0 ? 0f : (float)(weightedBonus / totalDemand);
    }

    private static Dictionary<string, FuelRuntime> BuildFuelRuntime(BalanceConfig config, Scenario scenario)
    {
        Dictionary<string, int> generatorFuelTicksNeeded = new(StringComparer.OrdinalIgnoreCase);

        foreach (GeneratorLoad load in scenario.Generators)
        {
            GeneratorDefinition generator = ResolveGenerator(config, load.Id);
            IReadOnlyList<string> fuelOptions = GetFuelOptions(generator);
            if (fuelOptions.Count == 0)
                continue;

            foreach (string fuelId in fuelOptions)
            {
                generatorFuelTicksNeeded.TryGetValue(fuelId, out int current);
                generatorFuelTicksNeeded[fuelId] = current;
            }
        }

        Dictionary<string, FuelRuntime> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in generatorFuelTicksNeeded)
        {
            FuelDefinition fuel = ResolveFuel(config, pair.Key);
            scenario.FuelInventory.TryGetValue(pair.Key, out int unitsAvailable);
            int availableTicks = Math.Max(0, unitsAvailable) * fuel.TicksPerUnit;

            result[pair.Key] = new FuelRuntime(pair.Key, Math.Max(0, unitsAvailable), availableTicks, fuel.TicksPerUnit);
        }

        return result;
    }

    private static int GenerateForTick(BalanceConfig config, Scenario scenario, Dictionary<string, FuelRuntime> fuels)
    {
        double output = 0;
        foreach (GeneratorLoad load in scenario.Generators)
        {
            GeneratorDefinition generator = ResolveGenerator(config, load.Id);
            int count = Math.Max(0, load.Count);
            if (count == 0)
                continue;

            IReadOnlyList<string> fuelOptions = GetFuelOptions(generator);
            if (fuelOptions.Count > 0)
            {
                int fueledGenerators = ConsumeFuelTicks(config, fuels, generator, fuelOptions, count);
                if (fueledGenerators <= 0)
                    continue;

                output += generator.OutputEuPerTick * fueledGenerators;
                continue;
            }

            output += generator.OutputEuPerTick * count * WeatherMultiplier(generator, scenario.Weather);
        }

        return (int)Math.Round(output);
    }

    private static int ConsumeFuelTicks(BalanceConfig config, Dictionary<string, FuelRuntime> fuels, GeneratorDefinition generator, IReadOnlyList<string> fuelOptions, int requested)
    {
        int remaining = requested;
        int fueled = 0;

        foreach (string fuelId in fuelOptions)
        {
            if (!fuels.TryGetValue(fuelId, out FuelRuntime? fuel))
            {
                FuelDefinition definition = ResolveFuel(config, fuelId);
                fuel = new FuelRuntime(fuelId, 0, 0, definition.TicksPerUnit);
                fuels[fuelId] = fuel;
            }

            int consumed = fuel.ConsumeTicks(remaining);
            fueled += consumed;
            remaining -= consumed;
            if (remaining <= 0)
                break;
        }

        if (remaining > 0)
        {
            string shortageId = fuelOptions.Count == 1
                ? fuelOptions[0]
                : $"{generator.Name} fuel";
            if (!fuels.TryGetValue(shortageId, out FuelRuntime? shortage))
            {
                shortage = new FuelRuntime(shortageId, 0, 0, 1);
                fuels[shortageId] = shortage;
            }

            shortage.AddShort(remaining);
        }

        return fueled;
    }

    private static double WeatherMultiplier(GeneratorDefinition generator, WeatherProfile weather)
    {
        if (!generator.WeatherAdjusted)
            return 1;

        return (weather.NormalizedClear * 1.0)
            + (weather.NormalizedRain * 1.5)
            + (weather.NormalizedStorm * 1.5)
            + (weather.NormalizedSnow * 0.7);
    }

    private static List<string> BuildWarnings(
        int demandPerTick,
        int generationPerTick,
        int cableThroughput,
        int batteryCapacity,
        int totalUnmet,
        int totalWasted,
        bool throughputBottleneck,
        IReadOnlyList<FuelUseResult> fuelUse)
    {
        List<string> warnings = new();

        if (demandPerTick == 0)
            warnings.Add("No active machine demand in this scenario.");
        if (generationPerTick < demandPerTick)
            warnings.Add("Generation is lower than demand; this setup depends on batteries or will stall.");
        if (throughputBottleneck)
            warnings.Add("Cable throughput is lower than either generation or demand.");
        if (batteryCapacity == 0 && generationPerTick < demandPerTick)
            warnings.Add("No battery storage is available to cover generator shortfalls.");
        if (batteryCapacity == 0 && generationPerTick > demandPerTick)
            warnings.Add("No battery storage is available to capture surplus generation.");
        if (totalUnmet > 0)
            warnings.Add("The simulation had unmet EU demand.");
        if (totalWasted > 0 && generationPerTick > demandPerTick)
            warnings.Add("Some generated EU is wasted after machine demand and battery storage.");
        foreach (FuelUseResult fuel in fuelUse.Where(fuel => fuel.TicksShort > 0))
            warnings.Add($"{fuel.Fuel} runs short by {fuel.TicksShort} generator-ticks.");

        return warnings;
    }

    private static List<string> BuildRecommendations(
        int demandPerTick,
        int generationPerTick,
        int cableThroughput,
        int batteryCapacity,
        int totalUnmet,
        int totalWasted,
        bool throughputBottleneck,
        IReadOnlyList<FuelUseResult> fuelUse)
    {
        List<string> recommendations = new();

        if (throughputBottleneck)
            recommendations.Add("Try a higher cable tier or split the setup into multiple networks.");
        if (generationPerTick < demandPerTick)
            recommendations.Add($"Add about {demandPerTick - generationPerTick} EU/tick of generation for full coverage.");
        if (batteryCapacity == 0)
            recommendations.Add("Add at least one battery if the setup relies on wind or limited fuel.");
        if (totalWasted > 0 && batteryCapacity == 0)
            recommendations.Add("Add battery storage to capture surplus EU instead of wasting generator output.");
        if (totalWasted > 0 && batteryCapacity > 0)
            recommendations.Add("Add more battery capacity or reduce generator count if surplus waste is higher than intended.");
        if (totalUnmet == 0 && demandPerTick > 0)
            recommendations.Add("This setup is stable under the configured assumptions.");
        foreach (FuelUseResult fuel in fuelUse.Where(fuel => fuel.TicksShort > 0))
        {
            int units = (int)Math.Ceiling((double)fuel.TicksShort / Math.Max(1, fuel.TicksConsumed / Math.Max(1, fuel.UnitsConsumed)));
            recommendations.Add($"Add more {fuel.Fuel} or reduce fueled generator runtime.");
        }

        return recommendations;
    }

    private static MachineDefinition ResolveMachine(BalanceConfig config, string id)
    {
        return config.Machines.TryGetValue(id, out MachineDefinition? machine)
            ? machine
            : throw new InvalidOperationException($"Unknown machine '{id}'.");
    }

    private static GeneratorDefinition ResolveGenerator(BalanceConfig config, string id)
    {
        return config.Generators.TryGetValue(id, out GeneratorDefinition? generator)
            ? generator
            : throw new InvalidOperationException($"Unknown generator '{id}'.");
    }

    private static BatteryDefinition ResolveBattery(BalanceConfig config, string id)
    {
        return config.Batteries.TryGetValue(id, out BatteryDefinition? battery)
            ? battery
            : throw new InvalidOperationException($"Unknown battery '{id}'.");
    }

    private static CableDefinition ResolveCable(BalanceConfig config, string id)
    {
        return config.Cables.TryGetValue(id, out CableDefinition? cable)
            ? cable
            : throw new InvalidOperationException($"Unknown cable '{id}'.");
    }

    private static FuelDefinition ResolveFuel(BalanceConfig config, string id)
    {
        return config.Fuels.TryGetValue(id, out FuelDefinition? fuel)
            ? fuel
            : throw new InvalidOperationException($"Unknown fuel '{id}'.");
    }

    private static IReadOnlyList<string> GetFuelOptions(GeneratorDefinition generator)
    {
        if (generator.FuelOptions.Count > 0)
            return generator.FuelOptions.Where(fuel => !string.IsNullOrWhiteSpace(fuel)).ToList();

        return string.IsNullOrWhiteSpace(generator.Fuel)
            ? Array.Empty<string>()
            : new[] { generator.Fuel };
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private sealed class FuelRuntime
    {
        private int ticksAvailable;
        private readonly int ticksPerUnit;

        public FuelRuntime(string id, int unitsAvailable, int ticksAvailable, int ticksPerUnit)
        {
            Id = id;
            UnitsAvailable = unitsAvailable;
            this.ticksAvailable = ticksAvailable;
            this.ticksPerUnit = Math.Max(1, ticksPerUnit);
        }

        public string Id { get; }
        public int UnitsAvailable { get; }
        public int TicksConsumed { get; private set; }
        public int TicksShort { get; private set; }
        public int UnitsConsumed => (int)Math.Ceiling((double)TicksConsumed / ticksPerUnit);

        public int ConsumeTicks(int requested)
        {
            int consumed = Math.Min(requested, ticksAvailable);
            ticksAvailable -= consumed;
            TicksConsumed += consumed;
            return consumed;
        }

        public void AddShort(int ticks)
        {
            TicksShort += Math.Max(0, ticks);
        }
    }
}
