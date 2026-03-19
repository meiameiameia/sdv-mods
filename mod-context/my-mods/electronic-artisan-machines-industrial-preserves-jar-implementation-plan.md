# Electronic Artisan Machines: Industrial Preserves Jar Implementation Plan

## Purpose
Define the first real implementation slice for `Industrial Preserves Jar` in a way that is ready for coding, while staying inside v1 scope.

This is a structure-and-integration plan, not a runtime implementation.

## Decision: Start Mod Shell Now

The repo is ready for the new mod shell now. A separate planning pass is not needed before creating the project.

Why this is the smallest safe next step:

- contract docs already exist (`systems/powered-machine-contract.md`)
- product brief exists (`my-mods/electronic-artisan-machines.md`)
- machine brief exists (`my-mods/electronic-artisan-machines-industrial-preserves-jar.md`)
- existing SMAPI project patterns are stable and reusable

## Mod / Project Location

Create a new SMAPI mod project at:

- `[SMAPI] Electronic Artisan Machines`

Recommended core identity:

- folder: `[SMAPI] Electronic Artisan Machines`
- assembly: `ElectronicArtisanMachines.dll`
- project: `ElectronicArtisanMachines.csproj`
- unique id: `meiameiameia.ElectronicArtisanMachines`
- root namespace: `Darth.ElectronicArtisanMachines`

## First-Slice File Layout

Keep v1 minimal and similar to current SMAPI mods in this repo.

Required initial files:

- `manifest.json`
- `ElectronicArtisanMachines.csproj`
- `ModEntry.cs`
- `ModConfig.cs`
- `README.md`
- `assets/IndustrialPreservesJar.png`

Expected follow-up files in the same first implementation slice:

- `assets/IndustrialPreservesJar__unpowered.png` (optional in first coding pass)
- `assets/IndustrialPreservesJar__powered.png` (optional in first coding pass)
- `Integrations/GmcmIntegration.cs` (if meaningful config is exposed in v1)

## Data / Runtime Integration Path

The first slice should establish one machine end-to-end:

### 1) Machine registration via data edits

Use `AssetRequested` edits for:

- `Data/BigCraftables`:
  - add `darth.ElectronicArtisanMachines_IndustrialPreservesJar`
  - use a preserves-jar-style baseline machine identity
- `Data/Machines`:
  - add machine rule entry for `(BC)darth.ElectronicArtisanMachines_IndustrialPreservesJar`
  - keep standard insert/process/output behavior (no queue, no quality changes)
- `Data/CraftingRecipes`:
  - add `Industrial Preserves Jar` recipe

### 2) PowerGrid integration (optional runtime dependency)

Own registration inside the new mod:

- on `GameLaunched`, resolve `IPowerGridApi` via mod registry
- register and unregister this machine consumer from the machine mod
- respect config-owned demand/speedup/priority values
- degrade gracefully if `PowerGrid` is absent

### 3) Telemetry and ProgressText path

For this first machine, use default minute-based behavior:

- `ProgressMode`: `minutes`
- rely on PowerGrid snapshot generation for `ProgressText`
- avoid machine-specific telemetry keys unless a concrete gap is discovered

This keeps `Farm Terminal` integration simple and contract-aligned.

## Config / GMCM Plan

Keep config small in first implementation:

- `EnablePowerGridIntegration` (bool)
- `IndustrialPreservesJarEUPerMinute` (int placeholder default)
- `IndustrialPreservesJarMaxSpeedup` (float placeholder default)
- `IndustrialPreservesJarPriority` (int placeholder default)

Optional unlock config can be deferred if not needed for first runtime slice.

GMCM expectation:

- if these settings ship as player-facing config, add GMCM parity in the same hardening pass or explicitly defer it in docs.

## Validation Slice For This Machine

Run the `testing/powered-machine-family-validation.md` checklist, scoped to this one machine:

1. Base machine behavior (no PowerGrid)
2. PowerGrid registration and speedup effect
3. Automate-compatible IO
4. Snapshot and terminal observability (`ProgressText` readability)
5. Render parity only if stateful runtime art is added in this slice
6. Config/GMCM sanity

## Explicit Non-Goals For This Slice

Do not include:

- `Powered Cheese Press`
- `Powered Oil Maker`
- Cornucopia support
- queue or batching behavior
- hard power gating
- quality changes
- chipset or machine UI behavior

## Build / Packaging Readiness Notes

When this mod shell is created, update:

- `scripts/release-mod.ps1`:
  - add `ElectronicArtisanMachines` to `ValidateSet`
  - add resolver entry with folder/manifest/csproj paths
- `mod-context/versioning.md`:
  - add the new deployable mod to authoritative version/source lists

This keeps packaging parity with existing SMAPI mods.
