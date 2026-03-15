using DarthMods.API.Power;
using StardewValley;

namespace Darth.PowerGrid.Core;

internal sealed class PowerQueryService
{
    private readonly PowerManager powerManager;
    private readonly BatteryStateManager batteryState;
    private readonly FuelManager fuelManager;

    public PowerQueryService(PowerManager powerManager, BatteryStateManager batteryState, FuelManager fuelManager)
    {
        this.powerManager = powerManager;
        this.batteryState = batteryState;
        this.fuelManager = fuelManager;
    }

    public IReadOnlyList<PowerNetworkSnapshot> GetNetworkSnapshots(string? locationName = null)
    {
        var context = powerManager.BuildQueryContext();
        if (context.Networks.Count == 0)
            return Array.Empty<PowerNetworkSnapshot>();

        var snapshots = new List<PowerNetworkSnapshot>();
        foreach (PowerNetwork network in context.Networks)
        {
            string[] involvedLocations = GetInvolvedLocationNames(network);
            if (!MatchesLocationFilter(involvedLocations, locationName))
                continue;

            TickReport? report = FindReport(network, involvedLocations);

            int totalStoredEu = 0;
            foreach (PowerNode battery in network.Batteries)
                totalStoredEu += batteryState.GetCharge(battery);

            snapshots.Add(new PowerNetworkSnapshot
            {
                NetworkId = network.NetworkId,
                LocationNames = involvedLocations,
                CableCount = network.Cables.Count,
                GeneratorCount = network.Generators.Count,
                BatteryCount = network.Batteries.Count,
                ConsumerCount = network.Consumers.Count,
                ConduitCount = network.Conduits.Count,
                CableThroughputCap = network.MinCableThroughput == int.MaxValue ? 0 : network.MinCableThroughput,
                TotalGenerationPerTick = network.TotalGenerationPerTick(),
                TotalDemandPerTick = network.TotalDemandPerTick(),
                TotalStoredEU = totalStoredEu,
                TotalBatteryCapacity = network.TotalBatteryCapacity(),
                LastTickGenerated = report?.TotalGenerated ?? 0,
                LastTickConsumed = report?.TotalConsumed ?? 0,
                LastTickFromBatteries = report?.TotalFromBatteries ?? 0,
                LastTickStoredInBatteries = report?.TotalStoredInBatteries ?? 0
            });
        }

        return snapshots;
    }

    public IReadOnlyList<PowerConsumerSnapshot> GetConsumerSnapshots(string? locationName = null)
    {
        var context = powerManager.BuildQueryContext();
        if (context.Networks.Count == 0)
            return Array.Empty<PowerConsumerSnapshot>();

        var snapshots = new List<PowerConsumerSnapshot>();
        foreach (PowerNetwork network in context.Networks)
        {
            string[] involvedLocations = GetInvolvedLocationNames(network);
            if (!MatchesLocationFilter(involvedLocations, locationName))
                continue;

            TickReport? report = FindReport(network, involvedLocations);
            Dictionary<string, AllocationResult> allocations = BuildAllocationLookup(report);

            foreach (PowerNode consumer in network.Consumers)
            {
                if (!context.Locations.TryGetValue(consumer.LocationName, out GameLocation? location))
                    continue;

                if (!MatchesLocationFilter(consumer.LocationName, locationName))
                    continue;

                StardewValley.Object? obj = location.getObjectAtTile((int)consumer.Tile.X, (int)consumer.Tile.Y);
                allocations.TryGetValue(consumer.UniqueKey, out AllocationResult? allocation);

                snapshots.Add(new PowerConsumerSnapshot
                {
                    NetworkId = network.NetworkId,
                    LocationName = consumer.LocationName,
                    TileX = (int)consumer.Tile.X,
                    TileY = (int)consumer.Tile.Y,
                    ItemId = consumer.ItemId,
                    DisplayName = GetConsumerDisplayName(obj, consumer),
                    IsProcessing = obj?.MinutesUntilReady > 0,
                    IsPowered = (allocation?.EUAllocated ?? 0) > 0,
                    DemandPerTick = consumer.DemandPerTick,
                    EUAllocated = allocation?.EUAllocated ?? 0,
                    SpeedupFraction = allocation?.SpeedupFraction ?? 0f,
                    MinutesAccelerated = allocation?.MinutesAccelerated ?? 0,
                    MinutesRemaining = allocation?.MinutesRemaining ?? (obj?.MinutesUntilReady ?? 0),
                    MaxSpeedupFraction = consumer.MaxSpeedupFraction,
                    Priority = consumer.Priority
                });
            }
        }

        return snapshots;
    }

