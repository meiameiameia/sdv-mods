namespace PowerGrid.Balancer.Core;

public sealed class ProgressionProfile
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public List<ProgressionStage> Stages { get; init; } = new();
}

public sealed class ProgressionStage
{
    public required string Name { get; init; }
    public int Year { get; init; }
    public string Season { get; init; } = "";
    public int Day { get; init; }
    public string Goal { get; init; } = "";
    public string Generator { get; init; } = "Steam Generator";
    public string Cable { get; init; } = "Copper Cable";
    public string Battery { get; init; } = "";
    public int BatteryCount { get; init; }
    public int CableCount { get; init; }
    public int ConduitCount { get; init; }
    public Dictionary<string, int> PoweredMachines { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Stockpile { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
