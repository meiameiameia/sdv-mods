# Roadmap

## Direction

This repository is building toward a machine/infrastructure ecosystem for Stardew Valley with:

- `PowerGrid` as optional energy infrastructure
- `Metal Kegs` as machine content that can integrate with `PowerGrid`
- `FishSmoker Recipe` as a lightweight balance/content tweak
- `Farm Terminal` as a read-only monitoring dashboard over `PowerGrid`

## Current Stable Baseline

- Phase 1 completed: shared API introduced and `PowerGrid` no longer hard-codes `Metal Kegs`.
- Phase 1.5 completed: `Metal Kegs` owns its `PowerGrid` tuning and GMCM surface.
- Phase 2 completed: `PowerGrid` exposes read-only snapshot/query services.
- Runtime validation is in a usable state for local networks, lifecycle transitions, exact-name filtering, and conduit-linked Farm/FarmHouse merged reporting.
- `Farm Terminal` MVP exists as a separate read-only StardewUI shell over `PowerGrid` snapshots.

## Working Principles

1. Keep integrations optional where practical.
2. Keep ownership local to the mod that owns the gameplay object.
3. Use shared contracts only when they remove real coupling.
4. Preserve compatibility with the broader machine-heavy modpack.
5. Keep sandbox validation as the main runtime gate.

## Near-Term Roadmap

### Maintain

- Keep `PowerGrid`, `Metal Kegs`, and `Farm Terminal` behavior stable under the existing sandbox workflow.
- Prefer narrow compatibility and observability improvements over feature sprawl.

### Extend Carefully

- Grow `Farm Terminal` only as a read-focused system surface unless a stronger control contract is justified.
- Keep future machine integrations on the shared API/modData path rather than reintroducing cross-mod hard-coupling.

### Avoid

- Broad framework churn.
- Speculative abstractions without a concrete consumer.
- Feature work that outruns runtime validation.
