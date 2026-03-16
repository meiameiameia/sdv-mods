# Roadmap

## Ecosystem Direction

This repository is building toward a machine/infrastructure ecosystem for Stardew Valley with:

- `PowerGrid` as the core systems/energy layer.
- `Metal Kegs` as a powered consumer module on standard machine data contracts.
- `Farm Terminal` as a read-only observability surface over PowerGrid snapshots.
- `FishSmoker Recipe` as a small progression/balance module.

## Current Stable Pillars

- `PowerGrid` exposes stable read-only snapshots for networks, consumers, generators, and batteries.
- `PowerGrid` runtime behavior has a validated baseline for local networks, lifecycle transitions, exact-name filtering, and conduit-linked Farm/FarmHouse merged reporting.
- `Metal Kegs` is integrated as a powered consumer module through the current PowerGrid contract.
- `Farm Terminal` MVP is live as a separate StardewUI read-only shell over PowerGrid snapshots.
- The sprite workflow is mature enough to support one-sprite generation with explicit specs, prompt mapping, and state-aware art contracts.

## Working Principles

1. Keep integrations optional where practical.
2. Keep ownership local to the mod that owns the gameplay object.
3. Use shared contracts only when they remove real coupling.
4. Preserve compatibility with the broader machine-heavy modpack.
5. Keep sandbox validation as the main runtime gate.

## Near-Term Productization

- Keep the current stable gameplay/runtime baseline intact while tightening compatibility and observability quality.
- Implement PowerGrid runtime sprite state transition support aligned to the current art contract for:
  - `SteamGenerator` (`off`, `on`)
  - `WindGenerator` (`idle`, `generating`)
  - `BasicBattery` (`low`, `charged`)
  - `IridiumBattery` (`low`, `charged`)
  - `PowerConduit` (`unpaired`, `linked`)
- Require safe fallback to the current single default sprite whenever a state variant asset is missing, invalid, or not yet shipped.
- Keep `Metal Kegs` single-state for now; do not add powered/unpowered visuals unless later runtime value is clearly justified.
- Keep `Farm Terminal` read-only in the near term; no control/automation ownership work in this horizon.

## Next Ecosystem Expansion

- Expand PowerGrid consumer coverage for additional machine families through the existing API/modData integration path.
- Improve Farm Terminal observability depth (module quality, drill-down quality, bounded refresh behavior) while staying read-focused.
- Strengthen compatibility confidence for machine-heavy stacks via focused sandbox validations.

## Later / Optional

- Revisit broader Farm Terminal control workflows only if a clear contract and ownership boundary is justified.
- Add richer policy/simulation features only when there is a concrete validated consumer and test coverage.
- Evaluate additional UI/system unification only after near-term runtime and productization goals are stable.
