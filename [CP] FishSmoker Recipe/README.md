# [CP] FishSmoker Recipe

Tweaks the Fish Smoker crafting recipe to avoid the jelly grind while staying balanced.

## Requirements
- Content Patcher
- SMAPI

## Installation
Place the `[CP] FishSmoker Recipe` folder into your `Stardew Valley/Mods/` directory.

### Vortex Installation Workflow
1. Zip the `[CP] FishSmoker Recipe` folder.
2. Open Vortex and go to your Stardew Valley mods list.
3. Click **Install From File** in the top menu and select the zip file.
4. Enable the mod.
5. Click **Deploy**.

## Verification
1. Launch the game via SMAPI.
2. Open the crafting menu in-game.
3. Check the Fish Smoker recipe ingredients. It should now require 10 Hardwood, 5 Iron Bars, 1 Battery Pack, and 10 Coal.

## Troubleshooting
- **Recipe not changing:** Ensure Content Patcher is installed and SMAPI is running.
- **Mod not loading:** Check for wrong folder nesting in your zip (e.g., a double folder like `Mods/[CP] FishSmoker Recipe/[CP] FishSmoker Recipe`). The `manifest.json` must be directly inside the mod folder.
- **Patch conflicts:** Another mod might be editing the `Data/CraftingRecipes` entry for "Fish Smoker". Check the SMAPI console logs for any red/purple text indicating conflicts.
