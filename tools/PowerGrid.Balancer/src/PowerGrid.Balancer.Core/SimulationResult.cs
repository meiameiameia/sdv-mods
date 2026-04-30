namespace PowerGrid.Balancer.Core;

public sealed class SimulationResult
{
    public required string ScenarioName { get; init; }
    public int Days { get; init; }
    public int TicksSimulated { get; init; }
    public int DemandEuPerTick { get; init; }
    public int GenerationEuPerTick { get; init; }
    public int CableThroughputEuPerTick { get; init; }
    public int BatteryCapacityEu { get; init; }
    public int FinalBatteryChargeEu { get; init; }
    public int TotalDemandEu { get; init; }
    public int TotalGeneratedEu { get; init; }
    public int TotalConsumedEu { get; init; }
    public int TotalUnmetEu { get; init; }
    public int TotalBatteryChargedEu { get; init; }
    public int TotalBatteryDrainedEu { get; init; }
    public float AveragePowerCoverage { get; init; }
    public float AverageSpeedBonus { get; init; }
    public float SurplusRatio { get; init; }
    public float BatteryUtilization { get; init; }
    public bool HasThroughputBottleneck { get; init; }
    public bool IsStable => TotalUnmetEu == 0;
    public IReadOnlyList<FuelUseResult> FuelUse { get; init; } = Array.Empty<FuelUseResult>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
}

public sealed class FuelUseResult
{
    public required string Fuel { get; init; }
    public int UnitsAvailable { get; init; }
    public int UnitsConsumed { get; init; }
    public int TicksConsumed { get; init; }
    public int TicksShort { get; init; }
}
