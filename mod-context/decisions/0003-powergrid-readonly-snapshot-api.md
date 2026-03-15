# Decision 0003: PowerGrid Read-Only Snapshot API

## Status
Accepted

## Decision

Add a lightweight read-only snapshot/query surface to `PowerGrid` for monitoring consumers and future UI consumers.

The query layer exposes:

- network summaries
- consumer snapshots
- generator snapshots
- battery snapshots

It remains read-only and does not add remote control behavior.

## Why

The repository needs a stable way for future UI mods, especially a future `Farm Terminal`, to read power state without:

- scraping `modData`
- duplicating simulation logic
- coupling directly to internal `PowerGrid` classes

## Consequences

- `PowerGrid` now has a small monitoring-oriented query layer in addition to consumer registration.
- Current `PowerGrid` gameplay and menus remain unchanged.
- Future terminal-style UI can start with snapshot reads before any control workflows exist.
