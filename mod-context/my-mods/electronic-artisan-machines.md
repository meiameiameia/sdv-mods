# Electronic Artisan Machines

## Purpose
`Electronic Artisan Machines` is the planned powered artisan machine-family mod for this repo.

It should stay separate from `Metal Kegs` and start with vanilla artisan machines only.

The mod's job is to introduce late-game powered artisan throughput upgrades that fit the repo's powered-machine contract from day one.

## Product Identity

- working mod name: `Electronic Artisan Machines`
- family role: powered artisan machine pack
- scope rule: vanilla artisan machines first, broader compatibility later

This line should feel like a clean extension of the farm's machine economy, not like a catch-all bucket for every machine idea.

## Why It Is Separate From Metal Kegs

- `Metal Kegs` already owns the keg/cask family.
- This line will cover multiple non-keg artisan processors.
- Future machine UI / chipset support belongs to this broader machine family, not to the keg line.
- Keeping it separate preserves clean ownership for gameplay, config, telemetry, and future art/runtime work.

## Wave 1 Scope

Do not start with every vanilla artisan machine.

Wave 1 should stay small:

1. `Industrial Preserves Jar`
2. `Powered Cheese Press`
3. `Powered Oil Maker`

Why this subset:

- it broadens late-game artisan value beyond wine loops
- it creates meaningful `PowerGrid` demand across crop, animal, and oil processing
- it keeps the first implementation/test matrix manageable

Recommended first implementation target:

- `Industrial Preserves Jar`

## Baseline Powered Behavior

Wave 1 should keep the machine contract simple:

- standard machine IO behavior
- powered speed bonus
- no queue or batching in v1
- no hard power gating in v1
- no quality changes in v1

These machines should remain useful on their own and become more valuable when connected to `PowerGrid`.

## Powered-Machine Contract Fit

This mod should use the repo's powered-machine contract from the start.

Each machine family member should be describable with the minimum shared shape:

- `QualifiedItemId`
- `DisplayName`
- `FamilyId`
- `FamilyName`
- `MachineCategory`
- `SupportsPower`
- `PowerBehavior`
- `DefaultDemandPerTick`
- `DefaultMaxSpeedupFraction`
- `DefaultPriority`
- `ProgressMode`
- `HasStatefulArt`
- optional terminal summary hint

Recommended family defaults:

- `FamilyId`: `ElectronicArtisanMachines`
- `FamilyName`: `Electronic Artisan Machines`
- `MachineCategory`: `artisan`
- `SupportsPower`: `true`
- `PowerBehavior`: `speedup`

## PowerGrid Integration

Consumer registration should be machine-mod-owned:

- `Electronic Artisan Machines` registers its own powered consumers with `PowerGrid`
- `PowerGrid` should not hard-code this family
- missing `PowerGrid` must leave the machines usable through their baseline behavior

This line should follow the same optional-integration rule as the rest of the ecosystem.

## Telemetry / ProgressText Expectations

`Farm Terminal` should not have to reverse-engineer this family.

Expectations:

- `ProgressText` is the canonical human-readable progress/status surface
- machine status should be understandable in powered, unpowered, processing, and ready states
- progress wording should stay compatible with the existing PowerGrid snapshot model

Wave 1 will likely be mostly `minutes` progress mode, but the human-readable summary should still come through `ProgressText`.

## Validation Expectations

Wave 1 should be treated as a normal powered machine-family mod and validated against:

- base machine behavior
- `PowerGrid` integration
- Automate-compatible IO
- terminal observability
- render parity if runtime stateful art is introduced
- config / GMCM sanity

Use `testing/powered-machine-family-validation.md` as the default validation checklist.

## Future Upgrade / Chipset Direction

Upgrade depth belongs to a later phase, not to v1.

Preferred future shape:

- one machine UI first
- one chipset slot first
- no multi-slot combinatorics at the beginning

Likely future chipset categories:

- throughput
- efficiency
- control
- buffer

Likely grade ladder:

- basic
- advanced
- iridium

This is a future depth layer, not a baseline behavior dependency.

## Explicit V1 Non-Goals

Do not include these in the first release:

- all vanilla artisan machines
- Cornucopia machine support
- queue or batching systems
- hard power-required operation
- quality-changing power bonuses
- machine UI
- live chipset installation
- remote control behavior

## Recommended First Implementation Order

1. `Industrial Preserves Jar`
2. `Powered Cheese Press`
3. `Powered Oil Maker`

That order keeps the first implementation aligned with the roadmap's highest-value non-keg consumer and limits early balance and compatibility noise.
