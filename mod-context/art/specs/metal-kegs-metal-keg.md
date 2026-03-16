# Metal Keg

## Identity

- Mod: `Metal Kegs`
- Output path: `[SMAPI] Metal Kegs/assets/MetalKeg.png`
- Output filename: `MetalKeg.png`
- Asset family: `keg-family machine`
- Current shipped status: custom sprite exists; vanilla template extract also exists

## Output Contract

- Canvas size: `16x32`
- Layout: single big-craftable sprite
- State variants: none
- State deliverable convention: single-state only for now; no powered/unpowered companion deliverable is required yet
- Animation: none

## In-Game Role

- What the player should immediately read this as: a sturdier metal-bodied sibling of the vanilla keg
- What must stay recognizable from the current gameplay identity: keg lineage, brewing machine role, immediate distinction from the vanilla keg without confusion

## Family Consistency Rules

- Shared silhouette rules: stays in the keg family and should still feel craftable beside vanilla keg-class machines
- Shared connection or placement rules: vanilla big-craftable footprint and grounding
- Variant differentiation rules: `MetalKeg` should feel more industrial and metallic than the vanilla keg while staying less premium than `HardIridiumKeg`

## Reference Hierarchy

1. Vanilla style authority: `Craftables.png` and the repo's `assets/templates/VanillaKeg.png`
2. Compatibility references only: current repo `assets/MetalKeg.png`
3. Inspiration only: steel brewing tanks, banded barrels, metal vat hardware
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Perspective / view rule: preserve vanilla keg-style slight top-down / 3/4-ish perspective with a readable top ellipse; do not render it as a flat front-on barrel
- Width / silhouette discipline: soft target `11-14 px` for the main barrel/body mass on most rows, with hoops or handles allowed to flare slightly; avoid squaring it into a full-width `16 px` slab
- Pixel readability requirements: still reads as keg-family machinery, not a furnace or chest
- Edge / connection requirements: stay within `16x32` and preserve plausible left/right/up/down cable adjacency around the keg body
- Forbidden output mistakes: modern polished brewery realism, noisy rivet spam, losing the keg family silhouette entirely

## Assumptions

- The sprite is static in code.
- A powered/unpowered visual split is not justified enough yet to require multi-state art deliverables.
- Gameplay equivalence to the vanilla keg makes immediate keg recognition more important than novelty.

## Done Criteria

- Reads as a metal upgrade on the keg idea, not a different machine class.
- Fits next to the vanilla keg without style clash.
- Can replace the shipped file directly.
