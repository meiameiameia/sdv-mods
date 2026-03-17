# PowerGrid Steam Generator Prompt

Attach these references if available before generation:

- Vanilla machine-like big craftable crops from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/SteamGenerator.png`
- Optional compact boiler / small steam engine references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: PowerGrid Steam Generator
Output filename: SteamGenerator.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: `off`, `on`
If multiple states are requested, deliver separate review files named `SteamGenerator__off.png` and `SteamGenerator__on.png` while keeping the same footprint and attachment zones.
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references are the style authority.
2. The current repo SteamGenerator sprite is a compatibility reference only. Preserve gameplay identity, but do not preserve placeholder-style rendering if it conflicts with vanilla readability.
3. External industrial boiler or steam-engine references are inspiration only.
4. master_64.txt is palette discipline only.

Subject brief:
Create a compact fuel-burning generator that reads as a handcrafted farm-tech machine. It should imply heat, pressure, and mechanical power generation, with one strong boiler or vent cue and a grounded base. It should not look like a straight Furnace recolor.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- use Stardew's slight top-down / 3/4-ish machine perspective; the top plane should be clearly readable at `1x` instead of flat front-on or reduced to a token sliver
- keep the dominant machine body near a soft `11-14 px` width on most rows, with only small accents allowed to flare slightly wider
- give the lower body enough mass that the generator reads as sturdy and grounded instead of thin, spindly, or top-heavy
- leave plausible cable attachment zones on the left, right, top, and bottom adjacency sides; do not use bulk that makes one side look non-connectable
- lowest visible base/body pixels should touch or nearly touch the bottom row so the machine does not feel like it is levitating
- stay strictly inside the exact canvas

Family consistency rules:
- belongs to the same PowerGrid hardware family as the wind generator, batteries, and conduit
- heavier and more fuel-driven than WindGenerator
- machine detail should stay vanilla-readable and not become noisy
- do not let the generator body balloon to full-canvas width, collapse into a thin center column, or lose the readable top face
- keep the off and on states silhouette-compatible, with state change coming from controlled vent/indicator cues rather than shape drift

Do not:
- output multiple options
- add smoke, steam clouds, particles, or animation effects
- make it photoreal or modern-industrial
- overload it with pipes or gauges
- turn it into a furnace clone with only a recolor
```
