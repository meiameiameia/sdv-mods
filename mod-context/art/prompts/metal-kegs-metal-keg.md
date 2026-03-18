# Metal Keg Prompt

Attach these references if available before generation:

- Vanilla big-craftable reference from `Craftables.png`
- Repo vanilla lineage reference: `[SMAPI] Metal Kegs/assets/templates/VanillaKeg.png`
- Current repo sprites: `[SMAPI] Metal Kegs/assets/MetalKeg.png`, `[SMAPI] Metal Kegs/assets/MetalKeg__unpowered.png`, `[SMAPI] Metal Kegs/assets/MetalKeg__powered.png`
- Optional industrial brewing tank references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: Metal Keg
Output filename: MetalKeg.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: `unpowered`, `powered`
If multiple states are requested, deliver separate review files named `MetalKeg__unpowered.png` and `MetalKeg__powered.png` while keeping the same footprint and cable-adjacency zones.
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references and the vanilla keg template are the style authority. Match their perspective, grounded mass, rounded body logic, and brewing-machine readability first.
2. The current repo MetalKeg sprite set is a compatibility reference only. Keep gameplay identity and keg lineage. Do not copy placeholder weaknesses if they conflict with vanilla readability.
3. External industrial brewing references are inspiration only. Use them for idea support, not style control.
4. master_64.txt is palette discipline only.

Identity first:
- This must read as an industrial keg-family brewing machine.
- It must still read as a keg first.
- It must not drift into a plain barrel-only object or a generic machine box.

Hard requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- preserve vanilla keg-style slight top-down / 3/4-ish perspective so the top ellipse or lid plane is clearly readable
- keep the main barrel/body mass near a soft `12-14 px` width on most rows
- make the lower body read as a sturdy grounded metal brewing vessel, not a thin barrel shell
- keep enclosed keg volume visibly rounded; do not flatten it into a front-on box
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides
- lowest visible body/base pixels should touch or nearly touch the bottom row
- stay strictly inside the exact canvas

Family consistency rules:
- preserve keg-family identity first
- differentiate through metal material, trim, and reinforcement rather than a new machine class silhouette
- keep it less premium than HardIridiumKeg
- keep unpowered and powered variants silhouette-compatible, with state change coming from restrained powered trim or indicator cues rather than shape drift

Do not:
- output multiple options
- make it read like a plain barrel-only prop, generic machine cabinet, furnace, or chest
- make it too boxy, too front-on, or too thin
- over-detail with rivets or modern realism
- drift away from vanilla big-craftable readability
- turn it into a sci-fi brewing machine
```
