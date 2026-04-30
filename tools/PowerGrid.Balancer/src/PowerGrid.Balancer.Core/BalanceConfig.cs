namespace PowerGrid.Balancer.Core;

public sealed class BalanceConfig
{
    public int TickMinutes { get; init; } = 10;
    public int TicksPerDay { get; init; } = 120;
    public float BatteryDailyLeakPercent { get; init; } = 2f;
    public Dictionary<string, MachineDefinition> Machines { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, GeneratorDefinition> Generators { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, BatteryDefinition> Batteries { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, CableDefinition> Cables { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, FuelDefinition> Fuels { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, RecipeDefinition> Recipes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MachineDefinition
{
    public required string Name { get; init; }
    public int DemandEuPerTick { get; init; }
    public float MaxSpeedBonus { get; init; }
    public string Notes { get; init; } = "";
}

public sealed class GeneratorDefinition
{
    public required string Name { get; init; }
    public int OutputEuPerTick { get; init; }
    public string? Fuel { get; init; }
    public List<string> FuelOptions { get; init; } = new();
    public bool WeatherAdjusted { get; init; }
    public string Notes { get; init; } = "";
}

public sealed class BatteryDefinition
{
    public required string Name { get; init; }
    public int CapacityEu { get; init; }
}

public sealed class CableDefinition
{
    public required string Name { get; init; }
    public int ThroughputEuPerTick { get; init; }
}

public sealed class FuelDefinition
{
    public required string Name { get; init; }
    public int TicksPerUnit { get; init; }
}

public sealed class RecipeDefinition
{
    public int OutputCount { get; init; } = 1;
    public Dictionary<string, int> Ingredients { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
