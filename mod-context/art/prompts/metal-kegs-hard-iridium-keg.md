# Hard Iridium Keg Prompt

Attach these references if available before generation:

- Vanilla big-craftable reference from `Craftables.png`
- Repo vanilla lineage reference: `[SMAPI] Metal Kegs/assets/templates/VanillaKeg.png`
- Repo compatibility reference: `[SMAPI] Metal Kegs/assets/templates/HardwoodKeg.png`
- Current repo sprites: `[SMAPI] Metal Kegs/assets/HardIridiumKeg.png`, `[SMAPI] Metal Kegs/assets/HardIridiumKeg__unpowered.png`, `[SMAPI] Metal Kegs/assets/HardIridiumKeg__powered.png`, and `[SMAPI] Metal Kegs/assets/MetalKeg.png`
- Optional reinforced cask references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: Hard Iridium Keg
Output filename: HardIridiumKeg.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: `unpowered`, `powered`
If multiple states are requested, deliver separate review files named `HardIridiumKeg__unpowered.png` and `HardIridiumKeg__powered.png` while keeping the same footprint and cable-adjacency zones.
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references and the vanilla keg template are the style authority.
2. The repo HardwoodKeg template and current HardIridiumKeg and MetalKeg sprite sets are compatibility references only. Preserve gameplay identity and lineage cues, but do not preserve off-style rendering if it conflicts with vanilla readability.
3. External reinforced cask or premium brewing-vessel references are inspiration only.
4. master_64.txt is palette discipline only.

Subject brief:
Create a premium reinforced keg-family machine that reads as the higher-tier sibling of MetalKeg. It should keep keg lineage readability while signaling iridium-grade quality, tougher construction, and a more valuable finish. Keep the result grounded in vanilla Stardew instead of making it magical or sci-fi.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- preserve vanilla keg-style slight top-down / 3/4-ish perspective so the top ellipse or lid plane is clearly readable; do not render it flat front-on
- keep the main barrel/body mass near a soft `12-14 px` width on most rows, with reinforced trim allowed to flare slightly wider
- give the lower body enough mass that the keg reads as a sturdy reinforced vessel instead of a thin barrel shell
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides; do not let reinforced trim or side hardware imply that a neighboring cable cannot connect
- lowest visible body/base pixels should touch or nearly touch the bottom row so the keg does not feel like it is floating
- stay strictly inside the exact canvas

Family consistency rules:
- must still read as a keg-family machine first
- should look clearly more premium than MetalKeg
- use iridium cues with restraint; quality comes from finish and reinforcement, not VFX
- keep the body rounded and keg-sized instead of broadening into a square full-width block or thinning into a weak barrel strip
- keep unpowered and powered variants silhouette-compatible, with state change coming from restrained powered trim or indicator cues rather than shape drift

Do not:
- output multiple options
- add purple glow, energy effects, or magic crystals
- let the silhouette drift so far that it no longer reads as keg-related
- over-detail it beyond vanilla craftable density
- make it look like a separate machine class
```