    public IReadOnlyList<PowerGeneratorSnapshot> GetGeneratorSnapshots(string? locationName = null)
    {
        var context = powerManager.BuildQueryContext();
        if (context.Networks.Count == 0)
            return Array.Empty<PowerGeneratorSnapshot>();

        var snapshots = new List<PowerGeneratorSnapshot>();
        foreach (PowerNetwork network in context.Networks)
        {
            string[] involvedLocations = GetInvolvedLocationNames(network);
            if (!MatchesLocationFilter(involvedLocations, locationName))
                continue;

            TickReport? report = FindReport(network, involvedLocations);

            foreach (PowerNode generator in network.Generators)
            {
                if (!context.Locations.TryGetValue(generator.LocationName, out GameLocation? location))
                    continue;

                if (!MatchesLocationFilter(generator.LocationName, locationName))
                    continue;

                StardewValley.Object? obj = location.getObjectAtTile((int)generator.Tile.X, (int)generator.Tile.Y);
                int generatedThisTick = GetGeneratedThisTick(report, generator.UniqueKey);
                int fuelTicksRemaining = generator.RequiresFuel ? fuelManager.GetFuelTicksRemaining(generator.UniqueKey) : -1;

                snapshots.Add(new PowerGeneratorSnapshot
                {
                    NetworkId = network.NetworkId,
                    LocationName = generator.LocationName,
                    TileX = (int)generator.Tile.X,
                    TileY = (int)generator.Tile.Y,
                    ItemId = generator.ItemId,
                    DisplayName = obj?.DisplayName ?? generator.ItemId,
                    GenerationPerTick = generator.GenerationPerTick,
                    GeneratedThisTick = generatedThisTick,
                    RequiresFuel = generator.RequiresFuel,
                    FuelTicksRemaining = fuelTicksRemaining,
                    IsOnline = generatedThisTick > 0
                });
            }
        }

        return snapshots;
    }

    public IReadOnlyList<PowerBatterySnapshot> GetBatterySnapshots(string? locationName = null)
    {
        var context = powerManager.BuildQueryContext();
        if (context.Networks.Count == 0)
            return Array.Empty<PowerBatterySnapshot>();

        var snapshots = new List<PowerBatterySnapshot>();
        foreach (PowerNetwork network in context.Networks)
        {
            string[] involvedLocations = GetInvolvedLocationNames(network);
            if (!MatchesLocationFilter(involvedLocations, locationName))
                continue;

            TickReport? report = FindReport(network, involvedLocations);

            foreach (PowerNode battery in network.Batteries)
            {
                if (!context.Locations.TryGetValue(battery.LocationName, out GameLocation? location))
                    continue;

                if (!MatchesLocationFilter(battery.LocationName, locationName))
                    continue;

                StardewValley.Object? obj = location.getObjectAtTile((int)battery.Tile.X, (int)battery.Tile.Y);
                int charge = batteryState.GetCharge(battery);

                snapshots.Add(new PowerBatterySnapshot
                {
                    NetworkId = network.NetworkId,
                    LocationName = battery.LocationName,
                    TileX = (int)battery.Tile.X,
                    TileY = (int)battery.Tile.Y,
                    ItemId = battery.ItemId,
                    DisplayName = obj?.DisplayName ?? battery.ItemId,
                    Charge = charge,
                    Capacity = battery.Capacity,
                    ChargePercent = battery.Capacity > 0 ? (float)charge / battery.Capacity : 0f,
                    DrainedThisTick = GetBatteryDrain(report, battery.UniqueKey),
                    StoredThisTick = GetBatteryStored(report, battery.UniqueKey)
                });
            }
        }

        return snapshots;
    }

    private TickReport? FindReport(PowerNetwork network, string[] involvedLocations)
    {
        foreach (string locationName in involvedLocations)
        {
            TickReport? report = powerManager.GetLastReports(locationName).FirstOrDefault(p => p.NetworkId == network.NetworkId);
            if (report != null)
                return report;
        }

        return null;
    }

    private static Dictionary<string, AllocationResult> BuildAllocationLookup(TickReport? report)
    {
        var allocations = new Dictionary<string, AllocationResult>(StringComparer.Ordinal);
        if (report == null)
            return allocations;

        foreach (AllocationResult allocation in report.Allocations)
            allocations[allocation.Consumer.UniqueKey] = allocation;

        return allocations;
    }

    private static int GetGeneratedThisTick(TickReport? report, string generatorKey)
    {
        return report != null && report.GeneratorOutputByKey.TryGetValue(generatorKey, out int generated)
            ? generated
            : 0;
    }

    private static int GetBatteryDrain(TickReport? report, string batteryKey)
    {
        return report != null && report.BatteryDrainByKey.TryGetValue(batteryKey, out int drained)
            ? drained
            : 0;
    }

    private static int GetBatteryStored(TickReport? report, string batteryKey)
    {
        return report != null && report.BatteryStoredByKey.TryGetValue(batteryKey, out int stored)
            ? stored
            : 0;
    }

    private static string GetConsumerDisplayName(StardewValley.Object? obj, PowerNode consumer)
    {
        if (obj != null && !string.IsNullOrWhiteSpace(obj.DisplayName))
            return obj.DisplayName;

        ConsumerDefinition? definition = ConsumerRegistry.Instance.GetConsumerDef(PowerConstants.Q(consumer.ItemId));
        if (!string.IsNullOrWhiteSpace(definition?.DisplayName))
            return definition.DisplayName;

        return consumer.ItemId;
    }

    private static string[] GetInvolvedLocationNames(PowerNetwork network)
    {
        return network.Cables.Select(p => p.LocationName)
            .Concat(network.Generators.Select(p => p.LocationName))
            .Concat(network.Batteries.Select(p => p.LocationName))
            .Concat(network.Consumers.Select(p => p.LocationName))
            .Concat(network.Conduits.Select(p => p.LocationName))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool MatchesLocationFilter(IEnumerable<string> locationNames, string? locationName)
    {
        return string.IsNullOrWhiteSpace(locationName)
            || locationNames.Any(name => string.Equals(name, locationName, StringComparison.Ordinal));
    }

    private static bool MatchesLocationFilter(string candidate, string? locationName)
    {
        return string.IsNullOrWhiteSpace(locationName)
            || string.Equals(candidate, locationName, StringComparison.Ordinal);
    }
}
