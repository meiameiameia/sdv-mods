# FishSmoker Recipe

## Purpose
`[CP] FishSmoker Recipe` rebalances the Fish Smoker recipe to remove the jelly requirement while keeping a mid/late-game cost profile.

## Current Architecture
- Content pack type: Content Patcher.
- `content.json` performs one `EditData` operation:
  - target: `Data/CraftingRecipes`
  - entry: `Fish Smoker`
  - recipe value: `709 10 335 5 787 1 382 10/Home/FishSmoker/true/null/`
- No custom runtime code, API, or config layer.

## Integration Opportunities With This Modpack
- Works naturally with CP-centered economy packs but shares a high-conflict target (`Data/CraftingRecipes`).
- Should be validated against machine/economy packs that also modify fish-related crafting progression.
- Throughput implications increase when combined with automation and PowerGrid-accelerated machine loops.

## Potential Improvements
- Add optional compatibility conditions for known competing recipe patches.
- Add update key in manifest (log flags missing update key).
- Consider documenting patch-order expectations for large CP modpacks.
