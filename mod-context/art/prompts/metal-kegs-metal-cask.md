# Metal Cask Prompt

Attach these references if available before generation:

- Vanilla machine-like big craftable crops from `Craftables.png`
- A local vanilla cask crop from `Craftables.png` if available
- Current repo sibling sprites: `[SMAPI] Metal Kegs/assets/MetalKeg.png` and `[SMAPI] Metal Kegs/assets/HardIridiumKeg.png`
- Optional industrial aging-vessel references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: Metal Cask
Output filename: MetalCask.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: none
Single-state only for now. Do not prepare powered/unpowered companion files yet.
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references, especially the vanilla cask, are the style authority. Match their perspective, grounded mass, closed-vessel logic, and aging-cask identity first.
2. The current repo MetalKeg and HardIridiumKeg sprites are compatibility references only. Keep the repo's metal-family identity where useful. Do not let keg-specific shaping override cask lineage.
3. External industrial aging-vessel references are inspiration only. Use them for idea support, not style control.
4. master_64.txt is palette discipline only.

Identity first:
- This must read as a closed industrial aging cask.
- It must read as a sealed vessel for wine or cheese aging.
- It must read as cask first, industrial reinforcement second.
- It must not read as an open basin, bowl, vat, tank, cabinet, cauldron, chalice, or pedestal vessel.

Hard requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- preserve Stardew's slight top-down / 3/4-ish cask perspective so the top ellipse or lid plane is clearly readable
- keep the main cask/body mass near a soft `12-14 px` width on most rows
- make the lower body read as a sturdy grounded aging vessel, not a thin shell or tank on a stand
- show clearly enclosed storage body with a lid, hatch, cap, or sealed top treatment; no open liquid surface
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides without making the cask read like a relay or battery machine
- lowest visible body/base pixels should touch or nearly touch the bottom row
- stay strictly inside the exact canvas

Family consistency rules:
- preserve cask-family identity first
- use reinforced metal bands, braces, trim, or housing details to sell the industrial upgrade
- keep it more cask-like than keg-like
- avoid pedestal, hoop-on-stand, goblet, basin, vat, or tank silhouettes
- do not invent powered/unpowered variants in this run

Do not:
- output multiple options
- make it read like an open-top basin, bowl, vat, cauldron, chalice, or hoop on a stand
- show visible liquid, exposed contents, or an empty open vessel mouth
- make it read like a storage tank or machine cabinet
- make it too thin, too tall, or too front-on
- add sci-fi glow, tech panels, or magical effects
- let the sprite float above the bottom of the canvas
```
