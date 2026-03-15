namespace DarthMods.API.Power;

public sealed class PowerNetworkSnapshot
{
    public int NetworkId { get; init; }
    public string[] LocationNames { get; init; } = Array.Empty<string>();
    public int CableCount { get; init; }
    public int GeneratorCount { get; init; }
    public int BatteryCount { get; init; }
    public int ConsumerCount { get; init; }
    public int ConduitCount { get; init; }
    public int CableThroughputCap { get; init; }
    public int TotalGenerationPerTick { get; init; }
    public int TotalDemandPerTick { get; init; }
    public int TotalStoredEU { get; init; }
    public int TotalBatteryCapacity { get; init; }
    public int LastTickGenerated { get; init; }
    public int LastTickConsumed { get; init; }
    public int LastTickFromBatteries { get; init; }
    public int LastTickStoredInBatteries { get; init; }

    public bool IsCrossLocation => LocationNames.Length > 1;
}
