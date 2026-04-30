namespace PowerGrid.Balancer.Core;

public sealed class LoadoutPlan
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string Generator { get; init; } = "Steam Generator";
    public string Cable { get; init; } = "Copper Cable";
    public string Battery { get; init; } = "";
    public int BatteryCount { get; init; }
    public int CableCount { get; init; }
    public int ConduitCount { get; init; }
    public int ReserveDays { get; init; } = 14;
    public double ComfortHeadroom { get; init; } = 0.15d;
    public Dictionary<string, int> PoweredMachines { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Stockpile { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
