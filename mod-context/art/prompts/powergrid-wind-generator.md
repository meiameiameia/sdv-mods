# PowerGrid Wind Generator Prompt

Attach these references if available before generation:

- Vanilla machine-like big craftable crops from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/WindGenerator.png`
- Optional small wind-driven generator references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: PowerGrid Wind Generator
Output filename: WindGenerator.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: `idle`, `generating`
If multiple states are requested, deliver separate review files named `WindGenerator__idle.png` and `WindGenerator__generating.png` while keeping the same footprint and attachment zones.
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references are the style authority. Match their silhouette logic, perspective, grounding, and machine readability first.
2. The current repo WindGenerator sprite is a compatibility reference only. Keep gameplay identity. Do not copy placeholder weaknesses if they conflict with vanilla readability.
3. External wind-generator references are inspiration only. Use them for idea support, not style control.
4. master_64.txt is palette discipline only.

Identity first:
- This must read as a wind-powered generator with a real enclosed machine base.
- It must read as generator first, wind cue second.
- It must not read as a decorative windmill, weather vane, toy turbine, pinwheel, or signpost.

Hard requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- use Stardew's slight top-down / 3/4-ish machine perspective on the base; the top plane must read clearly
- keep the base/main machine mass near a soft `11-14 px` width on most rows
- make the lower body read as a sturdy generator housing, not a narrow pole
- keep the rotor or vane compact and readable; it must support the machine identity, not replace it
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides of the base
- lowest visible base/body pixels should touch or nearly touch the bottom row
- stay strictly inside the exact canvas

Family consistency rules:
- belongs to the same PowerGrid hardware family as the steam generator and batteries
- lighter, cleaner, and more passive than SteamGenerator
- keep detail readable instead of thin and fragile
- keep idle and generating states silhouette-compatible, with state change coming from restrained running cues rather than shape drift

Do not:
- output multiple options
- make it read like a decorative windmill, weather vane, toy turbine, pinwheel, or signpost
- use giant real-world wind turbine proportions
- make the base too thin, skeletal, or front-on
- add spinning blur, wind trails, or animation effects
- drift into modern sci-fi styling
```
