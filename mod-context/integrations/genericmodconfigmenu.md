# Generic Mod Config Menu Integration Target

## What It Does
`Generic Mod Config Menu` (GMCM) exposes in-game configuration UIs for compatible mods.

## Why Integration Is Useful
- This pack includes many configurable systems; in-game config consistency lowers setup friction.
- Player-facing tuning is important for balancing machine throughput in a heavily modded economy.

## Relevant Systems of Yours
- **PowerGrid**: already integrated with GMCM (`GmcmIntegration.cs`).
- **Metal Kegs**: currently file-config only (`config.json`), good candidate for parity.
- **FishSmoker Recipe**: CP-only pack; config options would require an added config framework pattern.

## Current State
- GMCM is loaded (`spacechase0.GenericModConfigMenu` 1.15.0).
- PowerGrid registers sections for unlocking, throughput, generators, batteries, fuel, Metal Keg integration settings, and debug controls.

## High-Value Next Integration Steps
- Add GMCM support to Metal Kegs for `UnlockMode` and `MissingGofMode`.
- Keep setting names aligned with existing JSON keys to avoid migration friction.
- Continue using optional runtime detection (no hard dependency requirement).
