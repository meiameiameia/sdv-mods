using Microsoft.Xna.Framework;
using DarthMods.API.Power;

namespace Darth.PowerGrid.Integrations;

/// <summary>
/// Compatibility shim for older integrations still compiled against PowerGrid's local API namespace.
/// </summary>
public interface IPowerGridApi
{
    void RegisterConsumer(string qualifiedItemId, int demandPerTick, float maxSpeedupFraction, int priority, string displayName);

    void UnregisterConsumer(string qualifiedItemId);

    bool IsTilePowered(string locationName, Vector2 tile);

    float GetSpeedupAtTile(string locationName, Vector2 tile);

    int GetTotalStoredEU(string locationName);

    IReadOnlyList<PowerNetworkSnapshot> GetNetworkSnapshots(string? locationName = null);

    IReadOnlyList<PowerConsumerSnapshot> GetConsumerSnapshots(string? locationName = null);

    IReadOnlyList<PowerGeneratorSnapshot> GetGeneratorSnapshots(string? locationName = null);

    IReadOnlyList<PowerBatterySnapshot> GetBatterySnapshots(string? locationName = null);
}
