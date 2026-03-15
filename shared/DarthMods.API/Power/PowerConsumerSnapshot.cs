namespace DarthMods.API.Power;

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
}
