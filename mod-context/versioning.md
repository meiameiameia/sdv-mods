# Version Ownership

This repository keeps versions independent per deployable mod.

## Authoritative Version Source

`manifest.json` `Version` is the authoritative runtime version source for each deployable mod:

- `[SMAPI] PowerGrid/manifest.json`
- `[SMAPI] Metal Kegs/manifest.json`
- `[SMAPI] Farm Terminal/manifest.json`
- `[SMAPI] Electronic Artisan Machines/manifest.json`
- `[CP] FishSmoker Recipe/manifest.json`

## C# Version Sync

For C# mods, assembly metadata is explicit and synced from manifest version when packaging:

- `<Version>`
- `<AssemblyVersion>` (`{manifestVersion}.0`)
- `<FileVersion>` (`{manifestVersion}.0`)
- `<InformationalVersion>`

Projects:

- `[SMAPI] PowerGrid/PowerGrid.csproj`
- `[SMAPI] Metal Kegs/MetalKegs.csproj`
- `[SMAPI] Farm Terminal/FarmTerminal.csproj`
- `[SMAPI] Electronic Artisan Machines/ElectronicArtisanMachines.csproj`
- `shared/DarthMods.API/DarthMods.API.csproj`

`DarthMods.API` is not a deployable mod zip target, but it stays explicit and aligned when C# mods are packaged.

## Packaging Workflow

Use:

`scripts/release-mod.ps1`

This script performs the smallest required release tasks:

1. bumps `manifest.json` version (`patch`/`minor`/`major` or `-SetVersion`),
2. syncs C# project version metadata from the manifest version,
3. builds C# mods,
4. writes installable zip packages,
5. keeps only the newest and immediate previous zip per mod in the active package folder and archives older zips outside it.

Zip output directory:

- default: `artifacts/mod-zips/`
- override with `-OutputDir`

Zip archive directory:

- default sibling archive: `artifacts/mod-zips-archive/`
- if `-OutputDir` is overridden, archive zips are moved to a sibling folder named `[OutputDirLeaf]-archive`

### Examples

- Bump patch + package PowerGrid:
  - `.\scripts\release-mod.ps1 -Mods PowerGrid -Bump patch`
- Bump minor + package Metal Kegs:
  - `.\scripts\release-mod.ps1 -Mods MetalKegs -Bump minor`
- Set exact version + package CP mod:
  - `.\scripts\release-mod.ps1 -Mods FishSmoker -SetVersion 1.2.0`
- Bump patch + package multiple changed mods in one run:
  - `.\scripts\release-mod.ps1 -Mods PowerGrid,MetalKegs -Bump patch`
- Package Electronic Artisan Machines without changing its current version:
  - `.\scripts\release-mod.ps1 -Mods ElectronicArtisanMachines -Bump none`
- Package Farm Terminal without changing its current version:
  - `.\scripts\release-mod.ps1 -Mods FarmTerminal -Bump none`

## When To Bump Version

Bump when a mod's deployable behavior or assets change, including:

- gameplay behavior changes,
- data/content changes shipped to players,
- integration/API surface changes in that mod,
- config surface changes,
- localization/textures/assets that affect shipped output.

No bump is needed for non-deployable-only repo edits (for example, internal docs with no shipped mod change).

## Executor Rule

After any mod-changing work:

1. run `scripts/release-mod.ps1` for changed deployable mods,
2. let it handle version bump + sync + build + zip retention/archive,
3. attach/use zip(s) from `artifacts/mod-zips/`.

This remains intentionally lightweight: no CI, no tags, no changelog framework.
