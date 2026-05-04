using Microsoft.Xna.Framework;
using StardewModdingAPI;
using System.Globalization;

namespace Meiameiameia.PowerGrid.Core;

internal sealed class BatteryStateManager
{
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private const string MdCharge = "meiameiameia.PowerGrid/charge";

    // Key: "LocationName|TileX|TileY|ItemId" -> stored EU
    private Dictionary<string, int> batteryCharges = new();

    public BatteryStateManager(IMonitor monitor, ModConfig config)
    {
        this.monitor = monitor;
        this.config = config;
    }

    public int GetCharge(PowerNode battery)
    {
        if (TryReadObjectCharge(battery.SourceObject, battery.Capacity, out int objectCharge))
        {
            batteryCharges.Remove(battery.UniqueKey);
            return objectCharge;
        }

        if (!batteryCharges.TryGetValue(battery.UniqueKey, out int charge))
            return 0;

        charge = Math.Clamp(charge, 0, battery.Capacity);
        WriteObjectCharge(battery.SourceObject, charge);
        batteryCharges.Remove(battery.UniqueKey);
        return charge;
    }

    public void SetCharge(PowerNode battery, int charge)
    {
        int clamped = Math.Clamp(charge, 0, battery.Capacity);
        if (battery.SourceObject != null)
        {
            WriteObjectCharge(battery.SourceObject, clamped);
            batteryCharges.Remove(battery.UniqueKey);
            return;
        }

        batteryCharges[battery.UniqueKey] = clamped;
    }

    public int AddCharge(PowerNode battery, int amount)
    {
        int current = GetCharge(battery);
        int space = battery.Capacity - current;
        int added = Math.Min(amount, space);
        SetCharge(battery, current + added);
        return added;
    }

    public int DrainCharge(PowerNode battery, int amount)
    {
        int current = GetCharge(battery);
        int drained = Math.Min(amount, current);
        SetCharge(battery, current - drained);
        return drained;
    }

    public void ApplyDailyLeak()
    {
        if (config.BatteryDailyLeakPercent <= 0f)
            return;

        var keys = new List<string>(batteryCharges.Keys);
        foreach (string key in keys)
        {
            int current = batteryCharges[key];
            if (current > 0)
            {
                int leak = Math.Max(1, (int)(current * config.BatteryDailyLeakPercent / 100f));
                batteryCharges[key] = Math.Max(0, current - leak);
            }
        }
    }

    public void ApplyDailyLeak(PowerNode battery)
    {
        if (config.BatteryDailyLeakPercent <= 0f)
            return;

        int current = GetCharge(battery);
        if (current <= 0)
            return;

        int leak = Math.Max(1, (int)(current * config.BatteryDailyLeakPercent / 100f));
        SetCharge(battery, current - leak);
    }

    public void PruneStaleEntries(HashSet<string> activeKeys)
    {
        var toRemove = new List<string>();
        foreach (string key in batteryCharges.Keys)
        {
            if (!activeKeys.Contains(key))
                toRemove.Add(key);
        }
        foreach (string key in toRemove)
            batteryCharges.Remove(key);
    }

    public void RemoveTileState(PowerNode battery)
    {
        batteryCharges.Remove(battery.UniqueKey);
    }

    public Dictionary<string, int> ExportState() => new(batteryCharges);

    public void ImportState(Dictionary<string, int>? state)
    {
        batteryCharges = state ?? new();
    }

    public int TotalStoredEU()
    {
        int total = 0;
        foreach (int charge in batteryCharges.Values)
            total += charge;
        return total;
    }

    private static bool TryReadObjectCharge(StardewValley.Object? obj, int capacity, out int charge)
    {
        charge = 0;
        if (obj == null)
            return false;

        if (!obj.modData.TryGetValue(MdCharge, out string? raw) || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out charge))
            return false;

        charge = Math.Clamp(charge, 0, capacity);
        return true;
    }

    private static void WriteObjectCharge(StardewValley.Object? obj, int charge)
    {
        if (obj == null)
            return;

        obj.modData[MdCharge] = charge.ToString(CultureInfo.InvariantCulture);
    }
}
