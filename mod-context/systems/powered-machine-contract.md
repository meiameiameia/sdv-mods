# Powered Machine Contract

## Purpose
Define the minimum useful contract for repo-owned powered machine-family mods.

This contract is intentionally small. It exists so new powered machine mods can plug into:

- `PowerGrid`
- `Farm Terminal`
- Automate-style IO
- repo validation flow

without re-solving the same ownership and telemetry questions each time.

## Core Rules

1. Machine-family mods own machine identity, gameplay, default power tuning, config, and machine-specific telemetry details.
2. `PowerGrid` owns power simulation, allocation, and generic snapshots.
3. `Farm Terminal` owns read-only presentation.
4. Consumer registration should be machine-mod-owned.
5. Powered machines should remain graceful when `PowerGrid` is absent.
6. Standard machine IO behavior should remain compatible with Automate-style workflows.

## Minimum Machine Self-Description

Each powered machine family should be describable with this minimum shape:

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
- optional terminal summary hint / formatter hint

This is a descriptor shape, not a required base class or framework.

## Consumer Registration

Consumer registration should follow this model:

- machine mod registers its own powered machines with `PowerGrid`
- `PowerGrid` consumes traits and registration, not hard-coded machine-family knowledge
- missing `PowerGrid` must leave the machine usable through its base behavior

## Terminal Contract

For human-readable progress/status:

- `ProgressText` is the canonical read surface
- `Farm Terminal` should display `ProgressText`, not reinterpret machine progress rules ad hoc

## Config / GMCM Expectation

Shipped SMAPI mods with meaningful player-facing config should aim for GMCM parity as part of hardening.

Meaningful config includes things like:

- power demand
- max speedup
- priority
- unlock mode
- major compatibility fallback mode

## Out Of Scope

This contract does not currently define:

- Cornucopia-specific support
- producer/storage abstractions
- multi-slot module systems
- machine UI
- remote control behavior
- a shared runtime framework rewrite
