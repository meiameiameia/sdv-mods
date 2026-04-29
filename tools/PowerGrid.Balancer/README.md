# PowerGrid Balancer

PowerGrid Balancer is a developer tool for testing PowerGrid balance without needing to set up long manual saves.

The first goal is simple: run synthetic farm/network scenarios and see whether the current defaults feel fair.

It is not a player-facing planner yet. If the simulator proves useful, it can later grow into a friendly layout and network planning tool.

## What It Checks

- EU demand per tick and per scenario
- generator output
- fuel pressure
- battery charge and drain
- cable throughput bottlenecks
- unmet power demand
- average power coverage
- approximate speed bonus delivered

## Run One Scenario

From the repository root:

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- scenario tools/PowerGrid.Balancer/scenarios/early-mixed-room.json
```

## Run All Scenarios

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- batch tools/PowerGrid.Balancer/scenarios
```

## Generate Balance Data

Use `audit` when you want files that can shape balance decisions:

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- audit tools/PowerGrid.Balancer/scenarios --out artifacts/balance-lab/current
```

The audit command writes:

- `summary.md`: readable balance signals and scenario summary.
- `scenario-results.csv`: one row per benchmark scenario.
- `fuel-use.csv`: fuel consumed and shortfall by scenario.
- `generator-capacity.md`: readable generator-to-machine capacity table.
- `generator-capacity.csv`: sortable generator capacity data.
- `machine-defaults.csv`: current machine EU demand and speed bonuses.

Generated audit files are local artifacts. Re-run the command whenever balance defaults or benchmark scenarios change.

## Generate Progression Data

Use `progression` when you want to audit whether PowerGrid grows well across a save:

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- progression tools/PowerGrid.Balancer/profiles/powergrid-progression-ladder.json --out artifacts/balance-lab/progression
```

The progression command writes:

- `progression-summary.md`: dated checkpoints from early grid to mature save adoption.
- `progression-stages.csv`: EU demand, generator count, fuel pressure, cable zones, and headline signals.
- `progression-resource-gaps.csv`: upfront recipe costs versus the stage stockpile.
- `progression-fuel.csv`: sustained fuel window after upfront costs.

The included profile uses approximate early/mid-game checkpoints and an anonymized mature Year 3 organized production benchmark.

## Compare Balance Configs

Use `compare-progression` before applying balance changes to the mod:

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- compare-progression tools/PowerGrid.Balancer/balance/powergrid-0.1.0.json tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json tools/PowerGrid.Balancer/profiles/powergrid-progression-ladder.json --out artifacts/balance-lab/comparison-moderate
```

The comparison command writes:

- `progression-comparison.md`: readable current-vs-proposed summary.
- `progression-comparison.csv`: sortable stage deltas.
- `progression-comparison-gaps.csv`: resource gaps for both configs.

## Files

- `balance/powergrid-0.1.0.json`: current PowerGrid defaults.
- `balance/powergrid-0.1.x-test.json`: generous tuning sandbox, not runtime defaults.
- `balance/powergrid-0.1.x-moderate.json`: moderate Biofuel tuning sandbox, not runtime defaults.
- `scenarios/*.json`: benchmark setups.
- `profiles/*.json`: progression and stockpile benchmark ladders.
- `src/PowerGrid.Balancer.Core`: reusable simulation logic.
- `src/PowerGrid.Balancer.Cli`: command-line runner and Markdown reports.

## Design Notes

This tool intentionally starts as a CLI because the first user is the maintainer/architect workflow. It should be fast to run, easy to diff, and useful for batch comparisons.

The simulation is a balance approximation, not a SMAPI runtime clone. When a result points to a possible issue, confirm important changes in-game before shipping.
