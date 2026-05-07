# sdv-mods

Source repository for Stardew Valley mods by meiameiameia.

This repo is source-first. Public downloads are published on Nexus Mods; GitHub is used for source, history, and in-progress development.

## Released Mods

| Mod | Type | Summary | Downloads |
| --- | --- | --- | --- |
| `PowerGrid` | SMAPI mod | Electrical infrastructure for Stardew Valley: generators, batteries, cables, conduits, powered artisan machines, automation-friendly fuel handling, and a public integration API. | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/45572) |
| `ProspectorsPan` | SMAPI mod | Lightweight panning improvements: more reliable reachable spots, modest progression-aware bonus rewards, configurable hints, and optional GMCM support. | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/45921) |
| `FishSmokerRecipe` | Content Patcher pack | Small recipe tweak for the Fish Smoker. | Source only |

## Stewarded Mods

These are community mods maintained or preserved here with clear provenance and a narrow maintenance-first posture.

| Mod | Summary |
| --- | --- |
| `MatrixFishingUI` | Stewarded maintenance copy of Matrix Fishing UI by Script Kitty / LetsTussleBoiz. See `stewarded/MatrixFishingUI` for source notes and release context. |

## Repository Layout

```text
mods/
  FishSmokerRecipe/
  PowerGrid/
  ProspectorsPan/
stewarded/
  MatrixFishingUI/
scripts/
  build-mod.ps1
```

## Building Locally

Use the repo build helper for released SMAPI mods:

```powershell
.\scripts\build-mod.ps1 -Mods PowerGrid -Bump none
```

Individual SMAPI projects can also be built directly:

```powershell
dotnet build .\mods\ProspectorsPan\ProspectorsPan.csproj -c Release
```

## Downloads And Packages

Packaged mod downloads are distributed through Nexus Mods, not GitHub.

Generated packages, release candidates, local install snapshots, balance reports, and build outputs belong under `artifacts/` or build folders and should not be committed.

Common local artifact folders:

- `artifacts/release-candidates/<mod>/<version>/` for zips ready for testing.
- `artifacts/local-packages/<mod>/latest/` for local-only packages that are not release or test zips.
- `artifacts/balance-lab/<YYYYMMDD>-<topic>/<command-or-suite>/` for generated balance reports.

## Requirements

Most SMAPI mods in this repo target:

- Stardew Valley 1.6+
- SMAPI 4.0.0+

Check each mod folder for its own README, manifest, compatibility notes, and optional integration details.
