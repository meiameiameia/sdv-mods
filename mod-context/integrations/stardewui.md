# StardewUI Integration Target

## What It Does
`StardewUI` is a declarative UI framework for Stardew Valley mods.

## Why Integration Is Useful
- Your `PowerGrid` UI already has non-trivial scope (global tab, monitor, conduit linking, debug overlays).
- A declarative UI layer can reduce menu maintenance and make future dashboards easier to evolve.

## Relevant Systems of Yours
- **PowerGrid**: strongest candidate for UI migration or hybrid usage.
- **Metal Kegs**: candidate for future in-game status/config panel if scope expands.

## Current State
- StardewUI is installed (`focustense.StardewUI` 0.6.1).
- Your current UI is implemented with `IClickableMenu` and custom drawing.

## High-Value Next Integration Steps
- Keep existing `PowerTabMenu` as baseline behavior and prototype equivalent StardewUI components incrementally.
- Preserve existing telemetry contracts (`PowerGrid` API + `modData`) so UI technology can change without logic rewrites.
- Prioritize read-only monitoring views first, then interactive conduit-management UI.
