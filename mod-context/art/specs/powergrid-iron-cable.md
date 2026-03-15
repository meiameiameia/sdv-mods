# PowerGrid Iron Cable

## Identity

- Mod: `PowerGrid`
- Output path: `[SMAPI] PowerGrid/Assets/IronCable.png`
- Output filename: `IronCable.png`
- Asset family: `PowerGrid cable tier`
- Current shipped status: placeholder-style functional cable sheet exists

## Output Contract

- Canvas size: `64x128`
- Layout: `4x4` grid of `16x32` frames
- State variants: `16` connection masks, using `Up=1`, `Right=2`, `Down=4`, `Left=8`, with `column = mask % 4`, `row = floor(mask / 4)`
- Animation: none

## In-Game Role

- What the player should immediately read this as: mid-tier reinforced floor cable
- What must stay recognizable from the current gameplay identity: same cable family as copper and iridium, but visibly stronger and more metallic

## Family Consistency Rules

- Shared silhouette rules: exactly match `CopperCable` and `IridiumCable` frame geometry
- Shared connection or placement rules: identical edge reach, center anchor placement, and tile coverage
- Variant differentiation rules: iron should look sturdier and cooler-toned than copper, but less premium than iridium

## Reference Hierarchy

1. Vanilla style authority: `Floors.png`, `Flooring.png`, `Flooring_winter.png` for connection logic; `Craftables.png` only for shading discipline
2. Compatibility references only: current repo `IronCable.png` and sibling cable sheets
3. Inspiration only: iron conduit, braided shielding, reinforced cable jackets
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Pixel readability requirements: metallic tier change must read mostly through material and trim, not extra noise
- Edge / connection requirements: clean continuity across every adjacent cable state
- Forbidden output mistakes: changing the bitmask geometry, adding glowing sci-fi effects, over-detailing the center hub

## Assumptions

- Iron is the middle cable tier and should bridge copper and iridium visually.
- Family consistency matters more than decorative uniqueness.

## Done Criteria

- The sheet drops into the same code-driven frame mapping unchanged.
- Iron reads as the same family as copper with a sturdier upgrade step.
- All frames remain clean and connection-safe.
