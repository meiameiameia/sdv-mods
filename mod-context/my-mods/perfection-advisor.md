# Perfection Advisor

## Purpose
`Perfection Advisor` is the standalone read-only execution assistant for Stardew completion/perfection planning.

The point of this mod is not just to report progress. The point is to reduce wiki/context switching and let the player glance at the advisor, decide quickly, and act immediately toward perfection.

## Product Direction (Current)

- standalone first (separate from `Farm Terminal`)
- assistant-style guidance for completion/perfection planning
- execution-oriented, read-only guidance:
  - what still matters
  - what can be done today
  - where to go / who to target next
- spoiler/assist behavior only
- default-off guidance surfaces so players opt in deliberately

## Why Standalone First

- keeps advisory/spoiler behavior isolated from machine/infrastructure runtime code
- avoids overloading `Farm Terminal` ownership and UI scope
- allows independent pacing and tuning for players who want assistance versus players who do not

## Boundary With Farm Terminal

`Farm Terminal` remains read-only and should not absorb full advisor ownership in v1.

Allowed future connection:

- optional summary-level read integration in terminal views, if useful later
- optional compatibility with map/overlay mods for read-only action cues

Not allowed in v1:

- full advisor workflows inside `Farm Terminal`
- control/automation actions from advisor logic

## Scope Expectations (Pre-Implementation)

- keep docs and design lightweight until implementation starts
- do not broaden into broad ecosystem rewrites
- keep configuration intent aligned with repo hardening rules (meaningful player-facing settings should target GMCM parity when shipped)

## Current Near-Term Goal

The current work on this line should converge on one actionability slice before the mod is paused for a while:

- friendship execution guidance with meaningful next-action surfaces
- current-day prioritization that is immediately usable
- map/current-location compatible guidance where it removes real friction
- trust-safe wording whenever the underlying logic is heuristic rather than canonical

This line should pause once it reliably supports glance -> decide -> act workflows for real late-game saves, rather than growing sideways into every possible perfection subsystem at once.

## Immediate Routing

- roadmap sequencing: `roadmap.md` (Phase 4)
- ecosystem boundary: `architecture.md`
- implementation prep brief: `my-mods/perfection-advisor-implementation-plan.md`
