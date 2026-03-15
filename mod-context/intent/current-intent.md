# Current Intent

## Repository Direction

This repository is building toward a machine/infrastructure ecosystem for Stardew Valley with:

- `PowerGrid` as optional energy infrastructure
- `Metal Kegs` as machine content that can integrate with `PowerGrid`
- `FishSmoker Recipe` as a lightweight balance/content tweak
- `Farm Terminal` as a read-only monitoring dashboard over `PowerGrid`

## Current Architectural Priorities

1. Keep integrations optional where practical.
2. Keep ownership local to the mod that owns the gameplay object.
3. Use shared contracts only when they remove real coupling.
4. Preserve compatibility with the broader machine-heavy modpack.
5. Keep the sandbox save and profile workflow as the main validation path.

## Current State

- Phase 1 completed: shared API introduced, `PowerGrid` decoupled from hard-coded Metal Kegs handling.
- Phase 1.5 completed: `Metal Kegs` now owns its PowerGrid tuning and GMCM surface, with legacy `PowerGrid` settings deprecated.
- Phase 2 foundation completed: `PowerGrid` now exposes read-only snapshot/query services for future monitoring UIs.
- `Farm Terminal` MVP implemented: separate read-only StardewUI shell consuming `PowerGrid` snapshots.

## Near-Term Focus

- Continue incremental integration work.
- Avoid broad framework adoption or speculative abstractions.
- Prefer small changes that preserve current sandbox-tested behavior.
