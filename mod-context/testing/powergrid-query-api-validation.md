# PowerGrid Query API Validation

This file defines the smallest useful runtime validation plan for the Phase 2 read-only snapshot/query API.

It is intended to raise confidence before any `Farm Terminal` UI work begins.

## Scope

Validate the read-only `PowerGrid` query surface for:

- network summaries
- consumer snapshots
- generator snapshots
- battery snapshots

This is not a full QA plan. It is a targeted confidence check for the new API layer.

## Highest-Risk Runtime Scenarios

### 1. Empty state / world-not-ready
Expected behavior:

- snapshot methods return empty collections
- no exceptions on title screen or before a save is loaded

### 2. First-tick state after loading a save
Expected behavior:

- topology snapshots can exist immediately after load
- last-tick values may be zero before the first simulation tick
- no stale values should appear from a previous session

### 3. Save-load persistence
Expected behavior:

- snapshots still reflect the correct network topology after reload
- last-tick fields recover correctly after the next simulation tick

### 4. Return-to-title then reload
Expected behavior:

- no stale report data leaks across title transitions
- snapshot methods do not report old allocations for a newly loaded world

### 5. Empty or idle network
Expected behavior:

- network snapshots still show counts/capacity
- consumer snapshots show idle/unpowered state correctly
- generator/battery snapshots remain coherent even when no active processing exists

### 6. Active local network
Expected behavior:

- consumer allocations match `powergrid_status`
- battery/generator fields match current runtime state

### 7. Cross-location conduit network
Expected behavior:

- conduit-linked networks appear as one merged runtime network
- `LocationNames` and per-location filtering behave predictably
- generator/storage summaries remain coherent across linked locations

### 8. Integration stack coexistence
Expected behavior:

- snapshot reads remain coherent in `Integrations` and `Stress` profiles
- no obvious corruption from `Automate`, GOF, or machine-framework-heavy stacks

## Likely Risks To Watch

1. `lastReports` may remain stale across save-load or return-to-title boundaries until the next simulation tick.
2. Generator `TotalGenerationPerTick` in network snapshots is base generation, while `LastTickGenerated` is actual last-tick output; consumers must not assume those are identical.
3. Location filtering is exact-name based and should be checked against farm interiors and conduit-linked locations.
4. Query methods rebuild merged network views on demand, so they should not be polled aggressively without UI-side refresh limits.

## Minimal Validation Sequence

Use the built-in runtime harness command:

```text
powergrid_query_dump [locationName]
```

Compare its output against:

```text
powergrid_status
```

Interpretation notes:

- If topology is present immediately after load but all `lastTick(...)`, `generated`, `drained`, `stored`, or allocation values are zero, treat that as expected until the first 10-minute simulation tick runs.
- `locationName` filtering is exact and uses runtime location names such as `Farm`, `Cellar`, or shed interior `NameOrUniqueName` values, not player-facing display labels.
- For conduit-linked merged networks, filtering by one member location still returns the merged network summary with all `LocationNames`; only the per-tile consumer/generator/battery lists narrow to the exact filtered location.
- `powergrid_status` remains current-location and local-network oriented; use it to cross-check per-location machine/battery state, but treat `powergrid_query_dump` as the source of truth for merged conduit-network totals.

### Baseline

1. Launch `SDV Sandbox Baseline`.
2. Load `Mod Sandbox`.
3. Run `powergrid_query_dump` at title or before a save is loaded.
4. Check:
   - empty/title-safe behavior
   - first-load-before-tick behavior
   - one active local network with powered consumer
   - save/load behavior

### Integrations

1. Launch `SDV Sandbox Integrations`.
2. Re-run the same query harness.
3. Check:
   - active `Metal Keg` with `Automate`
   - idle/active consumer transitions
   - no desync between query output and `powergrid_status`

### Stress

1. Launch `SDV Sandbox Stress`.
2. Re-run the same query harness.
3. Check:
   - dual-consumer powered network
   - GOF-backed `Hard Iridium Keg`
   - save/reload under the heavy stack
   - conduit-linked network if available in the sandbox

## Runtime Acceptance Threshold

The API is ready for a read-only `Farm Terminal` MVP when:

- snapshot methods are safe before and after load,
- active network values match existing debug/status output,
- no stale cross-session data appears,
- conduit-linked networks produce coherent merged summaries.
