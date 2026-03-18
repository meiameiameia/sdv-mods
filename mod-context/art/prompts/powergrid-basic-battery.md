# PowerGrid Basic Battery Prompt

Attach these references if available before generation:

- Vanilla machine-like big craftable crops from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/BasicBattery.png`
- Current sibling sprite: `[SMAPI] PowerGrid/Assets/IridiumBattery.png`
- Optional compact industrial battery references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: PowerGrid Basic Battery
Output filename: BasicBattery.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: `low`, `charged`
If multiple states are requested, deliver separate review files named `BasicBattery__low.png` and `BasicBattery__charged.png` while keeping the same footprint and attachment zones.
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references are the style authority. Match their perspective, grounding, housing logic, and readable machine mass first.
2. The current repo BasicBattery and IridiumBattery sprites are compatibility references only. Keep gameplay identity and family relation. Do not copy placeholder weaknesses if they conflict with vanilla readability.
3. External industrial battery references are inspiration only. Use them for idea support, not style control.
4. master_64.txt is palette discipline only.

Identity first:
- This must read as a compact battery cabinet / storage unit.
- It must read as enclosed stored-energy hardware, not as a pillar, shrine, torch, lantern, or decorative post.
- It must feel like the lower-tier sibling of IridiumBattery.

Hard requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- use Stardew's slight top-down / 3/4-ish machine perspective; the top plane must be clearly readable
- keep the main battery body near a soft `11-14 px` width on most rows
- make the lower body read as a sturdy grounded cabinet, not a thin front strip or pole
- the front face must read as enclosed battery housing
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides
- lowest visible base/body pixels should touch or nearly touch the bottom row
- stay strictly inside the exact canvas

Family consistency rules:
- should read as a sibling to the IridiumBattery
- lower-tier identity must come from simpler material and trim, not a weaker silhouette
- keep low and charged states silhouette-compatible, with state change coming from restrained charge-window or indicator cues

Do not:
- output multiple options
- make it read like a pillar, shrine, torch, lantern, or decorative post
- make it too thin, too tall, or too front-on
- add glowing charge bars or modern decals
- overload it with tiny terminals and wires
- drift away from vanilla big-craftable treatment
```
