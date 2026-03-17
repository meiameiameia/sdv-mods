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
1. Vanilla Stardew Valley Craftables references, especially the vanilla cask, are the style authority.
2. The current repo MetalKeg and HardIridiumKeg sprites are compatibility references only. Preserve the repo's metal-family identity where useful, but do not let keg-specific shaping override cask lineage.
3. External industrial aging-vessel references are inspiration only.
4. master_64.txt is palette discipline only.

Subject brief:
Create an industrial aging cask that still reads immediately as part of Stardew's cask lineage. It should feel like a reinforced metal-supported cask or aging vessel, not a generic machine cabinet and not just a recolored keg. Preserve cask readability first, then add reinforced machine-like construction cues second.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- preserve Stardew's slight top-down / 3/4-ish cask perspective so the top ellipse or lid plane is clearly readable; do not render it flat front-on
- keep the main cask/body mass near a soft `12-14 px` width on most rows
- give the lower body enough mass that the cask reads as sturdy and grounded instead of thin, spindly, or top-heavy
- lowest visible body/base pixels should touch or nearly touch the bottom row so the cask does not feel like it is floating
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides without making the cask read like a relay or battery machine
- stay strictly inside the exact canvas

Family consistency rules:
- preserve cask-family identity first
- use reinforced metal bands, braces, trim, or housing details to sell the industrial upgrade
- keep it more cask-like than keg-like
- keep it grounded in vanilla big-craftable readability rather than modern realistic brewery art
- do not invent powered/unpowered variants in this run

Do not:
- output multiple options
- make it look like a generic barrel, chest, or machine cabinet
- make it too thin or too tall for 16x32
- add sci-fi glow, tech panels, or magical effects
- let the sprite float above the bottom of the canvas
```
