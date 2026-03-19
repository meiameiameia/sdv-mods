# Machine Systems

## Core Machine Stack In This Pack
- **ExtraMachineConfig (EMC)**: extends artisan/machine data behavior.
- **Machine Terrain Framework (MTF)**: machine-like systems on terrain/water/tapper style objects.
- **Machine Control Panel (MCP)**: centralized control/inspection for machine rules.
- **Automate**: machine IO automation orchestration.
- **Better Fish Ponds / Fish Pond Aquaponics**: specialized production systems.
- **Cornucopia - Artisan Machines / Grapes of Ferngill / Vegas compatibility patch**: expanded machine economy and inter-pack compatibility.

## Your Machine-Side Components

### PowerGrid (`meiameiameia.PowerGrid`)
- Adds power infrastructure big craftables (`cables`, `generators`, `batteries`, `conduits`).
- Builds per-location graph networks and merges linked locations via conduit links.
- Applies per-tick consumer acceleration through `MinutesUntilReady` updates.
- Exposes `IPowerGridApi` and writes rich `modData` telemetry (`darth.PowerGrid/*`, intentionally kept stable for compatibility).

### Metal Kegs (`meiameiameia.MetalKegs`)
- Adds two machine objects:
  - `darth.MetalKegs_MetalKeg`
  - `darth.MetalKegs_HardIridiumKeg`
- Clones behavior from vanilla Keg and GOF Hardwood Keg (with fallback policy).
- Injects entries into:
  - `Data/BigCraftables`
  - `Data/CraftingRecipes`
  - `Data/Machines`

## Repo Contract Notes

- `powered-machine-contract.md`: minimum ownership and registration rules for new powered machine-family mods.
- `powered-machine-vocabulary.md`: lightweight descriptor and telemetry vocabulary for powered machines.

### FishSmoker Recipe
- No machine runtime changes.
- Only edits Fish Smoker crafting recipe input requirements.

## Machine Behavior Patching Context
Log-marked code patchers include machine-adjacent mods like:
- `ExtraMachineConfig`
- `Machine Terrain Framework`
- `Machine Control Panel`
- `Better Fish Ponds`
- `Fish Pond Aquaponics`
- `Better Crafting`

This increases the importance of defensive integration style:
- soft dependency checks,
- no hard-coded assumptions on external machine keys,
- explicit conflict checking on `Data/Machines` and `Data/CraftingRecipes`.

For repo-owned powered machine mods, also use `testing/powered-machine-family-validation.md`.
