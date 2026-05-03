using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace Meiameiameia.PowerGrid.Core;

internal sealed class BatteryStateManager
{
    private readonly IMonitor monitor;
    private readonly ModConfig config;

    // Key: "LocationName|TileX|TileY|ItemId" -> stored EU
    private Dictionary<string, int> batteryCharges = new();

    public BatteryStateManager(IMonitor monitor, ModConfig config)
    {
        this.monitor = monitor;
        this.config = config;
    }

    public int GetCharge(PowerNode battery)
    {
        return batteryCharges.TryGetValue(battery.UniqueKey, out int charge) ? charge : 0;
    }

    public void SetCharge(PowerNode battery, int charge)
    {
        batteryCharges[battery.UniqueKey] = Math.Clamp(charge, 0, battery.Capacity);
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
}
