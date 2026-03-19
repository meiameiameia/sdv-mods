# Perfection Advisor: v1 Implementation Prep

## Purpose
Define the first implementation slice for `Perfection Advisor` before coding begins.

This is a lightweight design-to-implementation prep note, not a runtime spec.

## 1) v1 Product Role

`Perfection Advisor` v1 should be a standalone, read-only completion assistant for late-game players who want targeted guidance to finish perfection faster.

Primary value:

- show current completion status clearly
- show the next highest-value blockers
- reduce wiki/context switching without automating gameplay

## 2) Why Standalone First

- keeps spoiler/assist ownership isolated from `Farm Terminal`
- avoids coupling perfection guidance to power/machine observability surfaces
- allows separate opt-in and spoiler controls
- keeps first release focused and shippable

## 3) Exact v1 Scope

Ship one small advisor surface with actionable completion deltas.

Included in v1:

- one standalone in-game advisor UI (opened by its own keybind/command)
- read-only perfection summary with category progress
- prioritized "next blockers" list (top actionable gaps)
- spoiler guard behavior (default OFF; explicit opt-in before detailed reveals)
- no gameplay automation or control actions

Recommended v1 category coverage (small but useful):

- shipped items progress
- fish collection progress
- cooked recipes progress
- crafted items progress
- friendship progress summary
- golden walnut summary count only

## 4) Deferred Scope

Explicitly defer to later phases:

- full hint/walkthrough logic per missing target
- location-by-location routing guidance
- full museum/monster-goals deep advisor flows
- dynamic day planner/optimizer
- cross-mod perfection expansion logic
- rich Farm Terminal module ownership

## 5) Likely Data Sources / Game Surfaces To Read

Use stable in-save and game-state surfaces already tied to completion progress.

Likely read surfaces:

- player completion collections:
  - shipped items
  - fish caught
  - cooking recipes made/known
  - crafting recipes made/known
- player friendship data (hearts/status summary)
- player walnut counters/progress summary
- existing perfection/progress counters exposed by base game state where available

Do not add custom save authority in v1. Consume existing game state and summarize it.

## 6) Default-OFF Config Strategy

Keep spoiler/assist features disabled by default and require explicit opt-in.

Recommended config shape (design level):

- `EnableAdvisor` default `false`
- `EnableDetailedSpoilers` default `false`
- `ShowOnlyCategorySummary` default `true`
- `OpenAdvisorKey` configurable keybind

Behavior expectations:

- if disabled, mod stays passive and does not surface spoiler details
- enabling detailed spoilers is a second explicit step, not implied by enabling the mod
- JSON remains source of truth; GMCM parity is expected when v1 ships with meaningful settings

## 7) UI Surface Recommendation For v1

Use a dedicated standalone read-only panel, not Farm Terminal.

Recommended shape:

- compact summary header (overall completion + category counts)
- "Top blockers" list with short, scannable rows
- spoiler mode badge/state indicator

Keep interaction minimal:

- open/close
- toggle summary vs detailed spoiler view (only if enabled)
- no direct actions that change gameplay state

## 8) Future Farm Terminal Integration (Later)

If integration is added later, keep it summary-level only.

Acceptable later integration:

- optional Farm Terminal overview card:
  - overall completion percentage
  - top 1-3 blocker summaries
  - advisor enabled/spoiler mode indicator

Do not move full advisor workflows into `Farm Terminal`.

## 9) Explicit Non-Goals For v1

Do not include:

- machine/power control actions
- automated routing or task automation
- full spoiler walkthrough packs
- Cornucopia-specific perfection logic
- broad cross-mod completion frameworks
- replacing Farm Terminal as the ecosystem dashboard

## 10) Recommended First Implementation Order

1. finalize v1 category list and spoiler policy wording
2. define minimal config contract (default-off + keybind)
3. build one read-only summary aggregator from base game progress surfaces
4. add blocker-priority ranking for top actionable gaps
5. add standalone advisor UI shell (summary first, detailed spoilers gated)
6. add GMCM parity for shipped settings
7. run focused v1 validation: disabled-by-default behavior, spoiler opt-in, progress accuracy, no gameplay side effects
