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
- State deliverable convention: review-ready separate files `SteamGenerator__off.png` and `SteamGenerator__on.png`; runtime loads those optional state files when present and otherwise falls back to `SteamGenerator.png`
- Animation: none

## In-Game Role

- What the player should immediately read this as: a compact industrial boiler/furnace/generator machine
- What must stay recognizable from the current gameplay identity: machine-like heat/boiler feel, compact power unit, real enclosed machine housing, not a narrow post, cabinet, shrine, or furnace recolor

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
- Perspective / view rule: use Stardew's slight top-down / 3/4-ish machine perspective with a clearly readable top plane; do not render it flat front-on, and do not reduce the top to a token sliver
- Width / silhouette discipline: soft target `11-14 px` for the dominant boiler/body mass on most rows, with small accents allowed to flare slightly wider; avoid both spindly `8-10 px` body reads and a full-width `16 px` block
- Body-mass / sturdiness rule: the lower two-thirds should read as a broad compact machine housing with enough body mass to feel planted beside vanilla big craftables
- Grounding / floor-contact rule: the lowest visible base/body pixels should touch or nearly touch the bottom row of the `16x32` canvas; avoid visible empty space that makes the generator feel levitating
- Pixel readability requirements: one strong boiler/furnace/generator identity cue, readable top plane, and a front machine face that clearly reads as enclosed machinery rather than a post or pole
- Edge / connection requirements: stay fully inside `16x32` and preserve plausible left/right/up/down cable adjacency zones around the machine body
- Forbidden output mistakes: oversized smokestacks, modern realism, noisy pipe forests, fantasy electricity effects, narrow totem silhouettes, shrine-like shapes, ladder-like stacks, cabinet towers where the chimney becomes the whole read

## Assumptions

- Runtime state switching now supports optional `off` and `on` sprites with base-sprite fallback when either state file is missing.
- Fuel logic is communicated by gameplay behavior, so the sprite only needs to imply a fueled machine.

## Done Criteria

- Reads clearly as a compact fuel-based boiler/generator at vanilla scale.
- Sits cleanly beside vanilla big craftables.
- Top plane is clearly readable at `1x`, and the machine body feels grounded and broad enough instead of thin or front-on.
- Off and on variants keep the same attachment-compatible footprint and perspective.
