using Microsoft.Xna.Framework;
using System.Globalization;
using StardewModdingAPI;
using StardewValley;

namespace Meiameiameia.PowerGrid.Core;

internal sealed class AllocationResult
{
    public PowerNode Consumer { get; init; } = null!;
    public int EUAllocated { get; init; }
    public int EUDemanded { get; init; }
    public float SpeedupFraction { get; init; }
    public int MinutesAccelerated { get; init; }
    public int MinutesRemaining { get; init; }
}

internal sealed class TickReport
{
    public string LocationName { get; set; } = "";
    public int NetworkId { get; set; }
    public int TotalGenerated { get; set; }
    public int TotalFromBatteries { get; set; }
    public int TotalStoredInBatteries { get; set; }
    public int TotalConsumed { get; set; }
    public int CableThroughputCap { get; set; }
    public List<AllocationResult> Allocations { get; } = new();
    public Dictionary<string, int> GeneratorOutputByKey { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> BatteryDrainByKey { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> BatteryStoredByKey { get; } = new(StringComparer.Ordinal);
}

internal sealed class PowerManager
{
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly GraphBuilder graphBuilder;
    private readonly BatteryStateManager batteryState;
    private readonly FuelManager fuelManager;

    // Cached networks per location, rebuilt on object change
    private readonly Dictionary<string, List<PowerNetwork>> networkCache = new();
    private readonly HashSet<string> dirtyLocations = new();

    // Last tick reports for UI
    private readonly Dictionary<string, List<TickReport>> lastReports = new();
    private readonly HashSet<string> trackedMetadataTileKeys = new(StringComparer.Ordinal);

    private const string MdPrefix = "meiameiameia.PowerGrid/";
    private const string MdType = MdPrefix + "type";
    private const string MdConnected = MdPrefix + "connected";
    private const string MdNetworkId = MdPrefix + "networkId";

    private const string MdCableTier = MdPrefix + "cableTier";
    private const string MdThroughputCap = MdPrefix + "throughputCap";
    private const string MdConnectionMask = MdPrefix + "connectionMask";
    private const string MdConnectedSides = MdPrefix + "connectedSides";

    private const string MdEuPerTick = MdPrefix + "euPerTick";
    private const string MdGeneratedThisTick = MdPrefix + "generatedThisTick";
    private const string MdRequiresFuel = MdPrefix + "requiresFuel";
    private const string MdFuelTicksRemaining = MdPrefix + "fuelTicksRemaining";
    private const string MdOnline = MdPrefix + "online";

    private const string MdCharge = MdPrefix + "charge";
    private const string MdCapacity = MdPrefix + "capacity";
    private const string MdChargePercent = MdPrefix + "chargePercent";
    private const string MdDrainedThisTick = MdPrefix + "drainedThisTick";
    private const string MdStoredThisTick = MdPrefix + "storedThisTick";

    private const string MdLinked = MdPrefix + "linked";
    private const string MdPartnerLocation = MdPrefix + "partnerLocation";
    private const string MdPartnerTile = MdPrefix + "partnerTile";

    private const string MdPowered = MdPrefix + "powered";
    private const string MdEnergized = MdPrefix + "energized";
    private const string MdEuAllocated = MdPrefix + "euAllocated";
    private const string MdEuDemanded = MdPrefix + "euDemanded";
    private const string MdSpeedup = MdPrefix + "speedupFraction";
    private const string MdMinutesAccelerated = MdPrefix + "minutesAccelerated";
    private const string MdMinutesRemaining = MdPrefix + "minutesRemaining";
    private const string MdBonusMinutesCarry = MdPrefix + "bonusMinutesCarry";
    private const string MdLastTickTime = MdPrefix + "lastTickTime";

    private static readonly string[] AllPowerMetadataKeys = new[]
    {
        MdType, MdConnected, MdNetworkId,
        MdCableTier, MdThroughputCap, MdConnectionMask, MdConnectedSides,
        MdEuPerTick, MdGeneratedThisTick, MdRequiresFuel, MdFuelTicksRemaining, MdOnline,
        MdCharge, MdCapacity, MdChargePercent, MdDrainedThisTick, MdStoredThisTick,
        MdLinked, MdPartnerLocation, MdPartnerTile,
        MdPowered, MdEnergized, MdEuAllocated, MdEuDemanded, MdSpeedup, MdMinutesAccelerated, MdMinutesRemaining, MdBonusMinutesCarry, MdLastTickTime
    };

    private ConduitManager? conduitMgr;

    public PowerManager(IMonitor monitor, ModConfig config, BatteryStateManager batteryState, FuelManager fuelManager)
    {
        this.monitor = monitor;
        this.config = config;
        this.graphBuilder = new GraphBuilder(monitor, config);
        this.batteryState = batteryState;
        this.fuelManager = fuelManager;
    }

    public void SetConduitManager(ConduitManager mgr) => conduitMgr = mgr;

    public void MarkDirty(string locationName)
    {
        dirtyLocations.Add(locationName);
    }

    public void MarkAllDirty()
    {
        networkCache.Clear();
        dirtyLocations.Clear();
    }

    public void ResetRuntimeState()
    {
        networkCache.Clear();
        dirtyLocations.Clear();
        lastReports.Clear();
        trackedMetadataTileKeys.Clear();
    }

    public List<PowerNetwork> GetNetworks(GameLocation location)
    {
        string locName = location.NameOrUniqueName;

        if (dirtyLocations.Contains(locName))
        {
            networkCache.Remove(locName);
            dirtyLocations.Remove(locName);
        }

        if (!networkCache.TryGetValue(locName, out List<PowerNetwork>? networks))
        {
            networks = graphBuilder.BuildNetworks(location);
            networkCache[locName] = networks;
        }

        return networks;
    }

    public List<TickReport> GetLastReports(string locationName)
    {
        return lastReports.TryGetValue(locationName, out var reports) ? reports : new List<TickReport>();
    }

    public AllocationResult? GetLastAllocationAtTile(string locationName, Vector2 tile)
    {
        if (!lastReports.TryGetValue(locationName, out List<TickReport>? reports))
            return null;

        foreach (TickReport report in reports)
        {
            AllocationResult? allocation = report.Allocations.FirstOrDefault(a =>
                a.Consumer.LocationName == locationName && a.Consumer.Tile == tile);
            if (allocation != null)
                return allocation;
        }

        return null;
    }

    public QueryContext BuildQueryContext()
    {
        var allLocations = new Dictionary<string, GameLocation>();
        if (!Context.IsWorldReady)
            return new QueryContext(allLocations, new List<PowerNetwork>());

        var allNetworks = new List<(string LocName, PowerNetwork Network)>();

        foreach (GameLocation location in EnumerateUniqueLoadedLocations())
            CollectLocationNetworks(location, allLocations, allNetworks);

        List<PowerNetwork> mergedNetworks = MergeConduitLinkedNetworks(allNetworks);
        return new QueryContext(allLocations, mergedNetworks);
    }

    public void SimulateTick()
    {
        if (!Context.IsWorldReady)
            return;

        lastReports.Clear();

        // Step 1: Gather all locations and build their local networks
        var allLocations = new Dictionary<string, GameLocation>();
        var allNetworks = new List<(string LocName, PowerNetwork Network)>();

        foreach (GameLocation location in EnumerateUniqueLoadedLocations())
            CollectLocationNetworks(location, allLocations, allNetworks);

        if (allNetworks.Count == 0)
            return;

        var seenMetadataTileKeys = new HashSet<string>(StringComparer.Ordinal);

        // Step 2: Merge networks connected by conduit links
        List<PowerNetwork> mergedNetworks = MergeConduitLinkedNetworks(allNetworks);

        // Step 3: Simulate each (possibly merged) network
        foreach (PowerNetwork network in mergedNetworks)
        {
            TickReport report = SimulateNetwork(network, allLocations, seenMetadataTileKeys);

            // File report under each location that has nodes in this network
            var involvedLocations = new HashSet<string>();
            foreach (var n in network.Cables) involvedLocations.Add(n.LocationName);
            foreach (var n in network.Generators) involvedLocations.Add(n.LocationName);
            foreach (var n in network.Batteries) involvedLocations.Add(n.LocationName);
            foreach (var n in network.Consumers) involvedLocations.Add(n.LocationName);
            foreach (var n in network.Conduits) involvedLocations.Add(n.LocationName);

            foreach (string locName in involvedLocations)
            {
                if (!lastReports.TryGetValue(locName, out var reports))
                {
                    reports = new List<TickReport>();
                    lastReports[locName] = reports;
                }
                reports.Add(report);
            }
        }

        AnnotateStandalonePowerObjects(allLocations, seenMetadataTileKeys);
        ClearStaleMetadata(allLocations, seenMetadataTileKeys);
        trackedMetadataTileKeys.Clear();
        foreach (string key in seenMetadataTileKeys)
            trackedMetadataTileKeys.Add(key);
    }

    private void CollectLocationNetworks(GameLocation location,
        Dictionary<string, GameLocation> allLocations,
        List<(string, PowerNetwork)> allNetworks)
    {
        string locName = location.NameOrUniqueName;
        allLocations[locName] = location;

        List<PowerNetwork> networks = GetNetworks(location);
        foreach (var net in networks)
            allNetworks.Add((locName, net));
    }

    private static IEnumerable<GameLocation> EnumerateUniqueLoadedLocations()
    {
        var seenLocationNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (GameLocation location in Game1.locations)
        {
            if (seenLocationNames.Add(location.NameOrUniqueName))
                yield return location;

            if (location.buildings == null)
                continue;

            foreach (var building in location.buildings)
            {
                GameLocation? interior = building.GetIndoors();
                if (interior != null && seenLocationNames.Add(interior.NameOrUniqueName))
                    yield return interior;
            }
        }
    }

    private List<PowerNetwork> MergeConduitLinkedNetworks(List<(string LocName, PowerNetwork Network)> allNetworks)
    {
        if (conduitMgr == null || conduitMgr.GetAllLinks().Count == 0)
            return allNetworks.Select(x => x.Network).ToList();

        // Build a lookup: (locationName, tile) -> network index
        var conduitToNetIndex = new Dictionary<string, int>();
        for (int i = 0; i < allNetworks.Count; i++)
        {
            foreach (var conduit in allNetworks[i].Network.Conduits)
            {
                string key = $"{conduit.LocationName}|{conduit.Tile.X}|{conduit.Tile.Y}";
                conduitToNetIndex[key] = i;
            }
        }

        // Union-Find to merge linked networks
        int[] parent = new int[allNetworks.Count];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;

        int Find(int x) {
            while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
            return x;
        }
        void Union(int a, int b) {
            int ra = Find(a), rb = Find(b);
            if (ra != rb) parent[ra] = rb;
        }

        foreach (var link in conduitMgr.GetAllLinks())
        {
            string keyA = $"{link.LocationA}|{link.TileA.X}|{link.TileA.Y}";
            string keyB = $"{link.LocationB}|{link.TileB.X}|{link.TileB.Y}";

            if (conduitToNetIndex.TryGetValue(keyA, out int idxA) &&
                conduitToNetIndex.TryGetValue(keyB, out int idxB))
            {
                Union(idxA, idxB);
            }
        }

        // Group networks by root
        var groups = new Dictionary<int, List<int>>();
        for (int i = 0; i < allNetworks.Count; i++)
        {
            int root = Find(i);
            if (!groups.TryGetValue(root, out var list))
            {
                list = new List<int>();
                groups[root] = list;
            }
            list.Add(i);
        }

        // Merge grouped networks
        var result = new List<PowerNetwork>();
        int newId = 0;
        foreach (var group in groups.Values)
        {
            if (group.Count == 1)
            {
                var net = allNetworks[group[0]].Network;
                net.NetworkId = newId++;
                result.Add(net);
            }
            else
            {
                var merged = new PowerNetwork { NetworkId = newId++ };
                foreach (int idx in group)
                {
                    PowerNetwork src = allNetworks[idx].Network;
                    foreach (var n in src.Cables) merged.AddNode(n);
                    foreach (var n in src.Generators) merged.AddNode(n);
                    foreach (var n in src.Batteries) merged.AddNode(n);
                    foreach (var n in src.Consumers) merged.AddNode(n);
                    foreach (var n in src.Conduits) merged.AddNode(n);
                }
                result.Add(merged);
            }
        }

        return result;
    }

    private TickReport SimulateNetwork(PowerNetwork network, Dictionary<string, GameLocation> locations, HashSet<string> seenMetadataTileKeys)
    {
        // Use first location name for report, but this is a cross-location network
        string reportLoc = network.Generators.Count > 0 ? network.Generators[0].LocationName
            : network.Cables.Count > 0 ? network.Cables[0].LocationName
            : "";

        var report = new TickReport
        {
            LocationName = reportLoc,
            NetworkId = network.NetworkId,
            CableThroughputCap = network.MinCableThroughput == int.MaxValue ? 0 : network.MinCableThroughput
        };

        // Step 1: Generate EU (consume fuel for fuel-based generators)
        int generated = 0;
        var generatorOutputByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (PowerNode gen in network.Generators)
        {
            if (!locations.TryGetValue(gen.LocationName, out GameLocation? genLoc))
                continue;

            if (gen.RequiresFuel)
            {
                if (fuelManager.TryConsumeFuel(genLoc, gen))
                {
                    generated += gen.GenerationPerTick;
                    generatorOutputByKey[gen.UniqueKey] = gen.GenerationPerTick;
                }
                else
                {
                    generatorOutputByKey[gen.UniqueKey] = 0;
                }
            }
            else
            {
                int output = CalculatePassiveGeneration(gen, genLoc);
                generated += output;
                generatorOutputByKey[gen.UniqueKey] = output;
            }
        }
        report.TotalGenerated = generated;
        foreach (var pair in generatorOutputByKey)
            report.GeneratorOutputByKey[pair.Key] = pair.Value;

        // Step 2: Pool = generated + battery drain
        int pool = generated;

        int throughputCap = network.MinCableThroughput == int.MaxValue
            ? int.MaxValue
            : network.MinCableThroughput;

        // Step 3: Calculate total demand
        var sortedConsumers = new List<PowerNode>(network.Consumers);
        sortedConsumers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        int totalDemand = 0;
        foreach (var c in sortedConsumers)
            totalDemand += c.DemandPerTick;

        // If generation < demand, drain batteries to supplement
        int batteryDrain = 0;
        var batteryDrainByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        if (pool < totalDemand)
        {
            int deficit = totalDemand - pool;
            foreach (PowerNode bat in network.Batteries)
            {
                int drained = batteryState.DrainCharge(bat, deficit - batteryDrain);
                if (drained > 0)
                    batteryDrainByKey[bat.UniqueKey] = drained;
                batteryDrain += drained;
                if (batteryDrain >= deficit)
                    break;
            }
            pool += batteryDrain;
        }
        report.TotalFromBatteries = batteryDrain;
        foreach (var pair in batteryDrainByKey)
            report.BatteryDrainByKey[pair.Key] = pair.Value;

        // Apply throughput cap
        int availableEU = Math.Min(pool, throughputCap);

        // Step 4: Allocate EU to consumers deterministically by priority
        int totalConsumed = 0;
        var allocationsByConsumerKey = new Dictionary<string, AllocationResult>(StringComparer.Ordinal);
        bool networkEnergized = availableEU > 0;

        foreach (PowerNode consumer in sortedConsumers)
        {
            // Look up the machine in its own location
            if (!locations.TryGetValue(consumer.LocationName, out GameLocation? consumerLoc))
                continue;

            StardewValley.Object? machineObj = GetObjectAt(consumerLoc, consumer.Tile);
            string tileKey = MakeTileKey(consumer.LocationName, consumer.Tile);
            seenMetadataTileKeys.Add(tileKey);

            if (machineObj == null || !IsMachineProcessing(machineObj))
            {
                var allocation = new AllocationResult
                {
                    Consumer = consumer,
                    EUAllocated = 0,
                    EUDemanded = consumer.DemandPerTick,
                    SpeedupFraction = 0f,
                    MinutesAccelerated = 0,
                    MinutesRemaining = machineObj?.MinutesUntilReady ?? 0
                };

                report.Allocations.Add(allocation);
                allocationsByConsumerKey[consumer.UniqueKey] = allocation;
                if (machineObj != null)
                    SetConsumerMetadata(machineObj, network.NetworkId, allocation, networkEnergized);
                continue;
            }

            int allocated = availableEU > 0 ? Math.Min(consumer.DemandPerTick, availableEU) : 0;
            availableEU -= allocated;
            totalConsumed += allocated;

            float fraction = consumer.DemandPerTick > 0
                ? (float)allocated / consumer.DemandPerTick
                : 0f;
            float speedup = fraction * consumer.MaxSpeedupFraction;

            float bonusMinutes = PowerConstants.TickIntervalMinutes * speedup;
            AccelerateMachine(machineObj, bonusMinutes);
            int minutesAccelerated = (int)MathF.Round(bonusMinutes);

            var activeAllocation = new AllocationResult
            {
                Consumer = consumer,
                EUAllocated = allocated,
                EUDemanded = consumer.DemandPerTick,
                SpeedupFraction = speedup,
                MinutesAccelerated = minutesAccelerated,
                MinutesRemaining = machineObj.MinutesUntilReady
            };

            report.Allocations.Add(activeAllocation);
            allocationsByConsumerKey[consumer.UniqueKey] = activeAllocation;
            SetConsumerMetadata(machineObj, network.NetworkId, activeAllocation, networkEnergized);
        }
        report.TotalConsumed = totalConsumed;

        // Step 5: Store excess in batteries
        int excess = pool - totalConsumed;
        int stored = 0;
        var batteryStoredByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        if (excess > 0)
        {
            foreach (PowerNode bat in network.Batteries)
            {
                int added = batteryState.AddCharge(bat, excess - stored);
                if (added > 0)
                    batteryStoredByKey[bat.UniqueKey] = added;
                stored += added;
                if (stored >= excess)
                    break;
            }
        }
        report.TotalStoredInBatteries = stored;
        foreach (var pair in batteryStoredByKey)
            report.BatteryStoredByKey[pair.Key] = pair.Value;

        AnnotateNetworkNodes(
            network,
            locations,
            seenMetadataTileKeys,
            networkEnergized,
            generatorOutputByKey,
            batteryDrainByKey,
            batteryStoredByKey,
            allocationsByConsumerKey);

        return report;
    }

    public sealed record QueryContext(
        Dictionary<string, GameLocation> Locations,
        List<PowerNetwork> Networks
    );

    private int CalculatePassiveGeneration(PowerNode gen, GameLocation location)
    {
        // Wind generator: weather-based output
        if (gen.ItemId == PowerConstants.WindGeneratorId)
        {
            if (!location.IsOutdoors)
                return 0;

            if (Game1.isRaining || Game1.isLightning)
                return (int)(gen.GenerationPerTick * 1.5f);
            if (Game1.isSnowing)
                return (int)(gen.GenerationPerTick * 0.7f);
            return gen.GenerationPerTick;
        }

        return gen.GenerationPerTick;
    }

    private static StardewValley.Object? GetObjectAt(GameLocation location, Vector2 tile)
    {
        return location.getObjectAtTile((int)tile.X, (int)tile.Y);
    }

    private static bool IsMachineProcessing(StardewValley.Object obj)
    {
        if (obj is StardewValley.Objects.Cask cask)
            return cask.heldObject.Value != null;

        // Many modded machines (including BigCraftable-based ones) may not populate heldObject
        // consistently while processing. MinutesUntilReady is the most reliable signal.
        return obj.MinutesUntilReady > 0;
    }

    private static int AccelerateMachine(StardewValley.Object machine, float bonusMinutes)
    {
        if (machine is StardewValley.Objects.Cask || machine.MinutesUntilReady <= 0 || bonusMinutes <= 0f)
            return 0;

        float carry = 0f;
        if (machine.modData.TryGetValue(MdBonusMinutesCarry, out string? rawCarry))
            float.TryParse(rawCarry, NumberStyles.Float, CultureInfo.InvariantCulture, out carry);

        carry += bonusMinutes;
        int ticksToApply = (int)(carry / PowerConstants.TickIntervalMinutes);
        if (ticksToApply <= 0)
        {
            machine.modData[MdBonusMinutesCarry] = carry.ToString("0.###", CultureInfo.InvariantCulture);
            return 0;
        }

        int requestedMinutes = ticksToApply * PowerConstants.TickIntervalMinutes;
        int appliedMinutes = Math.Min(requestedMinutes, machine.MinutesUntilReady);
        machine.MinutesUntilReady = Math.Max(0, machine.MinutesUntilReady - appliedMinutes);

        carry -= requestedMinutes;
        if (machine.MinutesUntilReady <= 0 || carry <= 0.001f)
            machine.modData.Remove(MdBonusMinutesCarry);
        else
            machine.modData[MdBonusMinutesCarry] = carry.ToString("0.###", CultureInfo.InvariantCulture);

        return appliedMinutes;
    }

    private static string MakeTileKey(string locationName, Vector2 tile)
    {
        return $"{locationName}|{tile.X}|{tile.Y}";
    }

    private static bool TryParseTileKey(string key, out string locationName, out Vector2 tile)
    {
        locationName = "";
        tile = Vector2.Zero;

        string[] parts = key.Split('|');
        if (parts.Length != 3)
            return false;

        if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
            !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            return false;

        locationName = parts[0];
        tile = new Vector2(x, y);
        return true;
    }

    private static string GetConnectedSides(int mask)
    {
        var sides = new List<string>(4);
        if ((mask & 1) != 0) sides.Add("U");
        if ((mask & 2) != 0) sides.Add("R");
        if ((mask & 4) != 0) sides.Add("D");
        if ((mask & 8) != 0) sides.Add("L");
        return sides.Count == 0 ? "-" : string.Join(",", sides);
    }

    private static void SetNetworkMetadata(StardewValley.Object obj, string type, int networkId)
    {
        obj.modData[MdType] = type;
        obj.modData[MdConnected] = "1";
        obj.modData[MdNetworkId] = networkId.ToString(CultureInfo.InvariantCulture);
        obj.modData[MdLastTickTime] = Game1.timeOfDay.ToString(CultureInfo.InvariantCulture);
    }

    private static void SetConsumerMetadata(StardewValley.Object machineObj, int networkId, AllocationResult allocation, bool energized)
    {
        SetNetworkMetadata(machineObj, "Consumer", networkId);
        machineObj.modData[MdPowered] = allocation.EUAllocated > 0 ? "1" : "0";
        machineObj.modData[MdEnergized] = energized ? "1" : "0";
        machineObj.modData[MdEuAllocated] = allocation.EUAllocated.ToString(CultureInfo.InvariantCulture);
        machineObj.modData[MdEuDemanded] = allocation.EUDemanded.ToString(CultureInfo.InvariantCulture);
        machineObj.modData[MdSpeedup] = allocation.SpeedupFraction.ToString("0.####", CultureInfo.InvariantCulture);
        machineObj.modData[MdMinutesAccelerated] = allocation.MinutesAccelerated.ToString(CultureInfo.InvariantCulture);
        machineObj.modData[MdMinutesRemaining] = allocation.MinutesRemaining.ToString(CultureInfo.InvariantCulture);
    }

    private void AnnotateNetworkNodes(
        PowerNetwork network,
        Dictionary<string, GameLocation> locations,
        HashSet<string> seenMetadataTileKeys,
        bool networkEnergized,
        Dictionary<string, int> generatorOutputByKey,
        Dictionary<string, int> batteryDrainByKey,
        Dictionary<string, int> batteryStoredByKey,
        Dictionary<string, AllocationResult> allocationsByConsumerKey)
    {
        foreach (PowerNode cable in network.Cables)
        {
            if (!locations.TryGetValue(cable.LocationName, out GameLocation? location))
                continue;

            StardewValley.Object? obj = GetObjectAt(location, cable.Tile);
            if (obj == null)
                continue;

            seenMetadataTileKeys.Add(MakeTileKey(cable.LocationName, cable.Tile));
            SetNetworkMetadata(obj, "Cable", network.NetworkId);
            obj.modData[MdCableTier] = cable.CableTier.ToString();
            obj.modData[MdThroughputCap] = cable.ThroughputCap.ToString(CultureInfo.InvariantCulture);
            obj.modData[MdConnectionMask] = cable.ConnectionMask.ToString(CultureInfo.InvariantCulture);
            obj.modData[MdConnectedSides] = GetConnectedSides(cable.ConnectionMask);
        }

        foreach (PowerNode generator in network.Generators)
        {
            if (!locations.TryGetValue(generator.LocationName, out GameLocation? location))
                continue;

            StardewValley.Object? obj = GetObjectAt(location, generator.Tile);
            if (obj == null)
                continue;

            seenMetadataTileKeys.Add(MakeTileKey(generator.LocationName, generator.Tile));
            SetNetworkMetadata(obj, "Generator", network.NetworkId);

            int generatedThisTick = generatorOutputByKey.TryGetValue(generator.UniqueKey, out int output) ? output : 0;
            int fuelTicks = generator.RequiresFuel ? fuelManager.GetFuelTicksRemaining(generator.UniqueKey) : -1;

            obj.modData[MdEuPerTick] = generator.GenerationPerTick.ToString(CultureInfo.InvariantCulture);
            obj.modData[MdGeneratedThisTick] = generatedThisTick.ToString(CultureInfo.InvariantCulture);
            obj.modData[MdRequiresFuel] = generator.RequiresFuel ? "1" : "0";
            obj.modData[MdFuelTicksRemaining] = fuelTicks.ToString(CultureInfo.InvariantCulture);
            obj.modData[MdOnline] = generatedThisTick > 0 ? "1" : "0";
        }

        foreach (PowerNode battery in network.Batteries)
        {
            if (!locations.TryGetValue(battery.LocationName, out GameLocation? location))
                continue;

            StardewValley.Object? obj = GetObjectAt(location, battery.Tile);
            if (obj == null)
                continue;

            seenMetadataTileKeys.Add(MakeTileKey(battery.LocationName, battery.Tile));
            SetNetworkMetadata(obj, "Battery", network.NetworkId);

            int charge = batteryState.GetCharge(battery);
            float chargePercent = battery.Capacity > 0 ? (float)charge / battery.Capacity : 0f;
            int drained = batteryDrainByKey.TryGetValue(battery.UniqueKey, out int drainedTick) ? drainedTick : 0;
            int stored = batteryStoredByKey.TryGetValue(battery.UniqueKey, out int storedTick) ? storedTick : 0;

            obj.modData[MdCharge] = charge.ToString(CultureInfo.InvariantCulture);
            obj.modData[MdCapacity] = battery.Capacity.ToString(CultureInfo.InvariantCulture);
            obj.modData[MdChargePercent] = chargePercent.ToString("0.####", CultureInfo.InvariantCulture);
            obj.modData[MdDrainedThisTick] = drained.ToString(CultureInfo.InvariantCulture);
            obj.modData[MdStoredThisTick] = stored.ToString(CultureInfo.InvariantCulture);
        }

        foreach (PowerNode consumer in network.Consumers)
        {
            if (!locations.TryGetValue(consumer.LocationName, out GameLocation? location))
                continue;

            StardewValley.Object? obj = GetObjectAt(location, consumer.Tile);
            if (obj == null)
                continue;

            seenMetadataTileKeys.Add(MakeTileKey(consumer.LocationName, consumer.Tile));

            if (allocationsByConsumerKey.TryGetValue(consumer.UniqueKey, out AllocationResult? allocation))
            {
                SetConsumerMetadata(obj, network.NetworkId, allocation, networkEnergized);
            }
            else
            {
                SetNetworkMetadata(obj, "Consumer", network.NetworkId);
                obj.modData[MdPowered] = "0";
                obj.modData[MdEnergized] = networkEnergized ? "1" : "0";
                obj.modData[MdEuAllocated] = "0";
                obj.modData[MdEuDemanded] = consumer.DemandPerTick.ToString(CultureInfo.InvariantCulture);
                obj.modData[MdSpeedup] = "0";
                obj.modData[MdMinutesAccelerated] = "0";
                obj.modData[MdMinutesRemaining] = obj.MinutesUntilReady.ToString(CultureInfo.InvariantCulture);
            }
        }

        foreach (PowerNode conduit in network.Conduits)
        {
            if (!locations.TryGetValue(conduit.LocationName, out GameLocation? location))
                continue;

            StardewValley.Object? obj = GetObjectAt(location, conduit.Tile);
            if (obj == null)
                continue;

            seenMetadataTileKeys.Add(MakeTileKey(conduit.LocationName, conduit.Tile));
            SetNetworkMetadata(obj, "Conduit", network.NetworkId);

            var partner = conduitMgr?.GetPartner(conduit.LocationName, conduit.Tile);
            obj.modData[MdLinked] = partner == null ? "0" : "1";
            obj.modData[MdPartnerLocation] = partner?.Location ?? "";
            obj.modData[MdPartnerTile] = partner == null
                ? ""
                : $"{partner.Value.Tile.X},{partner.Value.Tile.Y}";
        }
    }

    private void AnnotateStandalonePowerObjects(Dictionary<string, GameLocation> allLocations, HashSet<string> seenMetadataTileKeys)
    {
        foreach (GameLocation location in allLocations.Values)
        {
            foreach (var pair in location.objects.Pairs)
            {
                Vector2 tile = pair.Key;
                StardewValley.Object obj = pair.Value;
                string tileKey = MakeTileKey(location.NameOrUniqueName, tile);

                if (seenMetadataTileKeys.Contains(tileKey))
                    continue;

                if (!IsPowerGridObject(obj))
                    continue;

                seenMetadataTileKeys.Add(tileKey);
                SetNetworkMetadata(obj, "Standalone", -1);
                obj.modData[MdLinked] = (obj.ItemId ?? "") == PowerConstants.PowerConduitId && conduitMgr?.GetPartner(location.NameOrUniqueName, tile) != null ? "1" : "0";
                obj.modData[MdPowered] = "0";
                obj.modData[MdEnergized] = "0";
                obj.modData[MdEuAllocated] = "0";
                obj.modData[MdEuDemanded] = "0";
                obj.modData[MdSpeedup] = "0";
                obj.modData[MdMinutesAccelerated] = "0";
                obj.modData[MdMinutesRemaining] = obj.MinutesUntilReady.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    private static bool IsPowerGridObject(StardewValley.Object obj)
    {
        string itemId = obj.ItemId ?? "";
        return itemId == PowerConstants.CopperCableId ||
               itemId == PowerConstants.IronCableId ||
               itemId == PowerConstants.IridiumCableId ||
               itemId == PowerConstants.EnergizedIridiumCableId ||
               itemId == PowerConstants.SteamGeneratorId ||
               itemId == PowerConstants.CombustionGeneratorId ||
               itemId == PowerConstants.WindGeneratorId ||
               itemId == PowerConstants.BasicBatteryId ||
               itemId == PowerConstants.IridiumBatteryId ||
               itemId == PowerConstants.PowerConduitId;
    }

    private static void ClearPowerMetadata(StardewValley.Object obj)
    {
        foreach (string key in AllPowerMetadataKeys)
            obj.modData.Remove(key);
    }

    private void ClearStaleMetadata(Dictionary<string, GameLocation> allLocations, HashSet<string> seenMetadataTileKeys)
    {
        foreach (string previousKey in trackedMetadataTileKeys)
        {
            if (seenMetadataTileKeys.Contains(previousKey))
                continue;

            if (!TryParseTileKey(previousKey, out string locationName, out Vector2 tile))
                continue;

            if (!allLocations.TryGetValue(locationName, out GameLocation? location))
                continue;

            StardewValley.Object? objectAtTile = GetObjectAt(location, tile);
            if (objectAtTile != null)
                ClearPowerMetadata(objectAtTile);
        }
    }
}
