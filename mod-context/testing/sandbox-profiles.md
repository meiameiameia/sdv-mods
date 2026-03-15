# Sandbox Profiles

The repository uses a three-profile sandbox workflow for mod validation.

## Profiles

### `SDV Sandbox Baseline`
Use for clean functional validation.

Expected core mods:

- `PowerGrid`
- `Metal Kegs`
- `FishSmoker Recipe`
- `Content Patcher`
- `Generic Mod Config Menu`
- optional utility mods like `CJB Cheats Menu`, `CJB Item Spawner`, `Lookup Anything`

### `SDV Sandbox Integrations`
Use for targeted interoperability testing.

Expected additions:

- `Automate`
- `Chests Anywhere`
- `Better Junimos`

### `SDV Sandbox Stress`
Use for heavy-stack validation.

Expected additions include machine/framework/UI-heavy mods such as:

- `ExtraMachineConfig`
- `Machine Terrain Framework`
- `Machine Control Panel`
- `StardewUI`
- `Better Crafting`
- `Data Layers`
- `Grapes of Ferngill`
- `Cornucopia - Artisan Machines`
- `Artisan Goods Keep Quality`
- `Vegas' Item Compatibility Patch`

## Save Snapshot Taxonomy

Current sandbox backup checkpoints:

- `Mod Sandbox - Phase1 Baseline Working`
- `Mod Sandbox - Pre Integrations`
- `Mod Sandbox - Integration Stable`
- `Mod Sandbox - Stress Stable`

## Usage Rule

Future agents should start testing in `Baseline`, then `Integrations`, then `Stress`.

Do not jump to the heaviest profile first unless the task is explicitly about stack conflicts.
