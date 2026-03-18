# PowerGrid Power Conduit Prompt

Attach these references if available before generation:

- Vanilla machine-like big craftable crops from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/PowerConduit.png`
- Optional relay, coupler, or signal-device references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: PowerGrid Power Conduit
Output filename: PowerConduit.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: `unpaired`, `linked`
If multiple states are requested, deliver separate review files named `PowerConduit__unpaired.png` and `PowerConduit__linked.png` while keeping the same footprint and attachment zones.
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references are the style authority. Match their perspective, grounding, compact machine mass, and readable device logic first.
2. The current repo PowerConduit sprite is a compatibility reference only. Keep gameplay identity. Do not copy placeholder weaknesses if they conflict with vanilla readability.
3. External relay or coupler references are inspiration only. Use them for idea support, not style control.
4. master_64.txt is palette discipline only.

Identity first:
- This must read as a compact relay / link device.
- It must read as a special conduit device, not as a battery box, generator, sign, pedestal, or antenna tower.
- It must preserve believable cable adjacency on the left, right, top, and bottom sides.

Hard requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- use Stardew's slight top-down / 3/4-ish machine perspective; a top cap or top plane must be clearly visible
- keep the main conduit body near a soft `11-14 px` width on most rows
- make the lower body read as a compact grounded device, not a thin post or pedestal
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides; no one-sided bulk that makes a cable neighbor look impossible
- lowest visible base/body pixels should touch or nearly touch the bottom row
- stay strictly inside the exact canvas

Family consistency rules:
- belongs to the same PowerGrid hardware family as generators and batteries
- should feel more specialized and link-oriented than storage or generation devices
- keep unpaired and linked states silhouette-compatible, with state change coming from restrained link cues rather than shape drift

Do not:
- output multiple options
- make it read like a battery box, generator, sign, pedestal, or antenna tower
- add portal effects, floating runes, lightning arcs, or animation cues
- make it too tall, too thin, or too front-on
- let one side look non-connectable
- drift away from vanilla big-craftable style
```
