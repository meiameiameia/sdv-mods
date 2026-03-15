namespace DarthMods.API.Power;

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
