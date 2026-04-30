namespace PowerGrid.Balancer.Core;

public sealed class ResourceBudgetProfile
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public List<ResourceBudgetStage> Stages { get; init; } = new();
}

public sealed class ResourceBudgetStage
{
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public Dictionary<string, ResourceBudget> Resources { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ResourceBudget
{
    public int WeeklyIncome { get; init; }
    public int ReserveFloor { get; init; }
    public double MaxPowerGridShare { get; init; } = 0.25d;
    public string Notes { get; init; } = "";
}
