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
1. Vanilla Stardew Valley Craftables references are the style authority.
2. The current repo BasicBattery and IridiumBattery sprites are compatibility references only. Preserve gameplay identity and family relation, but do not preserve placeholder-style rendering if it conflicts with vanilla readability.
3. External industrial battery references are inspiration only.
4. master_64.txt is palette discipline only.

Subject brief:
Create a compact entry-tier power storage machine. It should read clearly as a battery or energy-storage unit, with a simple charge-storage silhouette and modest hardware details. It should feel cheaper and more utilitarian than the iridium battery.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- use Stardew's slight top-down / 3/4-ish machine perspective; the top plane should be clearly readable instead of a flat front panel
- keep the main battery body near a soft `11-14 px` width on most rows, with small terminals or trim allowed to flare slightly wider
- give the lower body enough mass that the battery reads as a sturdy grounded cabinet instead of a thin front strip
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides; do not use overhangs that visually block a neighboring cable
- lowest visible base/body pixels should touch or nearly touch the bottom row so the cabinet does not feel like it is floating
- stay strictly inside the exact canvas

Family consistency rules:
- should read as a sibling to the IridiumBattery
- lower-tier identity should come from simpler material and trim
- keep the silhouette clean and big-craftable readable
- keep the battery cabinet narrower than the full canvas so it stays vanilla-machine sized, but do not let it drift into a thin spindly locker
- keep low and charged states silhouette-compatible, with state change coming from restrained charge-window or indicator cues

Do not:
- output multiple options
- add glowing charge bars or modern decals
- make it too futuristic
- overload it with tiny terminals and wires
- drift away from vanilla big-craftable treatment
```
