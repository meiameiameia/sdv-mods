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
State variants: none
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
- strong grounded base in the lower rows
- stay strictly inside the exact canvas

Family consistency rules:
- belongs to the same PowerGrid hardware family as the wind generator, batteries, and conduit
- heavier and more fuel-driven than WindGenerator
- machine detail should stay vanilla-readable and not become noisy

Do not:
- output multiple options
- add smoke, steam clouds, particles, or animation effects
- make it photoreal or modern-industrial
- overload it with pipes or gauges
- turn it into a furnace clone with only a recolor
```
