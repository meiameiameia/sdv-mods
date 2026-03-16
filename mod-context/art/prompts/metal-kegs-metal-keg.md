# Metal Keg Prompt

Attach these references if available before generation:

- Vanilla big-craftable reference from `Craftables.png`
- Repo vanilla lineage reference: `[SMAPI] Metal Kegs/assets/templates/VanillaKeg.png`
- Current repo sprite: `[SMAPI] Metal Kegs/assets/MetalKeg.png`
- Optional industrial brewing tank references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: Metal Keg
Output filename: MetalKeg.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: none
Single-state only for now. Do not prepare powered/unpowered companion files yet.
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references and the vanilla keg template are the style authority.
2. The current repo MetalKeg sprite is a compatibility reference only. Preserve gameplay identity and keg lineage, but do not preserve off-style rendering if it conflicts with vanilla readability.
3. External industrial brewing references are inspiration only.
4. master_64.txt is palette discipline only.

Subject brief:
Create a sturdier metal-bodied sibling of the vanilla keg. It must still read immediately as a keg-family brewing machine, but with clearer metal reinforcement, industrial trim, and a more manufactured feel than wood. Keep the silhouette close enough to the keg lineage that players recognize it at a glance.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- preserve vanilla keg-style slight top-down / 3/4-ish perspective so the top ellipse is readable; do not render it flat front-on
- keep the main barrel/body mass near a soft `11-14 px` width on most rows, with hoops or handles allowed to flare slightly wider
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides; do not let hoops, taps, or overhangs imply that a neighboring cable cannot connect
- grounded lower edge
- stay strictly inside the exact canvas

Family consistency rules:
- preserve keg-family identity first
- differentiate through metal material, trim, and reinforcement rather than a new machine class silhouette
- keep it less premium than HardIridiumKeg
- keep the body rounded and vanilla-keg sized instead of squaring it into a full-width block
- keep this asset single-state for now; do not invent powered/unpowered cues in the deliverable

Do not:
- output multiple options
- make it look like a furnace, chest, or generic barrel
- over-detail with rivets and modern realism
- drift away from vanilla big-craftable readability
- turn it into a sci-fi brewing machine
```
