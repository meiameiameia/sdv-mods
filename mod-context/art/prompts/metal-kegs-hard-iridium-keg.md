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
1. Vanilla Stardew Valley Craftables references and the vanilla keg template are the style authority. Match their perspective, grounded mass, rounded body logic, and brewing-machine readability first.
2. The repo HardwoodKeg template and current HardIridiumKeg and MetalKeg sprite sets are compatibility references only. Keep gameplay identity and family relation. Do not copy placeholder weaknesses if they conflict with vanilla readability.
3. External reinforced cask references are inspiration only. Use them for idea support, not style control.
4. master_64.txt is palette discipline only.

Identity first:
- This must read as a premium reinforced keg-family brewing machine.
- It must still read as a keg first.
- It must not drift into a plain barrel-only object, a generic machine box, or a magical-tech prop.

Hard requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- preserve vanilla keg-style slight top-down / 3/4-ish perspective so the top ellipse or lid plane is clearly readable
- keep the main barrel/body mass near a soft `12-14 px` width on most rows
- make the lower body read as a sturdy reinforced vessel, not a thin barrel shell
- keep enclosed keg volume visibly rounded; do not flatten it into a front-on box
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides
- lowest visible body/base pixels should touch or nearly touch the bottom row
- stay strictly inside the exact canvas

Family consistency rules:
- must still read as a keg-family machine first
- should look clearly more premium than MetalKeg
- use iridium cues with restraint; quality must come from finish and reinforcement, not VFX
- keep unpowered and powered variants silhouette-compatible, with state change coming from restrained powered trim or indicator cues rather than shape drift

Do not:
- output multiple options
- make it read like a plain barrel-only prop, generic machine cabinet, furnace, or magical relic
- make it too boxy, too front-on, or too thin
- add purple glow, magic effects, or energy-crystal styling
- let the silhouette drift so far that it no longer reads as keg-related
- over-detail it beyond vanilla craftable density
```
