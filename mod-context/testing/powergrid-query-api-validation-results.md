# PowerGrid Query API Validation Results

## Stage 1 Status

Runtime harness added:

- `powergrid_query_dump [locationName]`

## Bug Found During Stage 1 Inspection

### Stale lifecycle telemetry

Risk:

- query snapshots could surface stale `lastReports` after save-load or return-to-title until the next simulation tick

Fix applied:

- `PowerManager.ResetRuntimeState()`
- called on `SaveLoaded`
- called on `DayStarted`
- called on `ReturnedToTitle`

## Runtime Validation Status

Pending runtime sandbox pass.

Use `powergrid_query_dump` together with `powergrid_status` across:

- Baseline
- Integrations
- Stress

Do not clear the gate for Farm Terminal MVP work until the runtime scenarios in `powergrid-query-api-validation.md` are checked in-game.
