# Roadmap

## Ecosystem Direction

This repository is building toward a machine/infrastructure ecosystem for Stardew Valley with:

- `PowerGrid` as the core systems/energy layer.
- `Metal Kegs` as a powered consumer module on standard machine data contracts.
- `Farm Terminal` as a read-only observability surface over PowerGrid snapshots.
- `FishSmoker Recipe` as a small progression/balance module.

The medium-term direction is to grow this into a deeper infrastructure ecosystem in the style of machine-heavy sandbox mods, but at Stardew scale:

- `PowerGrid` remains the energy/infrastructure backbone.
- machine packs remain the place where gameplay value is introduced.
- `Farm Terminal` remains the monitoring and optimization surface.
- expansion happens in paced waves, not as a one-time feature dump.

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
6. Add new powered gameplay only when it creates a meaningful late-game decision.
7. Prefer staged machine-line expansion over trying to electrify every artisan machine at once.
8. Keep machine-family identity local to the mod that owns that family; do not overload `Metal Kegs` with unrelated artisan lines.
9. Treat config parity as part of product hardening: shipped mods with meaningful player-facing config should expose that config cleanly through GMCM.

## Phase 1: Harden The Current Core

- Keep the current stable gameplay/runtime baseline intact while tightening compatibility and observability quality.
- Keep the current state-aware PowerGrid art/runtime contract stable and fallback-safe for:
  - `SteamGenerator` (`off`, `on`)
  - `WindGenerator` (`idle`, `generating`)
  - `BasicBattery` (`low`, `charged`)
  - `IridiumBattery` (`low`, `charged`)
  - `PowerConduit` (`unpaired`, `linked`)
- Keep `Farm Terminal` read-only in the near term; no control/automation ownership work in this horizon.
- Finish hardening the current ecosystem before broadening the consumer surface:
  - `PowerGrid`
  - `Metal Kegs`
  - `Farm Terminal`
- Close obvious config-surface hardening gaps in shipped mods:
  - preserve `PowerGrid` GMCM support
  - bring `Metal Kegs` to GMCM parity
  - bring `Farm Terminal` keybind/config surface to GMCM parity
- Treat current art/runtime work as good-enough productization, not as a reason to stall gameplay progress indefinitely.

## Phase 2: Productize Farm Terminal

- Improve Farm Terminal readability, module quality, and drill-down usefulness while staying read-focused.
- Keep terminal ownership narrow:
  - monitoring first
  - optimization visibility second
  - no direct control/automation ownership yet
- Get Farm Terminal to a point where broader machine expansion is easy to observe and validate.
- Continue strengthening compatibility confidence for machine-heavy stacks via focused sandbox validations.

## Phase 3: Ecosystem Contract Hardening

Insert one small contract pass before the next major machine-family wave so new powered machine mods can plug in cleanly without re-solving the same integration work.

Scope:

- machine self-description
- consumer registration ownership in machine mods instead of ad hoc recognition
- shared terminal-friendly telemetry conventions
- a repeatable starter pattern for new powered machine-family mods
- a lightweight validation matrix for new powered machine families:
  - vanilla machine behavior parity
  - PowerGrid acceleration
  - Automate-compatible IO
  - ready/processing render parity
  - Farm Terminal observability

This should stay small:

- not a big framework rewrite
- not a broad shared-core rewrite
- just enough shared shape to make the next machine-family mod cheaper and more deterministic to build

This phase should also finish current config-surface hardening:

- `PowerGrid` remains the reference implementation for GMCM parity
- `Metal Kegs` reaches GMCM parity
- `Farm Terminal` reaches GMCM parity for its user-facing config
- `FishSmoker Recipe` only grows GMCM/config support if a real preset/config surface is introduced later

## Phase 4: Electronic Artisan Line (Wave 1)

Add a small number of high-value powered artisan lines that make late-game production more interesting without turning the ecosystem into ancient-wine-only optimization.

Wave 1 priorities:

1. `Industrial Preserves Jar`
2. `Powered Cheese Press`
3. `Powered Oil Maker`
4. continued `Metal Cask` / keg-family refinement where justified

Rationale:

- these machines broaden the value of artisan processing beyond wine loops
- they create meaningful reasons to build and scale power infrastructure
- they complement existing automation instead of replacing it

Scope rules:

- do not add every artisan machine at once
- prefer a separate broader artisan-machine line rather than expanding `Metal Kegs` into a catch-all machine pack
- preserve normal machine IO behavior; power should improve throughput, availability, or constraints rather than replacing standard machine rules

## Phase 5: Electronic Artisan Line (Wave 2+)

Once Wave 1 is stable, expand into more electronic artisan variants in additional waves rather than a single all-at-once release.

Likely candidates:

- `Loom`
- `Mayonnaise Machine`
- `Dehydrator`
- `Fish Smoker`-adjacent powered processing
- other artisan processors that gain real late-game value from throughput, batching, or reduced friction

This is where the long-term goal of "electronic versions of all artisan machines" belongs:

- as a staged roadmap program
- after the current core is hardened
- after Farm Terminal is good enough to observe a larger machine ecosystem

## Phase 6: Depth Layer

Add more system depth only after there are enough powered consumers to justify it.

Preferred direction:

- shared upgrade/module system first
- machine-specific upgrade specialization later

Good initial upgrade shape:

- `Basic Control Chip`
- `Advanced Control Chip`
- `Iridium Control Chip`

Or equivalent shared module classes such as:

- speed
- efficiency
- stability / quality

This should aim for the feel of deeper machine sandbox mods without front-loading too much complexity into the early ecosystem.

## Phase 7: Deeper PowerGrid Systems

After the consumer base and terminal surfaces are stable, expand the infrastructure layer itself.

Examples:

- new generator tiers
- new storage tiers
- category or policy-based allocation
- reserve thresholds and grid-planning decisions
- richer telemetry surfaces for future terminal modules

Depth should come from interacting systems, not just from adding more placeable objects.

## Recommended Next Consumer

If the ecosystem contract phase is in place and the current core is hardened enough for the next gameplay expansion, the best first non-keg consumer is:

- `Industrial Preserves Jar`

Why:

- it gives fruit and vegetable processing a meaningful late-game lane
- it complements wine rather than duplicating it
- it broadens the artisan economy in a clean way
- it gives `PowerGrid` more value without requiring a huge new system surface

## Deferred / Optional

- Revisit broader Farm Terminal control workflows only if a clear contract and ownership boundary is justified.
- Add richer power policy/simulation features only when there is a concrete validated consumer and test coverage.
- Evaluate additional UI/system unification only after the current runtime and expansion phases are stable.
