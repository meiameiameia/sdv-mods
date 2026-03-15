namespace DarthMods.API.Power;

/// <summary>
/// Public API for other SMAPI mods to interact with PowerGrid.
/// Obtain via helper.ModRegistry.GetApi&lt;IPowerGridApi&gt;("meiameiameia.PowerGrid").
/// </summary>
public interface IPowerGridApi
{
    /// <summary>Register a machine as a power consumer.</summary>
    /// <param name="qualifiedItemId">The qualified item ID, e.g. "(BC)mymod_MyMachine".</param>
    /// <param name="demandPerTick">EU demanded per 10-minute tick.</param>
    /// <param name="maxSpeedupFraction">Max speedup as a fraction (0.20 = 20%).</param>
    /// <param name="priority">Lower = higher priority (allocated first). Default consumers use 10.</param>
    /// <param name="displayName">Human-readable name for the monitor UI.</param>
    void RegisterConsumer(string qualifiedItemId, int demandPerTick, float maxSpeedupFraction, int priority, string displayName);

    /// <summary>Unregister a previously registered consumer.</summary>
    void UnregisterConsumer(string qualifiedItemId);

    /// <summary>Check if a specific tile in a location is receiving power.</summary>
    bool IsTilePowered(string locationName, int tileX, int tileY);

    /// <summary>Get the current speedup fraction for a consumer at a given tile (0.0 if unpowered).</summary>
    float GetSpeedupAtTile(string locationName, int tileX, int tileY);

    /// <summary>Get total stored EU across all batteries in a location.</summary>
    int GetTotalStoredEU(string locationName);

    /// <summary>Get read-only summaries for current power networks. If <paramref name="locationName"/> is null, returns all loaded networks.</summary>
    IReadOnlyList<PowerNetworkSnapshot> GetNetworkSnapshots(string? locationName = null);

    /// <summary>Get read-only consumer state snapshots. If <paramref name="locationName"/> is null, returns all loaded consumers.</summary>
    IReadOnlyList<PowerConsumerSnapshot> GetConsumerSnapshots(string? locationName = null);

    /// <summary>Get read-only generator snapshots. If <paramref name="locationName"/> is null, returns all loaded generators.</summary>
    IReadOnlyList<PowerGeneratorSnapshot> GetGeneratorSnapshots(string? locationName = null);

    /// <summary>Get read-only battery snapshots. If <paramref name="locationName"/> is null, returns all loaded batteries.</summary>
    IReadOnlyList<PowerBatterySnapshot> GetBatterySnapshots(string? locationName = null);
}
