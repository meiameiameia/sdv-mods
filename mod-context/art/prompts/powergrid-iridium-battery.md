# PowerGrid Iridium Battery Prompt

Attach these references if available before generation:

- Vanilla machine-like big craftable crops from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/IridiumBattery.png`
- Current sibling sprite: `[SMAPI] PowerGrid/Assets/BasicBattery.png`
- Optional premium industrial battery references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: PowerGrid Iridium Battery
Output filename: IridiumBattery.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: `low`, `charged`
If multiple states are requested, deliver separate review files named `IridiumBattery__low.png` and `IridiumBattery__charged.png` while keeping the same footprint and attachment zones.
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references are the style authority.
2. The current repo IridiumBattery and BasicBattery sprites are compatibility references only. Preserve gameplay identity and family relation, but do not preserve placeholder-style rendering if it conflicts with vanilla readability.
3. External premium battery references are inspiration only.
4. master_64.txt is palette discipline only.

Subject brief:
Create a premium high-capacity battery machine that reads as the upgraded sibling of the basic battery. It should feel better-built, cleaner, and more valuable, with restrained iridium-colored cues and no sci-fi glow overload.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- use Stardew's slight top-down / 3/4-ish machine perspective; the top face should be visibly readable instead of a flat front panel
- keep the main battery body near a soft `10-13 px` width on most rows, with modest trim allowed to flare slightly wider
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides; do not use overhangs that visually block a neighboring cable
- grounded lower edge
- stay strictly inside the exact canvas

Family consistency rules:
- clearly belongs to the same family as BasicBattery
- premium step should come from material and trim, not a completely different silhouette
- remain fully compatible with vanilla big-craftable readability
- keep the body vanilla-machine sized instead of broadening into a full-width block
- keep low and charged states silhouette-compatible, with state change coming from restrained charge-window or indicator cues

Do not:
- output multiple options
- add glowing energy cores, crystals, or magic-tech effects
- use neon purple as the dominant read
- make it overly futuristic
- over-detail the sprite beyond vanilla density
```
