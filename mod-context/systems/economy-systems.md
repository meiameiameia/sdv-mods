# Economy Systems

## Economy Expansion Signals
This modpack has a dense artisan-economy layer:
- **Grapes of Ferngill (CP + FTM)**: wine ecosystem expansion.
- **Cornucopia - More Crops / Artisan Machines**: crop and machine product breadth.
- **Artisan Goods Keep Quality**: quality-retention policy shift for artisan outputs.
- **Vegas' Item Compatibility Patch**: cross-pack item/machine compatibility normalization.
- **Fish Pond Aquaponics / Better Fish Ponds**: alternate production chains.
- **RushOrders / RentedToolsRefresh / Self Serve / Let Me Shop**: progression and service pacing changes.

## Your Mods In Economy Flow
- **Metal Kegs**: adds additional fermentation endpoints (keg-like processing capacity).
- **PowerGrid**: increases effective throughput (time-to-output) rather than changing outputs.
- **FishSmoker Recipe**: modifies crafting gate to Fish Smoker access.

## Integration Pressure Points
- **Recipe conflicts**:
  - FishSmoker Recipe edits `Data/CraftingRecipes` entry directly.
  - Other packs may also touch that key.
- **Machine rule conflicts**:
  - Metal Kegs binds into `Data/Machines`, where EMC and compatibility patches may also operate.
- **Throughput stacking**:
  - PowerGrid speedup + automation + expanded machine pools can amplify output rates; balancing may need periodic review.

## Practical Compatibility Focus
- Validate machine registration after load (`Data/Machines` keys for both keg variants).
- Validate recipe final state for `Fish Smoker` after all CP patches apply.
- Validate economy pacing with both automation and PowerGrid acceleration active.
