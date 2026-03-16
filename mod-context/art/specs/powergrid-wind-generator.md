# PowerGrid Wind Generator

## Identity

- Mod: `PowerGrid`
- Output path: `[SMAPI] PowerGrid/Assets/WindGenerator.png`
- Output filename: `WindGenerator.png`
- Asset family: `PowerGrid generator`
- Current shipped status: placeholder-style generator sprite exists

## Output Contract

- Canvas size: `16x32`
- Layout: single big-craftable sprite
- State variants: `idle`, `generating`
- State deliverable convention: review-ready separate files `WindGenerator__idle.png` and `WindGenerator__generating.png`; current shipped path stays `WindGenerator.png` until runtime switching exists
- Animation: none

## In-Game Role

- What the player should immediately read this as: passive wind-driven generator
- What must stay recognizable from the current gameplay identity: airy vertical silhouette, generator hardware, distinct from the steam generator

## Family Consistency Rules

- Shared silhouette rules: belongs to the same PowerGrid hardware set as other generators, batteries, and conduit
- Shared connection or placement rules: grounded lower rows even if the upper silhouette is lighter
- Variant differentiation rules: should look cleaner, lighter, and more passive than the steam generator

## Reference Hierarchy

1. Vanilla style authority: `Craftables.png` for big-craftable treatment and readable vertical silhouettes
2. Compatibility references only: current repo `WindGenerator.png`, current PowerGrid sibling machine sprites
3. Inspiration only: small wind turbines, vane-driven generators, farm-scale rotor hardware
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Perspective / view rule: keep a slight top-down / 3/4-ish read on the generator base so the top face is visible; do not make it a flat front-on pole sign
- Width / silhouette discipline: keep the base/main machine mass near a soft `10-13 px` body width on most rows; upper vane or rotor structure may narrow above that, but should not disappear into thread-thin lines
- Pixel readability requirements: rotor or vane cue must read immediately without becoming thin noise
- Edge / connection requirements: remain inside `16x32`, preserve grounded placement, and keep plausible cable adjacency on left/right/up/down sides of the base
- Forbidden output mistakes: giant modern turbine proportions, spinning FX, sci-fi neon, overly delicate lines that vanish in game

## Assumptions

- The code currently uses one static sprite only, but the art contract should prepare `idle` and `generating` variants now.
- Weather/output variation is mechanical, not visual.

## Done Criteria

- Reads as a passive generator rather than a decorative weather vane.
- Distinguishes itself from `SteamGenerator` while staying in the same family.
- Idle and generating variants keep the same attachment-compatible footprint and perspective.
