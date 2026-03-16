# Hard Iridium Keg

## Identity

- Mod: `Metal Kegs`
- Output path: `[SMAPI] Metal Kegs/assets/HardIridiumKeg.png`
- Output filename: `HardIridiumKeg.png`
- Asset family: `keg-family machine`
- Current shipped status: custom sprite exists; template extracts for vanilla keg and hardwood keg lineage exist

## Output Contract

- Canvas size: `16x32`
- Layout: single big-craftable sprite
- State variants: none
- State deliverable convention: single-state only for now; no powered/unpowered companion deliverable is required yet
- Animation: none

## In-Game Role

- What the player should immediately read this as: premium reinforced keg-family machine with iridium-quality finish
- What must stay recognizable from the current gameplay identity: relationship to both keg lineage and the harder premium variant concept

## Family Consistency Rules

- Shared silhouette rules: should still read as part of the keg family and as a sibling to `MetalKeg`
- Shared connection or placement rules: vanilla big-craftable footprint and grounding
- Variant differentiation rules: should feel more premium and reinforced than `MetalKeg`, but not leave vanilla readability or become magical-tech

## Reference Hierarchy

1. Vanilla style authority: `Craftables.png` and the repo's `assets/templates/VanillaKeg.png`
2. Compatibility references only: repo `assets/templates/HardwoodKeg.png`, repo `assets/HardIridiumKeg.png`, repo `assets/MetalKeg.png`
3. Inspiration only: reinforced casks, iridium-trimmed metal hardware, premium pressure-rated brewing vessels
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Perspective / view rule: preserve vanilla keg-style slight top-down / 3/4-ish perspective with a readable top ellipse; do not render it as a flat front-on cask
- Width / silhouette discipline: soft target `11-14 px` for the main barrel/body mass on most rows, with reinforced trim allowed to flare slightly; avoid a square full-width `16 px` slab
- Pixel readability requirements: premium variant must still read as a keg-family machine first
- Edge / connection requirements: stay inside `16x32` and preserve plausible left/right/up/down cable adjacency around the keg body
- Forbidden output mistakes: sci-fi glowing keg, purple neon overload, silhouette drift so large that it no longer reads as keg-related

## Assumptions

- The GOF hardwood-keg lineage is a compatibility cue, not a style authority.
- The sprite remains static regardless of installed integrations.
- A powered/unpowered visual split is not justified enough yet to require multi-state art deliverables.

## Done Criteria

- Reads as the premium sibling of `MetalKeg`.
- Keeps keg-family identity while clearly stepping up in quality.
- Fits the repo’s existing filename and runtime usage unchanged.
