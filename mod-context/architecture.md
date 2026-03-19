# Ecosystem Architecture

## Goal
Define a unified machine ecosystem where your mods participate cleanly in the broader modpack without hard dependencies.

## Current Building Blocks
- **PowerGrid (`meiameiameia.PowerGrid`)**: energy network layer (generation, cables, batteries, conduits, consumer speedup).
- **Metal Kegs (`meiameiameia.MetalKegs`)**: machine definition layer (new keg machines mapped into `Data/BigCraftables` and `Data/Machines`).
- **Farm Terminal (`meiameiameia.FarmTerminal`)**: read-only StardewUI observability layer over PowerGrid snapshot data.
- **FishSmoker Recipe (`meiameiameia.FishSmokerRecipe`)**: progression/balance layer (`Data/CraftingRecipes` override).

## Near-Term Product Boundary Update

- `Electronic Artisan Machines` remains a separate machine-family line, but expansion beyond `Industrial Preserves Jar` is deferred for now.
- The immediate next product/design focus is a new standalone `Perfection Advisor` line (spoiler/assist, default-off).
- `Farm Terminal` remains read-only and should not absorb full advisor ownership in v1; at most, later summary integration can be added through existing snapshot/read surfaces.

## Conceptual Topology

```text
[Content/Recipe Layer]
  FishSmoker Recipe (CP)
          |
          v
[Machine Layer]
  Metal Kegs + other machine packs (GOF/Cornucopia/EMC rules)
          |
          v
[Automation + Control Layer]
  Automate / Better Junimos / Machine Control Panel
          |
          v
[Energy Layer]
  PowerGrid (speedup + power state + conduit links)
          |
          v
[UI/Config Layer]
  Farm Terminal / GMCM / StardewUI / Data Layers style visual tooling
```

## Relationship Model
- `PowerGrid -> Metal Kegs`: already implemented via soft runtime detection of Metal Keg qualified IDs and per-tick speedup.
- `Metal Kegs -> Data/Machines ecosystem`: behaves as keg clones, allowing machine frameworks and compatibility patches to treat them as normal machines.
- `FishSmoker Recipe -> progression economy`: recipe rebalance influences when processing throughput enters the economy.

## Integration Surfaces
- **Public API**: PowerGrid exposes `IPowerGridApi` for consumer registration and power queries.
- **Read-only snapshots**:
  - network summaries,
  - consumer snapshots,
  - generator snapshots,
  - battery snapshots.
- **Data contracts**:
  - `Metal Kegs` edits `Data/BigCraftables`, `Data/CraftingRecipes`, `Data/Machines`.
  - `FishSmoker Recipe` edits `Data/CraftingRecipes`.
  - `PowerGrid` annotates object `modData` with `darth.PowerGrid/*` keys each tick (prefix intentionally kept stable for compatibility).
- **Config/UI**:
  - PowerGrid already integrates with GMCM.
  - Farm Terminal consumes PowerGrid snapshots through StardewUI as a read-only dashboard.
  - Metal Kegs currently uses JSON config only.

## Target Unified Pattern
1. Keep machine identity/data in machine mods (`Metal Kegs`, EMC/CP machine packs).
2. Keep throughput orchestration in automation mods (`Automate`, `Better Junimos`).
3. Keep acceleration/energy constraints in PowerGrid.
4. Use shared UI/config channels (GMCM now, StardewUI for Farm Terminal) for observability.

## Near-Term Ecosystem Contract

Before adding the next major machine-family wave, the ecosystem should converge on a small repeatable powered-machine pattern instead of continuing with ad hoc integration.

The minimum useful shared shape is:

- display name
- machine family
- qualified item id
- demand per tick
- speedup cap
- priority
- progress mode
- optional progress text / summary formatter

The ownership rule should be:

- machine mods self-register as `PowerGrid` consumers
- `PowerGrid` consumes traits and telemetry, not hard-coded machine-family knowledge
- `Farm Terminal` consumes stable snapshots and summary text instead of machine-specific assumptions

This is intentionally not a large framework rewrite. It is a small contract-hardening step so future powered machine mods are cheaper and more deterministic to build.

## Config Productization

Config parity should be treated as part of product hardening, not as a later polish-only task.

Near-term expectation for shipped mods:

- `PowerGrid` remains the reference implementation for GMCM support
- `Metal Kegs` should reach GMCM parity for its current config surface
- `Farm Terminal` should expose its current user-facing config surface through GMCM
- `FishSmoker Recipe` only needs GMCM/config parity if it grows a real player-facing preset/config surface later

This keeps the ecosystem more consistent before the next machine-family expansion.

## Forward-Compatible Concept
A farm terminal style mod can aggregate:
- machine state (`MinutesUntilReady`, machine rule origin),
- power state (`darth.PowerGrid/*` modData + snapshot API),
- automation routing state (Automate/Junimo context),
without changing existing gameplay contracts.
