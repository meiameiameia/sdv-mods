namespace PowerGrid.Balancer.Core;

public sealed class Scenario
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public int Days { get; init; } = 1;
    public string Cable { get; init; } = "Copper Cable";
    public float InitialBatteryChargePercent { get; init; } = 0f;
    public WeatherProfile Weather { get; init; } = new();
    public List<MachineLoad> Machines { get; init; } = new();
    public List<GeneratorLoad> Generators { get; init; } = new();
    public List<BatteryLoad> Batteries { get; init; } = new();
    public Dictionary<string, int> FuelInventory { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class WeatherProfile
{
    public float Clear { get; init; } = 1f;
    public float Rain { get; init; }
    public float Storm { get; init; }
    public float Snow { get; init; }

    public float NormalizedClear => Normalize(Clear);
    public float NormalizedRain => Normalize(Rain);
    public float NormalizedStorm => Normalize(Storm);
    public float NormalizedSnow => Normalize(Snow);

    public float Total => Clear + Rain + Storm + Snow;

    private float Normalize(float value)
    {
        float total = Total;
        return total <= 0 ? 0 : Math.Max(0, value) / total;
    }
}

public sealed class MachineLoad
{
    public required string Id { get; init; }
    public int Count { get; init; }
    public float ActiveFraction { get; init; } = 1f;
}

public sealed class GeneratorLoad
{
    public required string Id { get; init; }
    public int Count { get; init; }
}

public sealed class BatteryLoad
{
    public required string Id { get; init; }
    public int Count { get; init; }
}
