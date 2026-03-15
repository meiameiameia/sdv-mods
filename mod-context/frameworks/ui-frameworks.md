# UI Frameworks

## Active UI Stack
- **Generic Mod Config Menu (`spacechase0.GenericModConfigMenu`)**
  - In-game config editing layer.
  - Already integrated by PowerGrid.
- **StardewUI (`focustense.StardewUI`)**
  - Declarative UI framework available in the pack.
  - Currently used by `Farm Terminal`.
- **Better Crafting**
  - Replaces crafting UX and affects machine-facing workflows.
- **Data Layers / Lookup Anything / NPC Map Locations / Fish Helper UI**
  - High-information overlays and inspection tools that shape user expectations for observability.

## Integration Guidance
- Keep **GMCM parity** across your SMAPI mods:
  - PowerGrid already does this.
  - Metal Kegs is a candidate for the same UX pattern.
- Treat **StardewUI** as the current dashboard framework:
  - `Farm Terminal` uses it for the read-only terminal shell.
  - `PowerGrid` currently remains on `IClickableMenu` (`PowerTabMenu`, `PowerMonitorMenu`).
- Preserve clear machine feedback in tooltips/status menus because this modpack already favors data-rich interfaces.

## Existing UI in Your Mods
- **PowerGrid**:
  - Global power tab (`PowerTabMenu`),
  - legacy location monitor (`PowerMonitorMenu`),
  - debug overlay (`DebugOverlay`).
- **Metal Kegs**:
  - No custom UI; console-driven diagnostics only.
- **FishSmoker Recipe**:
  - No UI (content patch only).
- **Farm Terminal**:
  - StardewUI-based read-only shell over `PowerGrid` snapshots.
