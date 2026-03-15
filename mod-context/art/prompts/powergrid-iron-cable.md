# PowerGrid Iron Cable Prompt

Attach these references if available before generation:

- Vanilla modular reference crops from `Floors.png`, `Flooring.png`, and `Flooring_winter.png`
- Vanilla pixel-treatment crop from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/IronCable.png`
- Sibling repo cable sprites for family geometry
- Optional reinforced conduit references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite sheet for Stardew Valley.

Asset name: PowerGrid Iron Cable
Output filename: IronCable.png
Canvas: 64x128 pixels
Layout: 4 columns x 4 rows of 16x32 frames
State variants: 16 connection masks. Use Up=1, Right=2, Down=4, Left=8. Place each mask at column = mask % 4 and row = floor(mask / 4).
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley modular floor and flooring references are the style authority for connection logic and readability.
2. Vanilla Craftables references are authority only for pixel treatment and shading discipline.
3. The current repo IronCable sprite and sibling cable sheets are compatibility references only. Preserve the footprint and family identity, but do not preserve placeholder-style rendering if it conflicts with vanilla readability.
4. External industrial references are inspiration only.
5. master_64.txt is palette discipline only.

Subject brief:
Create a mid-tier reinforced iron cable family. It must use the exact same connection geometry as the other cable tiers, but read as sturdier, cooler-toned, and more reinforced than copper. Keep the center junction compact and clean.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- every frame must tile cleanly with adjacent frames
- iron material should read through metallic gray values and restrained reinforcement detail
- stay strictly inside the exact canvas

Family consistency rules:
- identical geometry to CopperCable and IridiumCable
- material upgrade should come from iron finish and trim, not shape change
- keep visual noise lower than a real-world industrial illustration

Do not:
- output multiple options
- add glow or high-tech VFX
- change the frame mapping
- over-detail the center hub
- let the cable look futuristic instead of Stardew-readable
```
