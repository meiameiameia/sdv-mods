# sdv-mods

Small Stardew Valley mods by meiameiameia.

## Mods

- `PowerGrid`: electrical infrastructure for Stardew Valley, with generators, batteries, conduits, powered artisan machines, and automation-friendly fuel handling.
- `FishSmokerRecipe`: small Content Patcher recipe tweak for the Fish Smoker.

## Layout

- `mods/PowerGrid/[SMAPI] PowerGrid`
- `mods/FishSmokerRecipe/[CP] FishSmoker Recipe`
- `scripts/release-mod.ps1`

## Build

```powershell
.\scripts\release-mod.ps1 -Mods PowerGrid -Bump none
```

Packaged zips are written to `artifacts/mod-zips/`.
