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

## Files

- `balance/powergrid-0.1.0.json`: current PowerGrid defaults.
- `scenarios/*.json`: benchmark setups.
- `src/PowerGrid.Balancer.Core`: reusable simulation logic.
- `src/PowerGrid.Balancer.Cli`: command-line runner and Markdown reports.

## Design Notes

This tool intentionally starts as a CLI because the first user is the maintainer/architect workflow. It should be fast to run, easy to diff, and useful for batch comparisons.

The simulation is a balance approximation, not a SMAPI runtime clone. When a result points to a possible issue, confirm important changes in-game before shipping.
