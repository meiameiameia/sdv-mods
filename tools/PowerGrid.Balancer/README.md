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
- `progression-plan.md`: setup recommendations with minimum/comfort generators, cable zones, and fuel reserves.
- `progression-plan.csv`: sortable planning data for audits and future UI work.

The included profile uses approximate early/mid-game checkpoints and an anonymized mature Year 3 organized production benchmark.

## Plan A Custom Loadout

Use `plan` when you want to ask, "If I build this room, what power setup do I need?"

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- plan tools/PowerGrid.Balancer/loadouts/sample-big-shed.json --config tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json --out artifacts/balance-lab/loadout-sample
```

You can also point it at the whole loadout folder to generate an index and one report per preset:

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- plan tools/PowerGrid.Balancer/loadouts --config tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json --out artifacts/balance-lab/loadout-suite
```

The plan command writes:

- `loadout-index.md`: summary of all loadouts when planning a folder or glob.
- `loadout-index.csv`: sortable summary of all loadouts.
- `<loadout>/loadout-plan.md`: readable power plan, machine mix, resource check, and recommendations.
- `<loadout>/loadout-plan.csv`: one-row summary for comparisons.
- `<loadout>/loadout-resource-gaps.csv`: resource need versus stockpile.

## Check Resource Pressure

Use `resources` when you want to know which ingredients are blocking planned setups:

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- resources tools/PowerGrid.Balancer/loadouts --config tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json --out artifacts/balance-lab/resource-pressure
```

The resources command writes:

- `resource-pressure.md`: ranked ingredient pressure, main pressure source, and short balance notes.
- `resource-pressure.csv`: sortable data for comparing recipe and fuel changes.

This is the first pass for answering questions like "is Biofuel asking for too much Sap?" or "are powered machines leaning too hard on one metal tier?"

## Check Resource Sustainability

Use `sustainability` when you want to know whether PowerGrid is using a fair share of the player's wider resource economy:

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- sustainability tools/PowerGrid.Balancer/loadouts --config tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json --out artifacts/balance-lab/sustainability
```

This command compares each loadout against a resource budget profile. Setup costs are checked against stockpiles after a protected reserve floor. Ongoing fuel costs are checked against a safe share of expected weekly income.

The sustainability command writes:

- `resource-sustainability.md`: readable setup and weekly fuel pressure notes.
- `resource-sustainability.csv`: sortable sustainability data.

Use this before changing fuel or recipe defaults. A resource can be technically available and still be bad balance if PowerGrid consumes too much of what players need for the rest of Stardew.

## Compare Sustainability Candidates

Use `compare-sustainability` when you want to compare several balance candidates against the same safe resource budget:

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- compare-sustainability tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json tools/PowerGrid.Balancer/balance/powergrid-0.1.x-biofuel-*.json tools/PowerGrid.Balancer/loadouts --out artifacts/balance-lab/biofuel-sustainability
```

The comparison command writes:

- `resource-sustainability-comparison.md`: readable candidate comparison focused on safe weekly resource use.
- `resource-sustainability-comparison.csv`: sortable comparison data.

Use this alongside `compare-resources`. Resource pressure answers "what blocks the setup?" Sustainability answers "does this setup crowd out normal Stardew play?"

## Compare Resource Candidates

Use `compare-resources` when you want to test several balance candidates against the same loadout suite:

```powershell
dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- compare-resources tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json tools/PowerGrid.Balancer/balance/powergrid-0.1.x-biofuel-*.json tools/PowerGrid.Balancer/loadouts --out artifacts/balance-lab/biofuel-candidates
```

The included Biofuel candidates only change Biofuel output or fuel duration. They are sandbox configs for analysis, not shipped defaults.

The comparison command writes:

- `resource-pressure-comparison.md`: readable bottleneck comparison across configs.
- `resource-pressure-comparison.csv`: sortable comparison data.

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
- `balance/powergrid-0.1.x-biofuel-*.json`: Biofuel candidate sandboxes for resource-pressure comparison.
- `resource-budgets/powergrid-sustainability.json`: safe resource share assumptions for sustainability checks.
- `scenarios/*.json`: benchmark setups.
- `loadouts/*.json`: custom machine mixes for setup planning.
- `profiles/*.json`: progression and stockpile benchmark ladders.
- `src/PowerGrid.Balancer.Core`: reusable simulation logic.
- `src/PowerGrid.Balancer.Cli`: command-line runner and Markdown reports.

## Design Notes

This tool intentionally starts as a CLI because the first user is the maintainer/architect workflow. It should be fast to run, easy to diff, and useful for batch comparisons.

The simulation is a balance approximation, not a SMAPI runtime clone. When a result points to a possible issue, confirm important changes in-game before shipping.
