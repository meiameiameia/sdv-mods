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
- State variants: `unpowered`, `powered`
- State deliverable convention: review-ready separate files `HardIridiumKeg__unpowered.png` and `HardIridiumKeg__powered.png`; runtime loads those optional state files when present and otherwise falls back to `HardIridiumKeg.png`
- Animation: none

## In-Game Role

- What the player should immediately read this as: a premium reinforced keg-family brewing machine
- What must stay recognizable from the current gameplay identity: keg lineage first, stronger premium construction second, not a plain barrel-only prop, generic machine cabinet, or magical-tech object

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
- Perspective / view rule: preserve vanilla keg-style slight top-down / 3/4-ish perspective with a clearly readable top ellipse or lid plane; do not render it as a flat front-on cask
- Width / silhouette discipline: soft target `12-14 px` for the main barrel/body mass on most rows, with reinforced trim allowed to flare slightly wider; avoid both thin/spindly barrel reads and a square full-width `16 px` slab
- Body-mass / sturdiness rule: the keg must read as a dense reinforced premium vessel with enough lower-body mass to feel grounded and durable at `1x`
- Grounding / floor-contact rule: the lowest visible body/base pixels should touch or nearly touch the bottom row; avoid empty air beneath the keg
- Pixel readability requirements: premium variant must still read as a keg-family machine first, with enclosed keg volume and rounded mass still obvious
- Edge / connection requirements: stay inside `16x32` and preserve plausible left/right/up/down cable adjacency around the keg body
- Forbidden output mistakes: plain barrel-only silhouette, generic machine cabinet, sci-fi glowing keg, purple neon overload, silhouette drift so large that it no longer reads as keg-related

## Assumptions

- The GOF hardwood-keg lineage is a compatibility cue, not a style authority.
- Runtime state switching now supports optional `unpowered` and `powered` sprites with base-sprite fallback when either state file is missing.

## Done Criteria

- Reads as the premium sibling of `MetalKeg`.
- Keeps keg-family identity while clearly stepping up in quality.
- Unpowered and powered variants keep the same keg-family footprint and perspective while differing through restrained powered cues rather than shape drift.
