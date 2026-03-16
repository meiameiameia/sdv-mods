# Vanilla Minimum Crop Set

Prepare this minimum set from local vanilla game assets for external sprite generation runs.

This keeps the workflow lightweight while still giving strong style authority references.

## Required Input Sources (local, not committed)

- `Craftables.png`
- `Floors.png`
- `Flooring.png`
- `Flooring_winter.png`
- `master_64.txt`

## Required Output Crops (small curated bundle)

Save these as separate files in a local working folder when preparing prompts.

### Craftables authority crops

- `craftables-keg-authority.png`
- `craftables-machine-authority-a.png`
- `craftables-machine-authority-b.png`
- `craftables-battery-like-authority.png`

Use these for:

- `MetalKeg`
- `HardIridiumKeg`
- `SteamGenerator`
- `WindGenerator`
- `BasicBattery`
- `IridiumBattery`
- `PowerConduit`

### Modular flooring authority crops

- `flooring-modular-authority-a.png` (from `Floors.png`)
- `flooring-modular-authority-b.png` (from `Flooring.png`)
- `flooring-modular-authority-winter.png` (from `Flooring_winter.png`)

Use these for:

- `CopperCable`
- `IronCable`
- `IridiumCable`

### Palette discipline reference

- `master64-palette-reference.txt` (copy from local `master_64.txt`)

Use this only for palette/value discipline, not as shading authority.

## Provenance Notes

When you produce external-generation prompts, include:

- source game version,
- source file names used,
- date extracted,
- any manual crop comments.

Keep that run log outside gameplay asset folders.

## Current Repo Snapshot

This repo currently ships a minimal equivalent crop bundle using these file names:

- `craftables-machine-authority-a.png`
- `craftables-machine-authority-b.png`
- `craftables-machine-authority-c.png`
- `floors-modular-authority.png`
- `flooring-modular-authority.png`
- `flooring-winter-modular-authority.png`
