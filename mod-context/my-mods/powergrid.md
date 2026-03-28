# PowerGrid

## Purpose
`[SMAPI] PowerGrid` adds an electrical infrastructure layer that speeds up eligible machines without changing their recipes or outputs.

## Current Architecture

### Entry and Event Flow
- Main entry: `ModEntry.cs`.
- Hooks key SMAPI events (`GameLaunched`, `SaveLoaded`, `DayStarted`, `TimeChanged`, `AssetRequested`, object changes, input, rendering).
- Simulates power every 10 in-game minutes.

### Data and Asset Injection
- Edits `Data/BigCraftables` to register power components.
- Edits `Data/CraftingRecipes` to register crafting recipes.
- Serves custom texture assets from `Mods/meiameiameia.PowerGrid/*` (with placeholder generation fallback).

### Runtime Power Engine
- `GraphBuilder`: scans objects, classifies nodes (cable/generator/battery/consumer/conduit), builds networks.
- `PowerManager`: simulates generation, battery drain/store, throughput cap, consumer allocation, and machine acceleration.
- `BatteryStateManager` + `FuelManager`: persistent energy/fuel bookkeeping.
- `ConduitManager`: cross-location network links.

### Integration Surfaces
- Public API: `IPowerGridApi` / `PowerGridApi`.
- Telemetry contract: writes `darth.PowerGrid/*` keys to object `modData` and intentionally keeps that prefix stable for compatibility.
- GMCM integration is implemented (`GmcmIntegration.cs`).
- Directional contract: consumer registration should be machine-mod-owned. `PowerGrid` should consume traits/registration and generic telemetry rather than owning hard-coded machine-family knowledge long-term.

### UI Layer
- `PowerTabMenu` (global overview + conduit linking).
- `PowerMonitorMenu` (location-focused monitor).
- `DebugOverlay` for world visualization.

## Integration Opportunities With This Modpack
- Register additional modpack machines as consumers (EMC/MTF ecosystems) through PowerGrid API.
- Feed PowerGrid state into control/inspection UIs (MCP-style views) via modData.
- Validate automation interactions with Automate + Better Junimos for stable throughput behavior.
- Use StardewUI as a future UI implementation path when menu complexity increases.

## Potential Improvements
- Add update keys in manifest (log flags missing update key).
- Expand compatibility test matrix around `Data/Machines` heavy packs.
- Formalize a small external integration guide for third-party consumer registration defaults.
- Keep near-term polish passes narrow and readability-focused; for example, a clearer `Wind Generator` animation/readability pass is a good fit as long as it mirrors existing vanilla-style motion instead of inventing a loose approximation.
