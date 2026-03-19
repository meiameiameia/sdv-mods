# Powered Machine Vocabulary

## Purpose
This file defines the minimum shared vocabulary for powered machine descriptors and terminal-friendly telemetry.

Keep it small. Add new fields only when they remove real repeated ambiguity.

## Descriptor Vocabulary

- `QualifiedItemId`: stable machine identity
- `DisplayName`: player-facing name
- `FamilyId`: owning machine-family mod id or local family key
- `FamilyName`: human-readable family label
- `MachineCategory`: broad grouping such as `artisan`, `aging`, `infrastructure`, `storage`
- `SupportsPower`: whether optional PowerGrid integration exists
- `PowerBehavior`: baseline power effect such as `speedup` or `day_bonus`
- `DefaultDemandPerTick`: owner default power demand
- `DefaultMaxSpeedupFraction`: owner default speed cap
- `DefaultPriority`: owner default PowerGrid allocation priority
- `ProgressMode`: `minutes`, `days`, or `discrete`
- `HasStatefulArt`: whether runtime stateful art is expected
- `TerminalSummaryHint`: optional hint for a family-appropriate summary formatter

## Snapshot / Telemetry Vocabulary

These are the minimum useful read surfaces for powered machine observability:

- `DisplayName`
- `QualifiedItemId`
- `FamilyId`
- `MachineCategory`
- `LocationName`
- `Tile`
- `NetworkId`
- `IsPowered`
- `IsProcessing`
- `DemandPerTick`
- `EUAllocated`
- `SpeedupFraction`
- `Priority`
- `ProgressMode`
- `ProgressText`

## Read Rules

- `ProgressText` is the human-readable truth surface.
- `ProgressMode` explains how the progress should be interpreted.
- machine mods may expose additional machine-specific details, but they should not force `Farm Terminal` to reverse-engineer core progress meaning.

## Optional Machine-Owned Fields

Use only when they are genuinely helpful:

- `StateName`
- `TerminalStatusLabel`
- `LastMeaningfulAcceleration`
- future module / chipset summary
