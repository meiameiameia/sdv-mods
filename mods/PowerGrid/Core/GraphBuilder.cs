using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewModdingAPI;

namespace Meiameiameia.PowerGrid.Core;

internal sealed class GraphBuilder
{
    private readonly IMonitor monitor;
    private readonly ModConfig config;

    private static readonly Vector2[] AdjacentOffsets = new Vector2[]
    {
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
    };

    public GraphBuilder(IMonitor monitor, ModConfig config)
    {
        this.monitor = monitor;
        this.config = config;
    }

    public List<PowerNetwork> BuildNetworks(GameLocation location)
    {
        string locName = location.NameOrUniqueName;
        Dictionary<Vector2, PowerNode> nodeMap = ScanLocation(location, locName);

        if (nodeMap.Count == 0)
            return new List<PowerNetwork>();

        return FloodFillNetworks(nodeMap);
    }

    private Dictionary<Vector2, PowerNode> ScanLocation(GameLocation location, string locName)
    {
        var nodeMap = new Dictionary<Vector2, PowerNode>();

        foreach (var pair in location.objects.Pairs)
        {
            Vector2 tile = pair.Key;
            StardewValley.Object obj = pair.Value;
            string itemId = obj.ItemId ?? "";
            string qualifiedId = obj.QualifiedItemId ?? "";

            if (config.DebugOverlayEnabled
                && (itemId.StartsWith(PowerConstants.ModPrefix, StringComparison.Ordinal)
                    || qualifiedId.StartsWith("(BC)" + PowerConstants.ModPrefix, StringComparison.Ordinal)))
            {
                monitor.Log($"[GraphBuilder.ScanLocation] Found PowerGrid object at {tile}: itemId='{itemId}', qualifiedId='{qualifiedId}'", LogLevel.Trace);
            }

            PowerNode? node = ClassifyObject(itemId, qualifiedId, locName, tile, obj);
            if (node != null)
                nodeMap[tile] = node;
        }

        // Calculate connection masks for cables (check for ANY adjacent power grid component)
        foreach (var kvp in nodeMap)
        {
            if (kvp.Value.NodeType == PowerNodeType.Cable)
            {
                int mask = 0;
                // Up = 1, Right = 2, Down = 4, Left = 8
                // A cable connects if there's ANY power grid node adjacent (cable, generator, battery, consumer, conduit)
                if (nodeMap.ContainsKey(kvp.Key + new Vector2(0, -1))) mask |= 1;  // Up
                if (nodeMap.ContainsKey(kvp.Key + new Vector2(1, 0)))  mask |= 2;  // Right
                if (nodeMap.ContainsKey(kvp.Key + new Vector2(0, 1)))  mask |= 4;  // Down
                if (nodeMap.ContainsKey(kvp.Key + new Vector2(-1, 0))) mask |= 8;  // Left
                kvp.Value.ConnectionMask = mask;
                
                if (config.DebugOverlayEnabled)
                    monitor.Log($"[GraphBuilder] Cable at {kvp.Key} has mask {mask} (binary: {Convert.ToString(mask, 2).PadLeft(4, '0')})", LogLevel.Trace);
            }
        }

        return nodeMap;
    }

    private PowerNode? ClassifyObject(string itemId, string qualifiedId, string locName, Vector2 tile, StardewValley.Object obj)
    {
        // Cables
        if (itemId == PowerConstants.CopperCableId)
            return MakeCable(locName, tile, itemId, CableTier.Copper, config.CopperCableThroughput);
        if (itemId == PowerConstants.IronCableId)
            return MakeCable(locName, tile, itemId, CableTier.Iron, config.IronCableThroughput);
        if (itemId == PowerConstants.IridiumCableId)
            return MakeCable(locName, tile, itemId, CableTier.Iridium, config.IridiumCableThroughput);
        if (itemId == PowerConstants.EnergizedIridiumCableId)
            return MakeCable(locName, tile, itemId, CableTier.EnergizedIridium, config.EnergizedIridiumCableThroughput);

        // Generators
        if (itemId == PowerConstants.SteamGeneratorId)
            return new PowerNode
            {
                NodeType = PowerNodeType.Generator,
                LocationName = locName,
                Tile = tile,
                ItemId = itemId,
                GenerationPerTick = config.SteamGeneratorEUPerTick,
                RequiresFuel = true
            };
        if (itemId == PowerConstants.CombustionGeneratorId)
            return new PowerNode
            {
                NodeType = PowerNodeType.Generator,
                LocationName = locName,
                Tile = tile,
                ItemId = itemId,
                GenerationPerTick = config.CombustionGeneratorEUPerTick,
                RequiresFuel = true
            };
        if (itemId == PowerConstants.RadioisotopeGeneratorId)
            return new PowerNode
            {
                NodeType = PowerNodeType.Generator,
                LocationName = locName,
                Tile = tile,
                ItemId = itemId,
                GenerationPerTick = config.RadioisotopeGeneratorEUPerTick,
                RequiresFuel = true
            };
        if (itemId == PowerConstants.WindGeneratorId)
            return new PowerNode
            {
                NodeType = PowerNodeType.Generator,
                LocationName = locName,
                Tile = tile,
                ItemId = itemId,
                GenerationPerTick = config.WindGeneratorEUPerTick,
                RequiresFuel = false
            };

        // Batteries
        if (itemId == PowerConstants.BasicBatteryId)
            return new PowerNode
            {
                NodeType = PowerNodeType.Battery,
                LocationName = locName,
                Tile = tile,
                ItemId = itemId,
                Capacity = config.BasicBatteryCapacity,
                SourceObject = obj
            };
        if (itemId == PowerConstants.IridiumBatteryId)
            return new PowerNode
            {
                NodeType = PowerNodeType.Battery,
                LocationName = locName,
                Tile = tile,
                ItemId = itemId,
                Capacity = config.IridiumBatteryCapacity,
                SourceObject = obj
            };

        // Power Conduit (cross-location bridge)
        if (itemId == PowerConstants.PowerConduitId)
            return new PowerNode
            {
                NodeType = PowerNodeType.Conduit,
                LocationName = locName,
                Tile = tile,
                ItemId = itemId
            };

        // Check registered external consumers
        var registered = ConsumerRegistry.Instance.GetConsumerDef(qualifiedId);
        if (registered != null)
        {
            (int demandPerTick, float maxSpeedupFraction) = GetConsumerPowerProfile(itemId, obj, registered.DemandPerTick, registered.MaxSpeedupFraction);
            return new PowerNode
            {
                NodeType = PowerNodeType.Consumer,
                LocationName = locName,
                Tile = tile,
                ItemId = itemId,
                DemandPerTick = demandPerTick,
                MaxSpeedupFraction = maxSpeedupFraction,
                Priority = registered.Priority
            };
        }

        return null;
    }

