# [SMAPI] Metal Kegs

Adds two new big craftable machines for Stardew Valley 1.6:

- **Metal Keg**: behavior-identical to the vanilla **Keg**.
- **Hard Iridium Keg**: behavior-identical to GOF's **Hardwood Keg** when Grapes of Ferngill (GOF) is installed.

## Requirements
- Stardew Valley 1.6.15
- SMAPI 4.5.1
- (Optional) Grapes of Ferngill (GOF): required for Hard Iridium Keg to match Hardwood Keg behavior.

## Installation (Vortex)
1. Zip the `[SMAPI] Metal Kegs` folder.
2. Open Vortex and go to your Stardew Valley mods list.
3. Click **Install From File** and select the zip file.
4. Enable the mod.
5. Click **Deploy**.

## Config
`config.json`:

- `UnlockMode`
  - `existingProgress` (default): unlocks recipes based on your current save progress.
  - `always`: grants recipes immediately (on next day start).
- `MissingGofMode`
  - `disable` (default): disables the **Hard Iridium Keg** recipe if GOF isn't installed.
  - `fallbackVanillaKeg`: allows crafting **Hard Iridium Keg** even without GOF (it will behave like a vanilla Keg).
- `EnablePowerGridIntegration`
  - `true` (default): registers Metal Kegs as PowerGrid consumers when PowerGrid is installed.
- `MetalKegEUPerMinute`, `MetalKegMaxSpeedup`, `MetalKegPriority`
  - PowerGrid energy tuning for Metal Keg.
- `HardIridiumKegEUPerMinute`, `HardIridiumKegMaxSpeedup`, `HardIridiumKegPriority`
  - PowerGrid energy tuning for Hard Iridium Keg.

If [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) is installed, these settings can also be edited in-game.

## Unlocking Rules (existingProgress)
- **Metal Keg**: unlock when `FarmingLevel >= 8` OR you have crafted at least 1 vanilla Keg.
- **Hard Iridium Keg**: unlock when you have a **Cellar** (final house upgrade) OR you've shipped at least **200** wines total.

Recipes are granted on the next `DayStarted` after requirements are met (including the first day after installing the mod).

## Verification: Metal Keg vs Keg
Test that Metal Keg matches vanilla Keg behavior:

1. Place a vanilla **Keg** and a **Metal Keg**.
2. Insert the same inputs into each and compare:
   - Fruit -> Wine
   - Hops -> Pale Ale
   - Wheat -> Beer
   - Honey -> Mead
3. Confirm:
   - Same accepted inputs.
   - Same processing time.
   - Same output and quality/edge behavior.

## Verification: Hard Iridium Keg vs GOF Hardwood Keg
With GOF installed:

1. Place GOF's **Hardwood Keg** and the **Hard Iridium Keg**.
2. Insert wine-related inputs that Hardwood Keg fortifies.
3. Confirm the fortified output matches, including the GOF fortified wine ID:
   - Fortified Wine qualified item ID must be `(O)GOF_Fortified_Wine`.

## Automate Compatibility
Both machines are standard big craftable machines (held object + minutes until ready) and should work with `Pathoschild.Automate` without special connectors.

## PowerGrid Compatibility
- PowerGrid integration is optional.
- Metal Kegs owns its own PowerGrid tuning values and registers them at runtime if PowerGrid is installed.
- Legacy PowerGrid-owned Metal Keg settings are migrated once into Metal Kegs config when possible.

## Troubleshooting
- **Hard Iridium Keg recipe missing**
  - Ensure GOF is installed, or set `MissingGofMode` to `fallbackVanillaKeg`.
- **Wrong zip nesting**
  - Avoid: `Mods/[SMAPI] Metal Kegs/[SMAPI] Metal Kegs/...`
  - The `manifest.json` must be directly inside `[SMAPI] Metal Kegs`.
- **Automate not picking it up**
  - Confirm Automate is installed and updated.
  - Check SMAPI console for errors/warnings from this mod.
