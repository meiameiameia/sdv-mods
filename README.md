# sdv-mods

Source repository for small Stardew Valley mods by meiameiameia.

## Mods

- `PowerGrid`: electrical infrastructure for Stardew Valley, with generators, batteries, conduits, powered artisan machines, and automation-friendly fuel handling. Public releases are available on [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/45572).
- `FishSmokerRecipe`: small Content Patcher recipe tweak for the Fish Smoker.

## Stewarded Mods

These are community mods maintained or preserved here with clear provenance and a narrow maintenance-first posture.

- `MatrixFishingUI`: stewarded maintenance copy of Matrix Fishing UI by Script Kitty / LetsTussleBoiz. See [`stewarded/MatrixFishingUI`](stewarded/MatrixFishingUI) for source notes and release context.

## Layout

- `mods/PowerGrid`
- `mods/FishSmokerRecipe`
- `stewarded/MatrixFishingUI`
- `scripts/build-mod.ps1`

## Downloads

Packaged mod downloads are published on Nexus Mods. GitHub is source-only and may contain work-in-progress code between packaged releases.

## Build Locally

```powershell
.\scripts\build-mod.ps1 -Mods PowerGrid -Bump none
```

## Local Artifacts

`artifacts/` is ignored by Git. Do not commit build outputs, generated reports, or zip packages. Published package files are distributed through Nexus Mods, not GitHub.

Use these folders:

- `artifacts/release-candidates/<mod>/<version>/` for zips ready for testing.
- `artifacts/local-packages/<mod>/latest/` for local-only packages that are not release or test zips.
- `artifacts/balance-lab/<YYYYMMDD>-<topic>/<command-or-suite>/` for generated balance reports.
