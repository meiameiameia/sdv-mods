# Ecosystem Architecture

## Goal
Define a unified machine ecosystem where your mods participate cleanly in the broader modpack without hard dependencies.

## Current Building Blocks
- **PowerGrid (`meiameiameia.PowerGrid`)**: energy network layer (generation, cables, batteries, conduits, consumer speedup).
- **Metal Kegs (`meiameiameia.MetalKegs`)**: machine definition layer (new keg machines mapped into `Data/BigCraftables` and `Data/Machines`).
- **Farm Terminal (`meiameiameia.FarmTerminal`)**: read-only StardewUI observability layer over PowerGrid snapshot data.
- **FishSmoker Recipe (`meiameiameia.FishSmokerRecipe`)**: progression/balance layer (`Data/CraftingRecipes` override).

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

## Forward-Compatible Concept
A farm terminal style mod can aggregate:
- machine state (`MinutesUntilReady`, machine rule origin),
- power state (`darth.PowerGrid/*` modData + snapshot API),
- automation routing state (Automate/Junimo context),
without changing existing gameplay contracts.
