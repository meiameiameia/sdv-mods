using Microsoft.Xna.Framework;
using StardewValley;
using Darth.PowerGrid.Core;
using DarthMods.API.Power;
using SharedPowerGridApi = DarthMods.API.Power.IPowerGridApi;

namespace Darth.PowerGrid.Integrations;

public sealed class PowerGridApi : IPowerGridApi, SharedPowerGridApi
{
    public void RegisterConsumer(string qualifiedItemId, int demandPerTick, float maxSpeedupFraction, int priority, string displayName)
    {
        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = qualifiedItemId,
            DemandPerTick = demandPerTick,
            MaxSpeedupFraction = maxSpeedupFraction,
            Priority = priority,
            DisplayName = displayName
        });
    }

    public void UnregisterConsumer(string qualifiedItemId)
    {
        ConsumerRegistry.Instance.Unregister(qualifiedItemId);
    }

    public bool IsTilePowered(string locationName, Vector2 tile)
    {
        return GetSpeedupAtTile(locationName, tile) > 0f;
    }

    public float GetSpeedupAtTile(string locationName, Vector2 tile)
    {
        var reports = ModEntry.Instance.PowerMgr.GetLastReports(locationName);
        foreach (var report in reports)
        {
            foreach (var alloc in report.Allocations)
            {
                if (alloc.Consumer.Tile == tile && alloc.SpeedupFraction > 0f)
                    return alloc.SpeedupFraction;
            }
        }
        return 0f;
    }

    public int GetTotalStoredEU(string locationName)
    {
        GameLocation? loc = Game1.getLocationFromName(locationName);
        if (loc == null)
            return 0;

        var networks = ModEntry.Instance.PowerMgr.GetNetworks(loc);
        int total = 0;
        foreach (var net in networks)
        {
            foreach (var bat in net.Batteries)
                total += ModEntry.Instance.BatteryState.GetCharge(bat);
        }
        return total;
    }

    public IReadOnlyList<PowerNetworkSnapshot> GetNetworkSnapshots(string? locationName = null)
    {
        return ModEntry.Instance.PowerQuery.GetNetworkSnapshots(locationName);
    }

    public IReadOnlyList<PowerConsumerSnapshot> GetConsumerSnapshots(string? locationName = null)
    {
        return ModEntry.Instance.PowerQuery.GetConsumerSnapshots(locationName);
    }

    public IReadOnlyList<PowerGeneratorSnapshot> GetGeneratorSnapshots(string? locationName = null)
    {
        return ModEntry.Instance.PowerQuery.GetGeneratorSnapshots(locationName);
    }

    public IReadOnlyList<PowerBatterySnapshot> GetBatterySnapshots(string? locationName = null)
    {
        return ModEntry.Instance.PowerQuery.GetBatterySnapshots(locationName);
    }

    bool SharedPowerGridApi.IsTilePowered(string locationName, int tileX, int tileY)
    {
        return IsTilePowered(locationName, new Vector2(tileX, tileY));
    }

    float SharedPowerGridApi.GetSpeedupAtTile(string locationName, int tileX, int tileY)
    {
        return GetSpeedupAtTile(locationName, new Vector2(tileX, tileY));
    }
}
