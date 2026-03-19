# Perfection Advisor

## Purpose
`Perfection Advisor` is the planned standalone guidance mod for Stardew completion/perfection planning.

This line is the immediate next design/product focus for the repo.

## Product Direction (Current)

- standalone first (separate from `Farm Terminal`)
- assistant-style guidance for completion/perfection planning
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

Not allowed in v1:

- full advisor workflows inside `Farm Terminal`
- control/automation actions from advisor logic

## Scope Expectations (Pre-Implementation)

- keep docs and design lightweight until implementation starts
- do not broaden into broad ecosystem rewrites
- keep configuration intent aligned with repo hardening rules (meaningful player-facing settings should target GMCM parity when shipped)

## Immediate Routing

- roadmap sequencing: `roadmap.md` (Phase 4)
- ecosystem boundary: `architecture.md`
- implementation prep brief: `my-mods/perfection-advisor-implementation-plan.md`
