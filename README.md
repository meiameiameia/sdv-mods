# sdv-mods

Source repository for small Stardew Valley mods by meiameiameia.

## Mods

- `PowerGrid`: electrical infrastructure for Stardew Valley, with generators, batteries, conduits, powered artisan machines, and automation-friendly fuel handling. Public releases are available on [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/45572).
- `FishSmokerRecipe`: small Content Patcher recipe tweak for the Fish Smoker.

## Layout

- `mods/PowerGrid/[SMAPI] PowerGrid`
- `mods/FishSmokerRecipe/[CP] FishSmoker Recipe`
- `scripts/build-mod.ps1`

## Downloads

Packaged mod downloads are published on Nexus Mods. GitHub is source-only and may contain work-in-progress code between packaged releases.

## Build Locally

```powershell
.\scripts\build-mod.ps1 -Mods PowerGrid -Bump none
```

Local build zips are written to `artifacts/mod-zips/`.
