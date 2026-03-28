# Farm Terminal

## Purpose
`[SMAPI] Farm Terminal` is a read-only in-game dashboard that aggregates PowerGrid snapshot data into a single StardewUI menu.

## Current Architecture

### Entry and UI Shell
- Main entry: `ModEntry.cs`.
- Opens via `farmterminal_open` or the configured keybind (`F7` by default).
- Uses StardewUI (`focustense.StardewUI`) to render a shell menu from `Assets/Views/TerminalShell.sml`.

### Data Source
- Primary data source: `IPowerGridApi` snapshot methods.
- Reads:
  - network snapshots,
  - consumer snapshots,
  - generator snapshots,
  - battery snapshots.
- Does not write back into PowerGrid or own any simulation state.
- `ProgressText` should be treated as the canonical human-readable progress/status surface for machine progress in terminal views.

### Modules
- `Overview`: high-level summary cards.
- `Power`: network topology and totals.
- `Consumers`: active, idle, and unpowered consumer list.
- `Sources`: generator and battery list.
- `Alerts`: derived read-only warnings.

## Integration Opportunities With This Modpack
- Grow into a wider farm observability surface without changing machine ownership boundaries.
- Reuse the same read-only shell pattern for future machine/storage/forecast modules.
- Stay decoupled from gameplay logic by consuming stable snapshot APIs first.

## Potential Improvements
- Add bounded auto-refresh on the same cadence as PowerGrid simulation ticks if the manual refresh model proves too stale.
- Add location-scoped drill-downs once broader multi-system terminal modules exist.
- Add dedicated tests or smoke docs once the UI shell has been runtime-smoked in-game.
- Keep future terminal modules read-focused by trusting snapshot semantics first, especially `ProgressText`, instead of re-deriving machine progress ad hoc.
- If `Perfection Advisor` is surfaced later, keep terminal integration summary-level and read-only rather than moving full execution-assistant workflows into this mod.
