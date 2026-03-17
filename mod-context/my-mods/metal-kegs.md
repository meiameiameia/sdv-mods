# Metal Kegs

## Purpose
`[SMAPI] Metal Kegs` introduces three machine lines:
- `Metal Cask` (industrial cask, slower than vanilla cellar aging by default, optional PowerGrid acceleration)
- `Metal Keg` (vanilla Keg behavior clone)
- `Hard Iridium Keg` (GOF Hardwood Keg behavior clone with configurable fallback)

## Current Architecture

### Entry and Runtime Detection
- Main entry: `ModEntry.cs`.
- Detects GOF by registry scan and by data template presence.

### Data Injection
- Edits `Data/BigCraftables`:
  - clones template entries for Cask, Keg, and optionally Hardwood Keg.
- Edits `Data/CraftingRecipes`:
  - adds `Metal Cask`, `Metal Keg`, and `Hard Iridium Keg` recipes.
- Edits `Data/Machines`:
  - maps new IDs to existing machine behavior models for 1:1 processing logic.
  - `Metal Cask` clones the live vanilla cask machine model and scales its aging multiplier down for slower baseline aging.

### Runtime Hooks
- Applies narrow Harmony hooks for `Metal Cask` only:
  - placement replacement so the placed object is a real `Cask` instance with a custom item ID
  - custom valid-location widening for player-owned indoor spaces
  - optional PowerGrid-driven bonus aging during day updates

### Config and Unlock Logic
- Config file: `ModConfig.cs` / `config.json`.
- Keys:
  - `UnlockMode` (`existingProgress`, `always`)
  - `MissingGofMode` (`disable`, `fallbackVanillaKeg`)
  - `MetalCaskEUPerMinute`, `MetalCaskMaxSpeedup`, `MetalCaskPriority`
- Recipe grants happen on `SaveLoaded` and `DayStarted` based on progression checks.

### Tools and Diagnostics
- Console commands for status, unlock, machine registration checks, and template sprite export.

## Integration Opportunities With This Modpack
- Maintain compatibility with EMC/Machine Control ecosystems by preserving stable machine IDs and predictable machine mappings.
- Continue leveraging Automate compatibility through standard machine registration.
- Coordinate with PowerGrid as a powered consumer pair (already active on PowerGrid side).

## Potential Improvements
- Add GMCM integration for config parity with PowerGrid.
- Add update key in manifest (log flags missing update key).
- Add explicit compatibility assertions for heavy `Data/Machines` patch stacks.
