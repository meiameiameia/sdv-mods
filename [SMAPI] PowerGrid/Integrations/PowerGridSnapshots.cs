namespace Meiameiameia.PowerGrid.Integrations;

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

public sealed class PowerConsumerSnapshot
{
    public int NetworkId { get; init; }
    public string LocationName { get; init; } = "";
    public int TileX { get; init; }
    public int TileY { get; init; }
    public string ItemId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public bool IsProcessing { get; init; }
    public bool IsPowered { get; init; }
    public int DemandPerTick { get; init; }
    public int EUAllocated { get; init; }
    public float SpeedupFraction { get; init; }
    public int MinutesAccelerated { get; init; }
    public int MinutesRemaining { get; init; }
    public float MaxSpeedupFraction { get; init; }
    public int Priority { get; init; }
    public string ProgressMode { get; init; } = "minutes";
    public string ProgressText { get; init; } = "";
}

public sealed class PowerGeneratorSnapshot
{
    public int NetworkId { get; init; }
    public string LocationName { get; init; } = "";
    public int TileX { get; init; }
    public int TileY { get; init; }
    public string ItemId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public int GenerationPerTick { get; init; }
    public int GeneratedThisTick { get; init; }
    public bool RequiresFuel { get; init; }
    public int FuelTicksRemaining { get; init; }
    public bool IsOnline { get; init; }
}

public sealed class PowerBatterySnapshot
{
    public int NetworkId { get; init; }
    public string LocationName { get; init; } = "";
    public int TileX { get; init; }
    public int TileY { get; init; }
    public string ItemId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public int Charge { get; init; }
    public int Capacity { get; init; }
    public float ChargePercent { get; init; }
    public int DrainedThisTick { get; init; }
    public int StoredThisTick { get; init; }
}
