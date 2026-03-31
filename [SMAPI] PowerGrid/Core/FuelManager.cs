using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Darth.PowerGrid.Core;

internal sealed class FuelManager
{
    private readonly IMonitor monitor;
    private readonly ModConfig config;

    // Track fuel ticks remaining per generator: key = node unique key
    private readonly Dictionary<string, int> fuelTicksRemaining = new();

    public FuelManager(IMonitor monitor, ModConfig config)
    {
        this.monitor = monitor;
        this.config = config;
    }

    public bool TryAddFuel(string generatorKey, StardewValley.Object fuelItem, int amount, out int ticksAdded)
    {
        ticksAdded = 0;
        if (amount <= 0)
            return false;

        int ticksPerUnit = GetFuelTicksPerUnit(fuelItem.QualifiedItemId);
        if (ticksPerUnit <= 0)
            return false;

        long addLong = (long)ticksPerUnit * amount;
        int add = (int)Math.Clamp(addLong, 0L, int.MaxValue);

        int current = fuelTicksRemaining.TryGetValue(generatorKey, out int existing) ? existing : 0;
        int next = (int)Math.Clamp((long)current + add, 0L, int.MaxValue);
        ticksAdded = next - current;

        if (ticksAdded <= 0)
            return false;

        fuelTicksRemaining[generatorKey] = next;
        return true;
    }

    public bool TryConsumeFuel(GameLocation location, PowerNode generator)
    {
        string key = generator.UniqueKey;

        // Check if we still have fuel ticks from a previously consumed item
        if (fuelTicksRemaining.TryGetValue(key, out int remaining) && remaining > 0)
        {
            fuelTicksRemaining[key] = remaining - 1;
            return true;
        }

        // Legacy migration: convert any old heldObject fuel stack to internal tick storage.
        if (TryAbsorbLegacyHeldFuel(location, generator, out _)
            && fuelTicksRemaining.TryGetValue(key, out int migratedRemaining)
            && migratedRemaining > 0)
        {
            fuelTicksRemaining[key] = migratedRemaining - 1;
            return true;
        }

        return false;
    }

    private bool TryAbsorbLegacyHeldFuel(GameLocation location, PowerNode generator, out int ticksAdded)
    {
        ticksAdded = 0;

        StardewValley.Object? genObj = location.getObjectAtTile((int)generator.Tile.X, (int)generator.Tile.Y);
        if (genObj == null || !string.Equals(genObj.ItemId, generator.ItemId, StringComparison.Ordinal))
            return false;

        StardewValley.Object? heldFuel = genObj.heldObject.Value;
        if (heldFuel == null || heldFuel.Stack <= 0)
            return false;

        if (!TryAddFuel(generator.UniqueKey, heldFuel, heldFuel.Stack, out ticksAdded))
            return false;

        genObj.heldObject.Value = null;
        monitor.Log($"Migrated legacy held fuel into tick storage for {generator.UniqueKey}: +{ticksAdded} ticks.", LogLevel.Trace);
        return true;
    }

    private int GetFuelTicksPerUnit(string qualifiedId)
    {
        // Coal = "(O)382", gives config-defined ticks
        // Wood = "(O)388"
        // Hardwood = "(O)709"
        // Battery Pack = "(O)787"

        return qualifiedId switch
        {
            "(O)382" => config.CoalFuelTicks,      // Coal: default 6 ticks = 60 minutes
            "(O)388" => config.WoodFuelTicks,       // Wood: default 2 ticks = 20 minutes
            "(O)709" => config.HardwoodFuelTicks,   // Hardwood: default 4 ticks = 40 minutes
            "(O)787" => config.BatteryPackFuelTicks, // Battery Pack: default 12 ticks = 120 minutes
            _ => 0
        };
    }

    public int GetFuelTicksRemaining(string nodeKey)
    {
        return fuelTicksRemaining.TryGetValue(nodeKey, out int ticks) ? ticks : 0;
    }

    public bool TryRemoveFuelState(string nodeKey, out int removedTicks)
    {
        removedTicks = 0;
        if (!fuelTicksRemaining.TryGetValue(nodeKey, out int existing))
            return false;

        removedTicks = existing;
        fuelTicksRemaining.Remove(nodeKey);
        return true;
    }

    public int PruneToKeys(ISet<string> validGeneratorKeys)
    {
        if (fuelTicksRemaining.Count == 0)
            return 0;

        var staleKeys = new List<string>();
        foreach (string key in fuelTicksRemaining.Keys)
        {
            if (!validGeneratorKeys.Contains(key))
                staleKeys.Add(key);
        }

        foreach (string key in staleKeys)
            fuelTicksRemaining.Remove(key);

        return staleKeys.Count;
    }

    public void ClearAll()
    {
        fuelTicksRemaining.Clear();
    }

    public Dictionary<string, int> ExportState() => new(fuelTicksRemaining);

    public void ImportState(Dictionary<string, int>? state)
    {
        fuelTicksRemaining.Clear();
        if (state != null)
        {
            foreach (var kvp in state)
                fuelTicksRemaining[kvp.Key] = kvp.Value;
        }
    }
}
