# [SMAPI] PowerGrid

A cable-based electrical power network mod for Stardew Valley 1.6+.
Build generators, run cables, store energy in batteries, and power your machines for speed boosts.

**Author:** darth  
**Version:** 1.0.0  
**Requires:** SMAPI 4.0.0+, Stardew Valley 1.6+

---

## Table of Contents

- [Overview](#overview)
- [How It Works](#how-it-works)
- [Craftable Items](#craftable-items)
- [Power Simulation](#power-simulation)
- [Cross-Location Power (Conduits)](#cross-location-power-conduits)
- [Metal Kegs Integration](#metal-kegs-integration)
- [Power Monitor UI](#power-monitor-ui)
- [Configuration (GMCM)](#configuration-gmcm)
- [Console Commands](#console-commands)
- [API for Other Mods](#api-for-other-mods)
- [Building from Source](#building-from-source)
- [Compatibility](#compatibility)
- [Sprite Upgrade Guide](#sprite-upgrade-guide)
- [AI Sprite Prompt Pack](#ai-sprite-prompt-pack)
- [Testing Guide (Dev Sandbox)](#testing-guide-dev-sandbox)
- [Troubleshooting](#troubleshooting)

---

## Overview

PowerGrid adds a Factorio/IndustrialCraft-inspired electrical system to Stardew Valley:

- **Generators** produce EU (Energy Units) from fuel or passively
- **Cables** connect components via 4-directional adjacency
- **Batteries** store excess EU for later use
- **Consumers** (machines) receive EU for speed boosts

**Core principle:** Power provides ONLY speed boosts. It never spawns items, never duplicates outputs, never removes input requirements. No cheating.

---

## How It Works

1. **Place a Generator** (Steam or Wind) on your farm or in a building
2. **Run Cables** from the generator to your machines (copper/iron/iridium tiers)
3. **Optionally add Batteries** to store excess energy
4. **Machines connected via cables** get speed boosts proportional to available EU

All components must be adjacent (4-direction: up/down/left/right) to form a connected network.

---

## Craftable Items

### Cables

| Item | Recipe | Throughput |
|------|--------|------------|
| **Copper Cable** | Copper Bar x3 | 50 EU/tick |
| **Iron Cable** | Iron Bar x3 | 150 EU/tick |
| **Iridium Cable** | Iridium Bar x2 + Refined Quartz x1 | 500 EU/tick |

Throughput is limited by the **weakest cable** in the network (bottleneck).

### Generators

| Item | Recipe | Output | Fuel |
|------|--------|--------|------|
| **Steam Generator** | Iron Bar x5, Coal x5, Copper Bar x3 | 40 EU/tick | Coal (60min), Wood (20min), Hardwood (40min), Battery Pack (120min) |
| **Wind Generator** | Iron Bar x3, Wood x20, Sap x5 | 20 EU/tick base | None (passive, weather-dependent) |

Wind Generator output varies:
- **Rain/Storm:** 150% output
- **Snow:** 70% output
- **Clear:** 100% output

To fuel a Steam Generator: **right-click it with fuel in hand** (same as loading a furnace).

### Batteries

| Item | Recipe | Capacity | Daily Leak |
|------|--------|----------|------------|
| **Basic Power Battery** | Battery Pack x1, Copper Bar x5 | 500 EU | 2% |
| **Iridium Power Battery** | Battery Pack x3, Iridium Bar x2 | 2,000 EU | 2% |

### Power Conduit

| Item | Recipe | Function |
|------|--------|----------|
| **Power Conduit** | Iridium Bar x1, Battery Pack x1, Refined Quartz x2 | Links power across locations |

---

## Power Simulation

Every **10 in-game minutes**, the simulation runs:

1. **Generate EU** â€” Each generator produces its output (fuel consumed if needed)
2. **Pool available EU** â€” Generation + battery drain if demand exceeds generation
3. **Apply cable throughput cap** â€” Energy limited by weakest cable in network
4. **Allocate to consumers** â€” Sorted by priority (lower = first). Partial allocation if insufficient
5. **Store excess in batteries** â€” Remaining EU charges batteries up to capacity

### Tick Acceleration

Powered machines get their `MinutesUntilReady` reduced proportionally:

```
minutesAccelerated = tickInterval (10min) Ã— speedupFraction
speedupFraction = (EU allocated / EU demanded) Ã— maxSpeedup
```

Example: A Metal Keg with full power at 20% max speedup processes 2 extra minutes every 10-minute tick.

---

## Cross-Location Power (Conduits)

**Use case:** Generator hub on your farm, machines inside a Shed or Cellar.

1. Craft two **Power Conduits**
2. Place one in Location A (e.g., Farm) connected to your generator network via cables
3. Place one in Location B (e.g., Shed interior) connected to your machines via cables
4. Link them either by:
   - **Power Tab**: open it with your keybind (default `K`), click conduit A then conduit B
   - **World interaction**: right-click conduit A, then right-click conduit B in the other location

The two locations now share a single power network. Energy flows bidirectionally.

---

## Metal Kegs Integration

`[SMAPI] Metal Kegs` now registers its own PowerGrid consumer traits through the shared `DarthMods.API` contract when both mods are installed:

| Machine | EU/minute | Max Speedup | Priority |
|---------|-----------|-------------|----------|
| Metal Keg | 2 EU/min (20 EU/tick) | 20% | 10 |
| Hard Iridium Keg | 4 EU/min (40 EU/tick) | 20% | 10 |

- If insufficient EU, machines process at **normal speed** (no penalty)
- No output changes, no recipe changes, no item duplication
- Metal Kegs owns these tuning values in its own config / GMCM menu
- PowerGrid remains an optional integration

---

## Power Monitor UI

PowerGrid includes a global **Power Tab** (default keybind: `K`) where you can:

- Inspect network totals across locations
- Inspect per-location/per-network generator/cable/battery/consumer data
- View all conduits and current links
- Link conduits directly from the menu (click conduit A, then conduit B)
- Unlink a selected conduit (`Delete`) and refresh data (`R`)

Legacy location-scoped monitor is still available with **Shift + right-click** on any Battery or Generator.

Steam Generator fuel insertion:
- Right-click a Steam Generator while holding Coal/Wood/Hardwood/Battery Pack to add fuel
- This no longer opens the monitor on normal right-click when inserting fuel

The monitor shows:

- Stored EU / capacity for each battery
- Generation rate and fuel status
- Consumption rate per consumer
- Cable throughput cap
- Active consumers list with tile position, EU use, and current speedup

### Debug Overlay

Press **F8** (configurable) to toggle a colored overlay on the world map:
- **Green** = Cables
- **Orange** = Generators  
- **Green (bright)** = Batteries
- **Blue** = Consumers/Machines
- **Yellow** = Conduits

Enable in config: `DebugOverlayEnabled: true`

---

## Configuration (GMCM)

If [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) is installed, all settings are editable in-game.

Otherwise, edit `config.json` in the mod folder. Key settings:

```json
{
  "UnlockMode": "existingProgress",
  "AutoGrantRecipes": true,
  "EnablePowerTab": true,
  "PowerTabKeybind": "K",
  "CopperCableThroughput": 50,
  "IronCableThroughput": 150,
  "IridiumCableThroughput": 500,
  "SteamGeneratorEUPerTick": 40,
  "WindGeneratorEUPerTick": 20,
  "BasicBatteryCapacity": 500,
  "IridiumBatteryCapacity": 2000,
  "BatteryDailyLeakPercent": 2.0,
  "DebugOverlayEnabled": false,
  "DebugOverlayKeybind": "F8"
}
```

### Unlocking

PowerGrid recipes can be granted automatically on save load/day start:

- `UnlockMode: "existingProgress"` (default): grant once you can craft **Keg** or **Lightning Rod**, or you have **Mining level 6**.
- `UnlockMode: "always"`: always grant recipes (good for testing).
- `UnlockMode: "disabled"`: never auto-grant (use the console command below).

---

## Console Commands

Open the SMAPI console (`~` key) and type:

| Command | Description |
|---------|-------------|
| `powergrid_status` | Print power network status for current location |
| `powergrid_debug` | Toggle debug overlay |
| `powergrid_conduit_reset` | Cancel pending conduit pairing |
| `powergrid_unlock [force]` | Grant PowerGrid crafting recipes (use `force` to bypass unlock conditions) |
| `powergrid_tab` | Open the global Power Tab menu |

---

## API for Other Mods

Other SMAPI mods can register custom consumers:

```csharp
var api = helper.ModRegistry.GetApi<IPowerGridApi>("meiameiameia.PowerGrid");
if (api != null)
{
    api.RegisterConsumer(
        qualifiedItemId: "(BC)mymod_MyMachine",
        demandPerTick: 30,
        maxSpeedupFraction: 0.15f,
        priority: 20,
        displayName: "My Custom Machine"
    );
}
```

Available methods:
- `RegisterConsumer(...)` / `UnregisterConsumer(...)`
- `IsTilePowered(locationName, tile)`
- `GetSpeedupAtTile(locationName, tile)`
- `GetTotalStoredEU(locationName)`
- `GetNetworkSnapshots(locationName?)`
- `GetConsumerSnapshots(locationName?)`
- `GetGeneratorSnapshots(locationName?)`
- `GetBatterySnapshots(locationName?)`

These snapshot methods are read-only and are intended for monitoring UIs or terminal-style dashboards.

### Power metadata (`modData`)

PowerGrid writes live status to object `modData` each simulation tick using keys prefixed with `darth.PowerGrid/`.
That prefix intentionally stays unchanged for save/modData compatibility even though the mod UniqueID is now `meiameiameia.PowerGrid`.

Shared keys:
- `type` (e.g. `Cable`, `Generator`, `Battery`, `Conduit`, `Consumer`, `Standalone`)
- `connected` (`0`/`1`)
- `networkId` (int, `-1` when standalone)
- `lastTickTime` (in-game time, e.g. `610`)

Cable keys:
- `cableTier` (`Copper`/`Iron`/`Iridium`)
- `throughputCap` (int)
- `connectionMask` (0-15)
- `connectedSides` (e.g. `U,R,D`)

Generator keys:
- `euPerTick` (int)
- `generatedThisTick` (int)
- `requiresFuel` (`0`/`1`)
- `fuelTicksRemaining` (int, `-1` for passive generators)
- `online` (`0`/`1`)

Battery keys:
- `charge` (int)
- `capacity` (int)
- `chargePercent` (float 0-1)
- `drainedThisTick` (int)
- `storedThisTick` (int)

Conduit keys:
- `linked` (`0`/`1`)
- `partnerLocation` (string)
- `partnerTile` (`x,y`)

Consumer keys:
- `powered` (`0`/`1`)
- `euAllocated` (int)
- `euDemanded` (int)
- `speedupFraction` (float)
- `minutesAccelerated` (int per tick)
- `minutesRemaining` (int)

Tooltip/hover mods can read these keys to show consistent real-time power status.

---

## Building from Source

```powershell
cd "c:\dev\sdv-mod\[SMAPI] PowerGrid"
dotnet build
```

This builds `PowerGrid.dll` and copies it to the mod root folder. The mod is self-contained and does not affect the other mods in the monorepo (`[CP] FishSmoker Recipe`, `[SMAPI] Metal Kegs`).

To install: copy the entire `[SMAPI] PowerGrid` folder into your `Stardew Valley/Mods/` directory (or symlink it).

---

## Compatibility

### With [CP] FishSmoker Recipe
- **No conflict.** PowerGrid is a SMAPI C# mod; FishSmoker Recipe is a Content Patcher pack. They operate in completely different domains. PowerGrid does not touch `Data/ObjectInformation` or fish smoker recipes.

### With [SMAPI] Metal Kegs
- **Designed to work together.** Metal Kegs registers `Metal Keg` and `Hard Iridium Keg` as PowerGrid consumers through the shared `DarthMods.API` integration surface.
- PowerGrid does NOT modify `Data/Machines` entries for Metal Kegs. It only adjusts `MinutesUntilReady` at runtime via tick events.
- Metal Kegs works perfectly without PowerGrid installed (no speedup, normal behavior).

### With Automate
- **Partially compatible by design.** Automate handles machine input/output chains; PowerGrid handles speed boosts.
- Metal Kegs and Hard Iridium Kegs can still be automated normally.
- PowerGrid objects (cables/generators/batteries/conduits) are not Automate connectors, so they won't appear as connected in Automate's overlay.
- Use PowerGrid's `Power Tab`, `powergrid_status`, and `DebugOverlay` to inspect electrical connectivity.

### Multiplayer
- **Host-authoritative:** Only the host runs the power simulation.
- Clients see synced `MinutesUntilReady` values naturally via SDV's built-in object sync.

---

## Sprite Upgrade Guide

The authoritative sprite workflow, locked specs, and ready-to-paste external prompts now live under `mod-context/art/`.
Prefer those files over the legacy guidance below when generating or replacing shipped art.

The mod ships with **runtime-generated placeholder sprites** (tinted vanilla textures). Here's how to replace them with custom art.

### Step 1: Recommended Free Tools

| Tool | Platform | Best For |
|------|----------|----------|
| [LibreSprite](https://libresprite.github.io/) | Win/Mac/Linux | Full pixel art editor (Aseprite fork, free) |
| [Piskel](https://www.piskelapp.com/) | Browser | Quick online pixel art, good for beginners |
| [Paint.NET](https://www.getpaint.net/) | Windows | General image editing with pixel-level control |
| [GIMP](https://www.gimp.org/) | Win/Mac/Linux | Advanced image editing |
| [Lospec Palette List](https://lospec.com/palette-list) | Browser | Find Stardew-like color palettes |

### Step 2: Sprite Size Requirements

All PowerGrid items are **BigCraftables**.

**For Generators, Batteries, and Conduits:**
- **Canvas size:** 16Ã—32 pixels (1 tile wide, 2 tiles tall)
- **Transparent background** (PNG with alpha channel)
- The bottom 16Ã—16 is the "base" that sits on the ground tile
- The top 16Ã—16 is the upper portion visible above

**For Cables (Autotiling):**
- **Canvas size:** 64Ã—128 pixels (A 4Ã—4 grid of 16Ã—32 frames)
- Cables dynamically draw connections to adjacent grid items.
- The spritesheet contains 16 frames based on a bitmask (Up=1, Right=2, Down=4, Left=8).
- **Frame Layout (Column = mask % 4, Row = mask / 4):**
  - **Row 0:** [0] None  | [1] U      | [2] R      | [3] U,R
  - **Row 1:** [4] D     | [5] U,D    | [6] R,D    | [7] U,R,D
  - **Row 2:** [8] L     | [9] U,L    | [10] R,L   | [11] U,R,L
  - **Row 3:** [12] D,L  | [13] U,D,L | [14] R,D,L | [15] U,R,D,L
- Even though the cable is drawn flat on the ground, each frame is still 16Ã—32 (the cable should be drawn in the bottom 16Ã—16 area of each frame, with the top 16Ã—16 left transparent).

### Step 3: Kitbashing from Vanilla Sprites

"Kitbashing" = starting from a similar vanilla sprite and modifying it. This is the fastest path to decent sprites.

1. **Export vanilla base sprites** using a tool like [Stardew Valley Sprite Viewer](https://mouseypounds.github.io/stardew-checkup/) or extract from `Content/TileSheets/Craftables.png`
2. **Pick a similar vanilla sprite:**
   - Cables: use Chest, Torch, or path tile as base
   - Generators: use Furnace, Kiln, or Oil Maker as base
   - Batteries: use Lightning Rod or Crystalarium as base
3. **Modify 3-5 key features:**
   - Change the silhouette (add/remove protrusions)
   - Swap primary color palette
   - Add a distinguishing icon/marking
   - Modify highlight positions
   - Change the top shape

### Step 4: No-Design Workflow

1. Pick a vanilla base sprite that matches the "feel" (e.g., Furnace for generators)
2. Open in LibreSprite/Piskel
3. Duplicate to a new file at 16Ã—32
4. **Recolor:** Select the main body color and shift hue (e.g., gray â†’ copper orange for copper cable)
5. **Reshape:** Move 3-5 pixels on the outline to create a new silhouette
6. **Add detail:** Draw 1-2 small distinctive marks (a lightning bolt, gear, wire, etc.)
7. Save as PNG with transparency
8. Test in-game (see Step 6)
9. Iterate until satisfied

### Step 5: Stardew Art Style Checklist

- [ ] **1px dark outline** around the entire object (usually dark brown/black, ~RGB 50,30,20)
- [ ] **Top-left light source** â€” highlights on top-left edges, shadows on bottom-right
- [ ] **Limited palette** â€” Use 4-6 colors max per object (base, highlight, shadow, accent, outline)
- [ ] **Consistent outline thickness** â€” Always 1px, never 2px or 0px
- [ ] **No anti-aliasing** â€” Stardew uses hard pixel edges, not smooth gradients
- [ ] **1px shading bands** â€” Light â†’ base â†’ shadow transitions are 1 pixel wide
- [ ] **Readable at 1x zoom** â€” The sprite should be recognizable when tiny
- [ ] **Stardew palette feel** â€” Warm, slightly desaturated colors. Avoid neon/pure colors.
- [ ] **Ground contact** â€” Bottom row should suggest the object sits on the ground (shadow or flat base)

### Step 6: Testing Sprites

1. Place your PNG in `[SMAPI] PowerGrid/Assets/` with the exact filename:
   - `CopperCable.png`, `IronCable.png`, `IridiumCable.png`
   - `SteamGenerator.png`, `WindGenerator.png`
   - `BasicBattery.png`, `IridiumBattery.png`
   - `PowerConduit.png`
2. **Hot-reload** in SMAPI console: type `patch reload meiameiameia.PowerGrid`
   - If that doesn't work, try: `invalidate Mods/meiameiameia.PowerGrid/CopperCable` (etc.)
3. **Common pitfalls:**
   - Wrong file size (must be exactly 16Ã—32)
   - Non-transparent background (use PNG-24 with alpha)
   - File named `.PNG` instead of `.png` (case matters on some systems)
   - Forgot to save with transparency layer enabled
   - Offset issues: the sprite may appear shifted if canvas isn't exactly 16Ã—32

---

## AI Sprite Prompt Pack

This section is legacy prompt context only.
Use `mod-context/art/prompts/` as the source of truth for current external-generation prompts.

Copy-paste these prompts into an AI image generator (Stable Diffusion, DALL-E, Midjourney) and then manually clean up the results in a pixel editor.

### General Style Prompt Prefix
```
pixel art, 16x32 pixels, stardew valley style, top-down RPG item sprite, 
1px dark outline, top-left lighting, warm desaturated palette, transparent background, 
no anti-aliasing, clean pixel edges, game asset
```

### Per-Item Prompts

**Copper Cable:**
```
[style prefix] small copper wire conduit, orange-brown copper color, 
coiled wire detail, compact industrial look, floor-mounted cable
```

**Iron Cable:**
```
[style prefix] iron wire conduit, silver-gray metallic, 
reinforced wire look, industrial cable, floor-mounted
```

**Iridium Cable:**
```
[style prefix] iridium wire conduit, purple-violet metallic sheen, 
premium glowing cable, high-tech industrial, floor-mounted
```

**Steam Generator:**
```
[style prefix] small steam engine generator, iron-gray body, 
smoke stack on top, furnace grate at bottom, industrial boiler, coal-powered
```

**Wind Generator:**
```
[style prefix] small wind turbine, light blue and white, 
spinning blades on top, wooden/metal pole base, farm windmill
```

**Basic Power Battery:**
```
[style prefix] small green battery unit, copper terminals on top, 
charge indicator bar, industrial energy storage, compact machine
```

**Iridium Power Battery:**
```
[style prefix] advanced purple battery unit, iridium-plated, 
glowing energy core, high capacity storage, premium industrial machine
```

**Power Conduit:**
```
[style prefix] golden energy relay device, crystal or antenna on top, 
magical-industrial hybrid, teleportation node, energy bridge device
```

### Post-Processing Workflow
1. Generate at higher resolution (64Ã—128 or 128Ã—256)
2. Downscale to 16Ã—32 using **nearest-neighbor** interpolation (no smoothing!)
3. Clean up in pixel editor: fix outline to consistent 1px, remove anti-aliased pixels
4. Adjust palette to match Stardew's warm tones
5. Ensure transparent background (no white/colored background pixels)
6. Test in-game

---

## Testing Guide (Dev Sandbox)

### Setting Up a Dev Save

1. **Create a dedicated test save:**
   - Start a new game with a simple farm (Four Corners or Standard)
   - Name it something like "DEV_PowerGrid"
   - Skip the intro: in SMAPI console, type `debug warp Farm` right after cutscene

2. **Essential SMAPI console commands for testing:**

```
# Give yourself all PowerGrid recipes
player_add recipe "Copper Cable"
player_add recipe "Iron Cable"
player_add recipe "Iridium Cable"
player_add recipe "Steam Generator"
player_add recipe "Wind Generator"
player_add recipe "Basic Power Battery"
player_add recipe "Iridium Power Battery"
player_add recipe "Power Conduit"

# Give yourself materials
player_add 334 100    # Copper Bar
player_add 335 100    # Iron Bar
player_add 337 50     # Iridium Bar
player_add 338 50     # Refined Quartz
player_add 382 200    # Coal
player_add 388 500    # Wood
player_add 787 20     # Battery Pack
player_add 92 50      # Sap

# Give yourself Metal Kegs (if installed)
player_add (BC)darth.MetalKegs_MetalKeg 5
player_add (BC)darth.MetalKegs_HardIridiumKeg 5

# Give yourself PowerGrid items directly
player_add (BC)darth.PowerGrid_CopperCable 20
player_add (BC)darth.PowerGrid_SteamGenerator 3
player_add (BC)darth.PowerGrid_BasicBattery 2
player_add (BC)darth.PowerGrid_PowerConduit 2

# Time manipulation
debug time 600        # Set time to 6:00 AM
debug speed 10        # Speed up time (careful!)
debug newday          # Skip to next day

# Warp commands
debug warp Farm
debug warp Shed       # Warp into a shed
debug warp Cellar     # Warp into cellar

# Check PowerGrid status
powergrid_status
powergrid_debug
```

3. **Testing workflow:**
   - Place generators + cables + batteries + machines in a line
   - Run `powergrid_status` to verify network detection
   - Toggle `powergrid_debug` to see the overlay
   - Advance time with `debug time XXXX` and watch `MinutesUntilReady` decrease
   - Test conduit pairing between Farm and Shed

4. **Building and testing quickly:**

```powershell
# Build the mod
cd "c:\dev\sdv-mod\[SMAPI] PowerGrid"
dotnet build

# Option A: Symlink (recommended, one-time setup)
# Run PowerShell as Administrator:
New-Item -ItemType Junction -Path "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\[SMAPI] PowerGrid" -Target "c:\dev\sdv-mod\[SMAPI] PowerGrid"

# Option B: Copy after each build
Copy-Item -Recurse -Force "c:\dev\sdv-mod\[SMAPI] PowerGrid" "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\"
```

5. **Verify no conflicts:**
   - Ensure `[SMAPI] Metal Kegs` loads without errors in SMAPI log
   - Ensure `[CP] FishSmoker Recipe` loads without errors
   - Check SMAPI log for any `[PowerGrid] ERROR` lines

### Debugging Tips

- **SMAPI log location:** `%AppData%\StardewValley\ErrorLogs\SMAPI-latest.txt`
- **Verbose logging:** Set SMAPI to `VerboseLogging: true` in `smapi-internal/config.json`
- **Hot reload DLL:** Unfortunately, DLL hot-reload isn't supported by SMAPI. You must restart the game after rebuilding. However, you can use `debug save` + `debug load` to quickly reload a save without fully restarting.
- **Check network graph:** Run `powergrid_status` after placing items to verify the graph builder detected everything correctly.

---

## Troubleshooting

**Q: Machines aren't getting speed boost**
- Ensure cables connect the generator to the machine (4-directional adjacency, no gaps)
- Check `powergrid_status` - the machine should appear as a "Consumer"
- Verify the machine is actively processing (has an item inside and MinutesUntilReady > 0)
- Check if cable throughput is sufficient

**Q: Generator not producing EU**
- Steam Generator: needs fuel inside. Right-click with Coal/Wood/Hardwood/Battery Pack
- Wind Generator: always produces some EU, but output varies with weather

**Q: Conduits not linking**
- Use the Power Tab: click conduit A, then conduit B
- Or right-click conduit A, then right-click conduit B in the other location
- If you get "Pairing cancelled", try again. Use `powergrid_conduit_reset` to clear state.

**Q: Battery charge disappearing**
- By default, batteries leak 2% of stored EU each morning. Adjust `BatteryDailyLeakPercent` in config.

---

## License

This mod is part of the darth SDV modding monorepo. All rights reserved until a specific license is chosen for distribution.

