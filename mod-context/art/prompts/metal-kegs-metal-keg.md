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
1. Vanilla Stardew Valley Craftables references and the vanilla keg template are the style authority.
2. The current repo MetalKeg sprite set is a compatibility reference only. Preserve gameplay identity and keg lineage, but do not preserve off-style rendering if it conflicts with vanilla readability.
3. External industrial brewing references are inspiration only.
4. master_64.txt is palette discipline only.

Subject brief:
Create a sturdier metal-bodied sibling of the vanilla keg. It must still read immediately as a keg-family brewing machine, but with clearer metal reinforcement, industrial trim, and a more manufactured feel than wood. Keep the silhouette close enough to the keg lineage that players recognize it at a glance.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- preserve vanilla keg-style slight top-down / 3/4-ish perspective so the top ellipse or lid plane is clearly readable; do not render it flat front-on
- keep the main barrel/body mass near a soft `12-14 px` width on most rows, with hoops or handles allowed to flare slightly wider
- give the lower body enough mass that the keg reads as a sturdy grounded metal brewing vessel instead of a thin barrel shell
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides; do not let hoops, taps, or overhangs imply that a neighboring cable cannot connect
- lowest visible body/base pixels should touch or nearly touch the bottom row so the keg does not feel like it is floating
- stay strictly inside the exact canvas

Family consistency rules:
- preserve keg-family identity first
- differentiate through metal material, trim, and reinforcement rather than a new machine class silhouette
- keep it less premium than HardIridiumKeg
- keep the body rounded and vanilla-keg sized instead of squaring it into a full-width block or thinning it into a weak barrel strip
- keep unpowered and powered variants silhouette-compatible, with state change coming from restrained powered trim or indicator cues rather than shape drift

Do not:
- output multiple options
- make it look like a furnace, chest, or generic barrel
- over-detail with rivets and modern realism
- drift away from vanilla big-craftable readability
- turn it into a sci-fi brewing machine
```
