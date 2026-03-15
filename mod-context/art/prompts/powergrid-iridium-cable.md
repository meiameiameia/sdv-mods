# PowerGrid Iridium Cable Prompt

Attach these references if available before generation:

- Vanilla modular reference crops from `Floors.png`, `Flooring.png`, and `Flooring_winter.png`
- Vanilla pixel-treatment crop from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/IridiumCable.png`
- Sibling repo cable sprites for family geometry
- Optional premium cable/conduit references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite sheet for Stardew Valley.

Asset name: PowerGrid Iridium Cable
Output filename: IridiumCable.png
Canvas: 64x128 pixels
Layout: 4 columns x 4 rows of 16x32 frames
State variants: 16 connection masks. Use Up=1, Right=2, Down=4, Left=8. Place each mask at column = mask % 4 and row = floor(mask / 4).
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley modular floor and flooring references are the style authority for connection logic and readability.
2. Vanilla Craftables references are authority only for pixel treatment and shading discipline.
3. The current repo IridiumCable sprite and sibling cable sheets are compatibility references only. Preserve family identity and exact geometry, but do not preserve placeholder-style rendering if it conflicts with vanilla readability.
4. External industrial references are inspiration only.
5. master_64.txt is palette discipline only.

Subject brief:
Create a premium late-game iridium cable family. It should feel cleaner, rarer, and higher quality than the copper and iron tiers while still reading as the same modular floor cable family. Use restrained iridium-purple cues, not magical glow.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- every frame must tile cleanly with adjacent frames
- same connection footprint as the other cable tiers
- stay strictly inside the exact canvas

Family consistency rules:
- identical geometry to CopperCable and IronCable
- upgrade expression should come from finish, trim, and premium material cues
- keep it grounded in vanilla Stardew, not sci-fi fantasy

Do not:
- output multiple options
- add bloom, particles, lightning, or magical glow
- change the frame mapping
- use neon purple as the main read
- add detail so dense that the cable stops reading cleanly at 1x
```
