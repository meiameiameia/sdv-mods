# ExtraMachineConfig Integration Target

## What It Does
`ExtraMachineConfig` extends machine artisan recipe and data behavior beyond vanilla `Data/Machines` limits.

## Why Integration Is Useful
- This pack uses broad machine expansion; EMC is a common compatibility anchor.
- `Metal Kegs` lives directly in `Data/Machines`, where EMC-oriented packs frequently operate.
- Power-aware balancing can be cleaner when machine definitions are normalized through EMC-aware conventions.

## Relevant Systems of Yours
- **Metal Kegs**: strongest target; machine keys and behavior cloning should stay EMC-compatible.
- **PowerGrid**: can power EMC-defined machines if they are registered as PowerGrid consumers (directly or via a bridge mod).

## Current State
- EMC API is exposed in the runtime log (`Selph.StardewMods.ExtraMachineConfig.ExtraMachineConfigApi`).
- Your mods do not currently call EMC API directly.

## High-Value Next Integration Steps
- Add compatibility tests for Metal Kegs when EMC-altered machine rules are present.
- Define optional mapping docs for EMC-managed machine IDs to PowerGrid consumer registration.
- Keep fallback behavior safe when external machine keys are renamed by compatibility packs.
