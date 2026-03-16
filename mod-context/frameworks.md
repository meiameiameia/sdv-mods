# Frameworks

## Core Framework Stack

- **Content Patcher**: data and asset patch framework for content packs; required by `FishSmoker Recipe`.
- **SpaceCore**: broad gameplay framework present in the pack and relevant to save/runtime compatibility.
- **Farm Type Manager (FTM)**: spawn/distribution framework used elsewhere in the pack.
- **Mail Framework Mod**: mail content extension infrastructure.
- **Fashion Sense**: framework for appearance content packs.
- **Item extensions**: item capability framework with API exposure.
- **StardewHack / BirbCore / Calcifer**: shared runtime utility cores used by dependent mods.

## Active UI Stack

- **Generic Mod Config Menu (`spacechase0.GenericModConfigMenu`)**
  - Already integrated by `PowerGrid`.
- **StardewUI (`focustense.StardewUI`)**
  - Currently used by `Farm Terminal`.
- **Better Crafting**
  - Replaces crafting UX and affects machine-facing workflows.
- **Data Layers / Lookup Anything / NPC Map Locations / Fish Helper UI**
  - High-information overlays that shape player expectations for observability.

## Implications For Repo Mods

- `FishSmoker Recipe` should remain CP-friendly and conflict-aware because CP is central in the pack.
- `Metal Kegs` and `PowerGrid` should avoid brittle assumptions about machine IDs because framework-driven content can alter machine behavior.
- `PowerGrid` integration should continue through API + `modData`, not hard references.
- `PowerGrid` can stay on `IClickableMenu` for its existing UI while `Farm Terminal` remains the StardewUI-based dashboard surface.
- GMCM parity across repo-owned SMAPI mods remains a good fit.

## Validation Notes

- Compatibility testing should keep watching:
  - `Data/Machines` interactions,
  - recipe override order,
  - machine runtime timing interactions such as `MinutesUntilReady`,
  - UI parity between text/config surfaces and richer dashboards.
