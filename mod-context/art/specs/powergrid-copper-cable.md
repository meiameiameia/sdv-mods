# PowerGrid Copper Cable

## Identity

- Mod: `PowerGrid`
- Output path: `[SMAPI] PowerGrid/Assets/CopperCable.png`
- Output filename: `CopperCable.png`
- Asset family: `PowerGrid cable tier`
- Current shipped status: placeholder-style functional cable sheet exists

## Output Contract

- Canvas size: `64x128`
- Layout: `4x4` grid of `16x32` frames
- State variants: `16` connection masks, using `Up=1`, `Right=2`, `Down=4`, `Left=8`, with `column = mask % 4`, `row = floor(mask / 4)`
- Animation: none

## In-Game Role

- What the player should immediately read this as: low-tier floor-laid electrical cable
- What must stay recognizable from the current gameplay identity: copper material, simple junction-centered cable piece, exact connection logic

## Family Consistency Rules

- Shared silhouette rules: same connection geometry as `IronCable` and `IridiumCable`
- Shared connection or placement rules: arms must reach the same edges in every tier
- Variant differentiation rules: copper should feel utilitarian and entry-tier through warm copper-orange material cues, not by changing footprint

## Reference Hierarchy

1. Vanilla style authority: `Floors.png`, `Flooring.png`, `Flooring_winter.png` for modular readability; `Craftables.png` only for pixel treatment on hardware details
2. Compatibility references only: current repo `CopperCable.png`, plus sibling repo cable sheets for family footprint
3. Inspiration only: real copper wiring, conduit clips, simple insulated floor cable details
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Pixel readability requirements: center junction readable at gameplay scale; arms readable without clutter
- Edge / connection requirements: every mask must tile cleanly with adjacent cable pieces
- Forbidden output mistakes: detached shadows, floating glow, decorative bulk that blocks edge alignment, different geometry from other cable tiers

## Assumptions

- The code expects identical frame mapping for all cable tiers.
- The current placeholder geometry is functionally correct even though the rendering is crude.

## Done Criteria

- All `16` masks are present in the correct grid positions.
- Copper tier reads as the weakest cable family member without looking like scrap.
- The sheet can replace the current file without needing code or layout changes.
