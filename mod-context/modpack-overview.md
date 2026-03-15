# Modpack Overview

## Source Snapshot
- Log file: `c:\Users\darth\Downloads\SMAPI-latest.txt`
- Session start: 2026-03-01 15:01:01 (local log time)
- Runtime: Stardew Valley 1.6.15 + SMAPI 4.5.1
- Loaded: 80 SMAPI mods, 36 content packs
- Skipped: 2 invalid-manifest mods (`Stairdew_Logic`, `[CP] Stairdew`)
- Note: repo-owned mod UniqueIDs later migrated to the `meiameiameia.*` namespace; the snapshot below predates that change.

## Your Mods In This Runtime
- `(SMAPI) PowerGrid` 1.0.0 (`darth.PowerGrid`)
- `(SMAPI) Metal Kegs` 1.0.0 (`darth.MetalKegs`)
- `(CP) FishSmoker Recipe` 1.0.0 (`matheus.fishsmoker.recipe`)

## Ecosystem Shape
This is a machine-heavy and infrastructure-heavy modpack with strong automation support:
- Automation backbone: `Automate`, `Better Junimos`, `WorkbenchHelper`, `Chests Anywhere`
- Machine rule layer: `ExtraMachineConfig`, `Machine Terrain Framework`, `Machine Control Panel`
- Content economy expansion: `Grapes of Ferngill`, `Cornucopia - Artisan Machines`, `Vegas' Item Compatibility Patch`
- UI/control layer: `Generic Mod Config Menu`, `StardewUI`, `Better Crafting`, `Data Layers`

## API-Rich Environment
The log reports mod APIs for:
- `Automate`, `Content Patcher`, `Chests Anywhere`
- `ExtraMachineConfig`, `Machine Terrain Framework`
- `Better Junimos`, `Better Sprinklers Plus`
- `SpaceCore`, `Fashion Sense`, `Item extensions`
- `Mail Framework`, `RushOrders`, `Special Power Utilities`
- `PowerGrid` (your mod API is exposed)

## High-Value Integration Targets
Primary targets for your mods:
- `Automate`: machine throughput and chain behavior alignment
- `ExtraMachineConfig`: recipe/machine data interoperability
- `Machine Terrain Framework`: terrain-bound machine interoperability
- `Machine Control Panel`: centralized control/reporting of machine states
- `Better Junimos`: farm labor + powered/automated production loops
- `StardewUI`: more scalable custom UI for future dashboards
- `Generic Mod Config Menu`: config UX consistency across your mods

## Operational Notes
- A large set of Harmony patchers is active, including machine-adjacent mods (`ExtraMachineConfig`, `Machine Terrain Framework`, `Machine Control Panel`, `Better Fish Ponds`, `Fish Pond Aquaponics`).
- Recipe/data conflict risk is highest in `Data/CraftingRecipes` and `Data/Machines`, where your `Metal Kegs` and `FishSmoker Recipe` operate.
