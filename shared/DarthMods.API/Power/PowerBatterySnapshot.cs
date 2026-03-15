namespace DarthMods.API.Power;

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
