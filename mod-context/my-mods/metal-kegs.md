# Metal Kegs

## Purpose
`[SMAPI] Metal Kegs` introduces two keg-class machines:
- `Metal Keg` (vanilla Keg behavior clone)
- `Hard Iridium Keg` (GOF Hardwood Keg behavior clone with configurable fallback)

## Current Architecture

### Entry and Runtime Detection
- Main entry: `ModEntry.cs`.
- Detects GOF by registry scan and by data template presence.

### Data Injection
- Edits `Data/BigCraftables`:
  - clones template entries for Keg and optionally Hardwood Keg.
- Edits `Data/CraftingRecipes`:
  - adds `Metal Keg` and `Hard Iridium Keg` recipes.
- Edits `Data/Machines`:
  - maps new IDs to existing machine behavior models for 1:1 processing logic.

### Config and Unlock Logic
- Config file: `ModConfig.cs` / `config.json`.
- Keys:
  - `UnlockMode` (`existingProgress`, `always`)
  - `MissingGofMode` (`disable`, `fallbackVanillaKeg`)
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
