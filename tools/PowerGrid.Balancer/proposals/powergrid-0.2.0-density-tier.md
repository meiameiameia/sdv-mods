# PowerGrid 0.2.0 Density Tier Brief

This is the first real `0.2.0` target brief.

It is grounded in the live `0.1.2` defaults already in the repo:

- Steam Generator: `75 EU/tick`
- Combustion Generator: `240 EU/tick`
- Wind Generator: `25 EU/tick`
- Cable caps: `50 / 250 / 1000`
- Biofuel: `Fiber x10`, `Wood x5`, `Coal x1`, crafts `8`

That matters because `0.2.0` should not be a disguised `0.1.x` rebalance patch. Early and midgame are already serviceable. The problem is mature conversion density.

## Current Live Signal

Balance Lab v2 says the late-game pain is not one broken number. It is infrastructure sprawl.

### Mature Y3 Organized Conversion

- Demand: `3310 EU/tick`
- Current minimum plan: `14x Combustion Generator`
- Current comfort plan: `16x Combustion Generator`
- Current cable plan: `4x Iridium Cable zones`
- Current battery runtime at full demand: `24.17 minutes`
- Main setup gaps: `Iron Bar`, `Fiber`, `Copper Bar`, `Refined Quartz`

### Mature Y3 Cellar Expansion

- Demand: `5310 EU/tick`
- Current plan: `23x Combustion Generator`
- Current cable plan: `6x Iridium Cable zones`

### What This Means

`0.1.2` is doing its job:

- Steam teaches the system.
- Combustion is a real midgame upgrade.
- Wind adds passive outdoor support.

What it does **not** do well is support organized late-game adoption without generator clutter and over-zoned wiring.

That is exactly what `0.2.0` should solve.

## Decision

`0.2.0` should add a **paired late-game density tier**:

1. one **higher-output generator**
2. one **higher-capacity transmission tier**

Do not ship only one half.

A stronger cable alone still leaves mature saves drowning in Combustion Generators. A stronger generator alone still leaves large rooms split into too many Iridium zones. The late-game answer needs both.

## Chosen Direction

### Generator: Radioisotope Generator

Preferred theme:

- late-game radioactive power
- clearly stronger than Combustion
- expensive, advanced, and compact

Prototype target:

- Output: `900 EU/tick`

Why `900`:

- Mature organized conversion (`3310`) becomes `4` generators minimum or `5` with comfort headroom.
- Mature cellar expansion (`5310`) becomes `6` generators minimum.
- That solves clutter without making a single machine do everything.

The generator should feel like a premium infrastructure upgrade, not a casual replacement for Steam or Combustion.

### Transmission: Energized Iridium Cable

Preferred theme:

- same family as the existing Copper -> Iron -> Iridium ladder
- clearly late-game
- supports denser rooms without changing early wiring behavior

Prototype target:

- Throughput: `3000 EU/tick`

Why `3000`:

- Mature organized conversion (`3310`) becomes a realistic `2-zone` plan.
- Mature cellar expansion (`5310`) becomes `2` very full zones or `3` comfortable zones.
- It preserves the idea that layout still matters, while ending the current "too many small zones" problem.

## Unlock Placement

Add one new bundle after `Advanced Grid`.

Working name:

- `High Density Grid`

Working unlock target:

- `Mining 10`
- know `Lightning Rod`
- know `Solar Panel`

Why this shape:

- it fits the existing unlock implementation style
- it lands clearly after current Biofuel progression
- it reads like advanced utility infrastructure, not a random extra drop

If `Solar Panel` proves too narrow in practice, the fallback is:

- `Mining 10`
- know `Lightning Rod`
- already qualified for `Advanced Grid`

## First Prototype Scope

Keep the first `0.2.0` prototype disciplined.

### Include

- new `Radioisotope Generator` object definition
- new `Energized Iridium Cable` object definition
- new unlock bundle and recipe grant path
- Power Tab and query support for the new tier
- Balance Lab comparison against current `0.1.2`

### Explicitly Defer

- battery slot / power-cell behavior
- emergency reserve mechanics
- fluid systems
- under-cable placement
- transformer objects
- touching Steam / Combustion / Wind baseline values

Those may still be good future ideas, but they are not required to solve the problem currently surfaced by the lab.

## Fuel Loop Guardrail

The generator theme is radioactive, but the first prototype should avoid a fuel loop that feels like a daily rare-resource tax.

That means:

- do **not** make late-game power depend on hand-feeding tiny amounts of rare material too often
- do **not** let it bypass progression so hard that Combustion becomes pointless

For the first prototype, the important thing is to preserve this feel target:

- fewer machines
- fewer zones
- premium setup
- manageable maintenance

Exact late fuel itemization can stay open for one more increment if needed.

## Success Criteria

The first `0.2.0` prototype is successful if it moves the benchmarks roughly toward this shape:

| Stage | Current | Target feel |
| --- | --- | --- |
| Summer Y2 Mid Shed | 3-4 Combustion, 1 zone | unchanged |
| Winter Y2 First Big Shed | 6 Combustion, 2 zones | unchanged |
| Mature Y3 Organized Conversion | 14-16 Combustion, 4 zones | 4-5 late generators, 2 zones |
| Mature Y3 Cellar Expansion | 23 Combustion, 6 zones | 6-7 late generators, 2-3 zones |

If the prototype improves mature density while leaving early and mid progression intact, it is doing the right job.

## Non-Goals

`0.2.0` should **not** try to solve all future tech fantasy at once.

Not part of this first density brief:

- universal battery-pack utility
- advanced storage logic
- battery-powered generators
- power-cell sockets
- deep automation redesign
- complete recipe rebalance of existing machines

The branch should stay focused on the real late-game pain that the lab already proved.

## Next Implementation Order

1. Add the new cable tier end-to-end.
2. Add the late generator end-to-end with placeholder fuel behavior if needed.
3. Hook the new unlock bundle into the current recipe grant system.
4. Simulate the prototype in Balance Lab before any wider expansion.

That is the smallest serious `0.2.0` step that answers the actual problem instead of wandering off into shiny side quests.