    private static (int DemandPerTick, float MaxSpeedupFraction) GetConsumerPowerProfile(
        string itemId,
        StardewValley.Object obj,
        int baseDemandPerTick,
        float baseMaxSpeedupFraction)
    {
        if (baseDemandPerTick <= 0 || !IsIndustrialProcessingMachine(itemId))
            return (baseDemandPerTick, baseMaxSpeedupFraction);

        float demandMultiplier = 1f;
        float speedupMultiplier = 1f;

        if (MachineUpgradeState.HasUpgrade(obj, PowerConstants.EfficiencyCoreId))
        {
            demandMultiplier = 0.70f;
        }
        else if (itemId == PowerConstants.ElectricSmelterId)
        {
            if (MachineUpgradeState.HasUpgrade(obj, PowerConstants.HeatingCoilId))
            {
                demandMultiplier = 1.25f;
                speedupMultiplier = 1.60f;
            }
            else if (MachineUpgradeState.HasUpgrade(obj, PowerConstants.CatalystChamberId))
            {
                demandMultiplier = 1.125f;
            }
        }
        else if (itemId == PowerConstants.IndustrialRecyclerId)
        {
            if (MachineUpgradeState.HasUpgrade(obj, PowerConstants.SortingMagnetId))
                demandMultiplier = 1.25f;
        }
        else if (itemId == PowerConstants.PoweredDehydratorId)
        {
            if (MachineUpgradeState.HasUpgrade(obj, PowerConstants.HeatRegulatorId))
            {
                demandMultiplier = 1.50f;
                speedupMultiplier = 1.625f;
            }
            else if (MachineUpgradeState.HasUpgrade(obj, PowerConstants.DryingRackArrayId))
            {
                demandMultiplier = 1.50f;
            }
        }

        int demandPerTick = Math.Max(1, (int)MathF.Ceiling(baseDemandPerTick * demandMultiplier));
        float maxSpeedupFraction = Math.Clamp(baseMaxSpeedupFraction * speedupMultiplier, 0f, 1f);
        return (demandPerTick, maxSpeedupFraction);
    }

    private static bool IsIndustrialProcessingMachine(string itemId)
    {
        return itemId == PowerConstants.ElectricSmelterId
            || itemId == PowerConstants.IndustrialRecyclerId
            || itemId == PowerConstants.PoweredDehydratorId;
    }

    private static PowerNode MakeCable(string locName, Vector2 tile, string itemId, CableTier tier, int throughput)
    {
        return new PowerNode
        {
            NodeType = PowerNodeType.Cable,
            LocationName = locName,
            Tile = tile,
            ItemId = itemId,
            CableTier = tier,
            ThroughputCap = throughput
        };
    }

    private List<PowerNetwork> FloodFillNetworks(Dictionary<Vector2, PowerNode> nodeMap)
    {
        var networks = new List<PowerNetwork>();
        var visited = new HashSet<Vector2>();
        int networkId = 0;

        if (config.DebugOverlayEnabled)
            monitor.Log($"[FloodFillNetworks] Starting with {nodeMap.Count} nodes", LogLevel.Trace);

        foreach (var kvp in nodeMap)
        {
            if (visited.Contains(kvp.Key))
                continue;

            var network = new PowerNetwork { NetworkId = networkId++ };
            var queue = new Queue<Vector2>();
            queue.Enqueue(kvp.Key);
            visited.Add(kvp.Key);

            while (queue.Count > 0)
            {
                Vector2 current = queue.Dequeue();
                if (nodeMap.TryGetValue(current, out PowerNode? node))
                {
                    network.AddNode(node);

                    foreach (Vector2 offset in AdjacentOffsets)
                    {
                        Vector2 neighbor = current + offset;
                        if (!visited.Contains(neighbor) && nodeMap.ContainsKey(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            // Add network if it has any nodes (including cable-only networks for rendering)
            if (network.Cables.Count > 0 || network.Generators.Count > 0 || network.Batteries.Count > 0 || network.Consumers.Count > 0)
            {
                if (config.DebugOverlayEnabled)
                    monitor.Log($"[FloodFillNetworks] Network {network.NetworkId}: {network.Cables.Count} cables, {network.Generators.Count} generators, {network.Batteries.Count} batteries, {network.Consumers.Count} consumers", LogLevel.Trace);
                networks.Add(network);
            }
        }

        if (config.DebugOverlayEnabled)
            monitor.Log($"[FloodFillNetworks] Returning {networks.Count} networks", LogLevel.Trace);
        return networks;
    }
}
