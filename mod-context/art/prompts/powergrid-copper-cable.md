# PowerGrid Copper Cable Prompt

Attach these references if available before generation:

- Vanilla modular reference crops from `Floors.png`, `Flooring.png`, and `Flooring_winter.png`
- Vanilla pixel-treatment crop from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/CopperCable.png`
- Optional industrial copper cable or conduit references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite sheet for Stardew Valley.

Asset name: PowerGrid Copper Cable
Output filename: CopperCable.png
Canvas: 64x128 pixels
Layout: 4 columns x 4 rows of 16x32 frames
State variants: 16 connection masks. Use Up=1, Right=2, Down=4, Left=8. Place each mask at column = mask % 4 and row = floor(mask / 4).
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley modular floor and flooring references are the style authority for connection logic and readability.
2. Vanilla Craftables references are authority only for pixel treatment and shading discipline.
3. The current repo CopperCable sprite is a compatibility reference only. Preserve gameplay identity and footprint, but do not preserve placeholder-style rendering if it conflicts with vanilla readability.
4. External industrial references are inspiration only.
5. master_64.txt is palette discipline only.

Subject brief:
Create a low-tier floor-laid copper electrical cable family. Each frame should show the same compact lower-half junction center with cable arms extending cleanly to the needed tile edges. The sheet must read as handcrafted farm-tech wiring, not sci-fi energy beams.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- every frame must tile cleanly with adjacent frames
- copper material should read through warm metallic color and modest hardware detail
- stay strictly inside the exact canvas

Family consistency rules:
- keep the same geometry that IronCable and IridiumCable will use
- differentiate this tier mostly through copper material cues, not footprint changes
- avoid decorative bulk that interferes with edge alignment

Do not:
- output multiple options
- add glow, lightning, sparks, or magical energy
- change the frame mapping
- add detached shadows outside the cable footprint
- make the cable so thin that it disappears in-game
```
