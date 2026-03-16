# PowerGrid Steam Generator

## Identity

- Mod: `PowerGrid`
- Output path: `[SMAPI] PowerGrid/Assets/SteamGenerator.png`
- Output filename: `SteamGenerator.png`
- Asset family: `PowerGrid generator`
- Current shipped status: placeholder-style generator sprite exists

## Output Contract

- Canvas size: `16x32`
- Layout: single big-craftable sprite
- State variants: `off`, `on`
- State deliverable convention: review-ready separate files `SteamGenerator__off.png` and `SteamGenerator__on.png`; current shipped path stays `SteamGenerator.png` until runtime switching exists
- Animation: none

## In-Game Role

- What the player should immediately read this as: fuel-burning power generator
- What must stay recognizable from the current gameplay identity: machine-like heat/boiler feel, compact power unit, not a generic chest or furnace clone

## Family Consistency Rules

- Shared silhouette rules: should feel related to `WindGenerator`, `BasicBattery`, `IridiumBattery`, and `PowerConduit` as a PowerGrid hardware set
- Shared connection or placement rules: grounded vanilla big-craftable footprint
- Variant differentiation rules: steam generator should read as heavier, fueled, and hotter than the passive wind generator

## Reference Hierarchy

1. Vanilla style authority: `Craftables.png` with Furnace-like and machine-like big craftables
2. Compatibility references only: current repo `SteamGenerator.png`, current PowerGrid sibling machine sprites
3. Inspiration only: compact boilers, pistons, small industrial pressure vessels
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Perspective / view rule: use Stardew's slight top-down / 3/4-ish machine perspective with a visibly readable top face; do not render it flat front-on
- Width / silhouette discipline: soft target `10-13 px` for the dominant boiler/body mass on most rows, with small accents allowed to flare slightly wider; avoid a full-width `16 px` block
- Pixel readability requirements: one strong boiler/vent identity cue, readable machine mass, grounded base
- Edge / connection requirements: stay fully inside `16x32` and preserve plausible left/right/up/down cable adjacency zones around the machine body
- Forbidden output mistakes: oversized smokestacks, modern realism, noisy pipe forests, fantasy electricity effects

## Assumptions

- The sprite has no alternate powered state in code today, but the art contract should prepare `off` and `on` variants now.
- Fuel logic is communicated by gameplay behavior, so the sprite only needs to imply a fueled machine.

## Done Criteria

- Reads clearly as a fuel-based generator at vanilla scale.
- Sits cleanly beside vanilla big craftables.
- Off and on variants keep the same attachment-compatible footprint and perspective.
