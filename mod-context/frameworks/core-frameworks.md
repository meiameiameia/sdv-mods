# Core Frameworks

## Primary Framework Stack
- **Content Patcher**: data and asset patch framework for 36 content packs; required by FishSmoker Recipe.
- **SpaceCore**: broad gameplay framework used by many mods; save serializer changer in this pack.
- **Farm Type Manager (FTM)**: spawn/distribution framework used by `(FTM) Grapes of Ferngill`.
- **Mail Framework Mod**: mail content extension infrastructure.
- **Fashion Sense**: framework for appearance content packs (for example `Luny's Small Boots`).
- **Item extensions**: item capability framework with API exposure.
- **StardewHack / BirbCore / Calcifer**: shared code/runtime utility cores used by dependent mods.

## Implications for Your Mods
- **FishSmoker Recipe** should remain CP-friendly and conflict-aware because CP is a central dependency in this pack.
- **Metal Kegs** and **PowerGrid** should avoid brittle assumptions about machine IDs because machine behavior can be altered by framework-driven content packs.
- **PowerGrid API + modData** is the cleanest integration style in a framework-dense pack: soft dependencies, runtime detection, no hard references.

## Stability Notes
- The log reports many Harmony patchers and one save serializer changer (`SpaceCore`), so compatibility testing should focus on:
  - `Data/Machines` interactions,
  - recipe override order,
  - in-game machine runtime timing interactions (`MinutesUntilReady`).
