# Machine Terrain Framework Integration Target

## What It Does
`Machine Terrain Framework` (MTF) provides infrastructure for terrain-bound machine behaviors (custom tappers, crab pots, water crops).

## Why Integration Is Useful
- MTF broadens "machine-like" production outside standard big craftables.
- PowerGrid can become a unifying speed/energy layer across both standard and terrain systems.

## Relevant Systems of Yours
- **PowerGrid**: best fit; its API supports custom consumer registration by qualified item ID.
- **Metal Kegs**: limited direct overlap (standard machine path already covered).

## Current State
- MTF API is exposed in runtime (`Selph.StardewMods.MachineTerrainFramework.MachineTerrainFrameworkApi`).
- No direct MTF bridge exists in your current code.

## High-Value Next Integration Steps
- Build an optional adapter that registers selected MTF machines as PowerGrid consumers.
- Use conservative defaults (demand, speedup cap, priority) to avoid over-accelerating terrain systems.
- Surface powered-state feedback through PowerGrid modData for easier diagnostics.
