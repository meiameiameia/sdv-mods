# Automate Integration Target

## What It Does
`Automate` connects chests and machines into processing chains so machine IO runs without manual interaction.

## Why Integration Is Useful
- Your pack is machine-heavy; Automate is the core throughput orchestrator.
- `Metal Kegs` benefits immediately if treated as normal keg-class machines.
- `PowerGrid` acceleration compounds with automated IO for meaningful production scaling.

## Relevant Systems of Yours
- **Metal Kegs**: already compatible by design through `Data/Machines` registration.
- **PowerGrid**: currently indirect integration (speedup affects any processing machine state), with no direct Automate API call.
- **FishSmoker Recipe**: recipe pacing influences when automated fish-smoking loops become available.

## Current State
- Confirmed runtime presence: `Pathoschild.Automate` 2.4.3 with API exposed.
- No hard dependency from your mods.

## High-Value Next Integration Steps
- Validate Metal Keg and Hard Iridium Keg behavior under full Automate chains.
- Add optional observability bridges (for example: powered/unpowered state surfaced where automation overlays inspect machine state).
- Keep machine logic deterministic so automation + speedup interactions stay predictable.
