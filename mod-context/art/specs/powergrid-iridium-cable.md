# PowerGrid Iridium Cable

## Identity

- Mod: `PowerGrid`
- Output path: `[SMAPI] PowerGrid/Assets/IridiumCable.png`
- Output filename: `IridiumCable.png`
- Asset family: `PowerGrid cable tier`
- Current shipped status: placeholder-style functional cable sheet exists

## Output Contract

- Canvas size: `64x128`
- Layout: `4x4` grid of `16x32` frames
- State variants: `16` connection masks, using `Up=1`, `Right=2`, `Down=4`, `Left=8`, with `column = mask % 4`, `row = floor(mask / 4)`
- Animation: none

## In-Game Role

- What the player should immediately read this as: premium late-game floor cable
- What must stay recognizable from the current gameplay identity: same PowerGrid cable family, purple/iridium tier signal, exact modular logic

## Family Consistency Rules

- Shared silhouette rules: identical to copper and iron cable geometry
- Shared connection or placement rules: no change to frame footprint or connection arms
- Variant differentiation rules: iridium should feel premium through material, subtle accenting, and cleaner finish, not through fantasy glow spam

## Reference Hierarchy

1. Vanilla style authority: `Floors.png`, `Flooring.png`, `Flooring_winter.png` for modular structure; `Craftables.png` for pixel treatment only
2. Compatibility references only: current repo `IridiumCable.png` and sibling cable sheets
3. Inspiration only: polished conductive hardware, insulated premium cable, iridium-themed machine trim
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Pixel readability requirements: purple tier signal must stay restrained and readable
- Edge / connection requirements: every frame must still tile perfectly with the other cable tiers
- Forbidden output mistakes: magical energy beams, bright bloom, changed center placement, excessive high-tech noise

## Assumptions

- Iridium is the top cable tier but still belongs in vanilla Stardew’s grounded visual world.
- The family should upscale mainly through finish and material confidence.

## Done Criteria

- The sheet is frame-compatible with the existing bitmask mapping.
- Iridium reads as the premium cable without abandoning vanilla readability.
- The cable family remains coherent when all three tiers are viewed together.
