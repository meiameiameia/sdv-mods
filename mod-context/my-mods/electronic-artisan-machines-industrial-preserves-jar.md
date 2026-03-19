# Electronic Artisan Machines: Industrial Preserves Jar

## Purpose
This is the first implementation-prep brief for `Electronic Artisan Machines`.

It defines the intended first machine in the line:

- `Industrial Preserves Jar`

This note is a bridge from product direction into later implementation. It is not a runtime spec and it does not lock final balance numbers.

For concrete first-slice project/data/runtime planning, use:

- `my-mods/electronic-artisan-machines-industrial-preserves-jar-implementation-plan.md`

## Machine Role In The Product Line

`Industrial Preserves Jar` should be the first powered artisan machine in the family because it gives the line a clear identity immediately:

- crop-driven artisan throughput
- strong late-game usefulness without duplicating the keg lane
- easy fit with standard machine IO and existing automation expectations

It should establish the baseline pattern for the broader mod:

- normal machine behavior first
- optional `PowerGrid` acceleration second
- simple observability from day one

## Why It Is The Best First Wave 1 Machine

This is the best first machine for the line because:

- it broadens artisan value beyond wine loops
- it supports fruit and vegetable processing, which is a broad and common farm lane
- it creates meaningful infrastructure value without needing a special machine UI
- it is easier to validate than a whole trio at once

If this machine works well, it becomes the template for `Powered Cheese Press` and `Powered Oil Maker` later.

## Baseline Behavior Without PowerGrid

Without `PowerGrid`, `Industrial Preserves Jar` should behave like a normal preserves-jar-style machine:

- standard placement and interaction
- standard machine IO
- standard insert / process / ready / collect loop
- no hard dependency on external power state

Design intent:

- the machine should still be useful as a standalone machine
- `PowerGrid` should improve it, not make it viable in the first place

## Powered Behavior With PowerGrid

With `PowerGrid` present, the machine should register as an optional powered consumer and gain a speed-focused benefit:

- baseline power effect: `speedup`
- no queue or batching in v1
- no hard power gating in v1
- no output quality changes in v1

The powered version should feel like:

- the same machine family
- better throughput when the farm has real infrastructure

It should not feel like a separate simulation system.

## Expected Descriptor Shape

At the design level, this machine should fit the powered-machine descriptor contract with values in this direction:

- `QualifiedItemId`: stable id for `Industrial Preserves Jar`
- `DisplayName`: `Industrial Preserves Jar`
- `FamilyId`: `ElectronicArtisanMachines`
- `FamilyName`: `Electronic Artisan Machines`
- `MachineCategory`: `artisan`
- `SupportsPower`: `true`
- `PowerBehavior`: `speedup`
- `DefaultDemandPerTick`: placeholder only for now; do not lock final number yet
- `DefaultMaxSpeedupFraction`: placeholder only for now; do not lock final number yet
- `DefaultPriority`: placeholder only for now; keep it configurable later
- `ProgressMode`: `minutes`
- `HasStatefulArt`: optional in first implementation; not required to start runtime work
- optional terminal summary hint: preserves-jar / artisan formatter if needed

## Telemetry / ProgressText Expectations

This machine should be easy for `Farm Terminal` and `PowerGrid` to observe without machine-specific guessing.

Expected behavior:

- `ProgressText` is the canonical human-readable status surface
- processing should read like normal minute-based machine progress
- powered and unpowered states should remain understandable in snapshot output
- ready state should remain clearly visible in both world behavior and terminal status

Good status examples at a design level:

- waiting for power
- powered, active
- powered, ready
- idle, unpowered

The exact wording can evolve later, but the machine should fit the current snapshot contract cleanly.

## Validation Slice

When this machine is implemented, the minimum validation slice should be:

### Base Machine Behavior
- place normally
- process correctly without `PowerGrid`
- collect output correctly
- survive save / reload

### PowerGrid Integration
- self-register as a consumer
- show meaningful powered vs unpowered distinction
- actually gain the declared speedup behavior
- remain graceful if `PowerGrid` is absent

### Automate-Compatible IO
- accept normal input
- expose normal output
- avoid custom interactions that break standard machine routing

### Terminal Observability
- appear in snapshot/status surfaces
- provide truthful `ProgressText`
- remain understandable in `Farm Terminal`

### Render Parity
- only required if custom runtime art/stateful art is added

### Config / GMCM
- only add GMCM-visible settings if the mod gains meaningful player-facing config

Use `testing/powered-machine-family-validation.md` as the parent checklist.

## Explicit Non-Goals For This First Implementation

Do not fold these into the first `Industrial Preserves Jar` implementation:

- Cornucopia support
- the full Wave 1 trio at once
- queue / batching behavior
- hard power-required operation
- output quality changes
- chipset or machine UI behavior
- remote control behavior
- machine-family framework work beyond the existing powered-machine contract

## Implementation Order Recommendation

When implementation begins, start with this machine before the rest of the family:

1. define the machine identity and baseline behavior
2. fit it to the powered-machine descriptor / registration contract
3. add `PowerGrid` speedup integration
4. add truthful snapshot / `ProgressText` behavior
5. validate the full small slice before moving to `Powered Cheese Press`
