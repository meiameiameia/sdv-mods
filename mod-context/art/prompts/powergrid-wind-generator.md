# PowerGrid Wind Generator Prompt

Attach these references if available before generation:

- Vanilla machine-like big craftable crops from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/WindGenerator.png`
- Optional small wind turbine or vane references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: PowerGrid Wind Generator
Output filename: WindGenerator.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: none
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references are the style authority.
2. The current repo WindGenerator sprite is a compatibility reference only. Preserve gameplay identity, but do not preserve placeholder-style rendering if it conflicts with vanilla readability.
3. External wind-generator references are inspiration only.
4. master_64.txt is palette discipline only.

Subject brief:
Create a passive wind-driven generator with a light but still grounded vertical silhouette. It should read as a farm-appropriate power device with one clear rotor or vane cue and a mechanical base. It should not look like a decorative weather vane.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- grounded lower rows with a lighter upper structure
- stay strictly inside the exact canvas

Family consistency rules:
- belongs to the same PowerGrid hardware family as the steam generator and batteries
- lighter, cleaner, and more passive than SteamGenerator
- keep detail readable instead of thin and fragile

Do not:
- output multiple options
- add spinning blur, wind trails, or animation effects
- use giant real-world wind turbine proportions
- make the silhouette so delicate that it disappears in game
- drift into modern sci-fi styling
```
