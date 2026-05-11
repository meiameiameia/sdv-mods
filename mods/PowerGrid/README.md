# PowerGrid

PowerGrid adds a compact electrical infrastructure layer to Stardew Valley. You can build generators, route power with cables, store surplus EU in batteries, link locations with conduits, and speed up compatible machines when a network can keep up with demand.

Power does not duplicate items, skip inputs, or change normal machine outputs by itself. In PowerGrid, electricity is a production bonus and planning layer, not a replacement for Stardew's normal machine gameplay.

## Requirements

- Stardew Valley 1.6+
- SMAPI 4.0.0+

## Strongly Recommended

- Grapes of Ferngill for Hard Iridium Keg to use Hardwood Keg behavior
- Automate or Event Driven Automation for larger machine rooms

## Optional

- Generic Mod Config Menu for in-game settings
- UI Info Suite for extra machine status integration

## Download

Download packaged releases from [PowerGrid on Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/45572). GitHub is the source repository, not the main place to download release zips.

## Installation

1. Install SMAPI.
2. Download PowerGrid from Nexus Mods.
3. Unzip the mod into your `Stardew Valley/Mods` folder.
4. Launch the game through SMAPI.

The installed folder should look like this:

```text
Stardew Valley/Mods/[SMAPI] PowerGrid/manifest.json
```

## What PowerGrid Adds

### Power Infrastructure

- `Copper Cable`, `Iron Cable`, `Iridium Cable`, and `Energized Iridium Cable`
- `Steam Generator`
- `Combustion Generator`
- `Radioisotope Generator`
- `Wind Generator`
- `Basic Power Battery`
- `Iridium Power Battery`
- `Power Conduit`
- `Biofuel`
- `Radioisotope Fuel`

### Powered Machines

- `Industrial Preserves Jar`
- `Metal Keg`
- `Hard Iridium Keg`
- `Metal Cask`
- `Electric Smelter`
- `Industrial Recycler`
- `Powered Dehydrator`

### Machine Upgrades

- `Heating Coil`
- `Efficiency Core`
- `Catalyst Chamber`
- `Sorting Magnet`
- `Drying Rack Array`
- `Heat Regulator`

## New In 0.3

PowerGrid's industrial processing tier adds three new powered machines and a simple machine panel:

- `Electric Smelter` for powered ore smelting
- `Industrial Recycler` for powered trash recycling
- `Powered Dehydrator` for powered fruit and mushroom dehydration
- a machine panel opened with `Shift + right-click` on those new machines
- one upgrade slot per new machine

Supported upgrade pairs:

- Electric Smelter: `Heating Coil`, `Efficiency Core`, `Catalyst Chamber`
- Industrial Recycler: `Sorting Magnet`, `Efficiency Core`
- Powered Dehydrator: `Drying Rack Array`, `Heat Regulator`, `Efficiency Core`

Hold a compatible upgrade item and open the machine panel to install it. Use the same panel to remove an installed upgrade later.

## Basic Use

1. Place a generator.
2. Add fuel if the generator needs fuel.
3. Place cables next to the generator and the machines you want to power.
4. Add batteries if you want to store extra power.
5. Open the Power Tab with `P` or `K` to inspect your grid.

Power networks connect through 4-directional adjacency: up, down, left, and right. Diagonal tiles do not connect.

Powered machines can pass power to other powered machines next to them. You do not need a cable between every machine. Use cables like trunk lines: bridge gaps, connect generators or batteries, and route power around the room.

## How The New 0.3 Machines Behave

### Electric Smelter

- Smelts ore into bars and must be connected to a live PowerGrid network to start.
- If it loses power connection while processing, the current ore pops back out instead of finishing for free.
- `Heating Coil` reduces smelting time.
- `Catalyst Chamber` improves the extra bar chance.
- `Efficiency Core` reduces EU demand.

### Industrial Recycler

- Recycles supported trash into useful infrastructure materials.
- Still works without power, but power speeds it up.
- `Sorting Magnet` improves metal salvage chances.
- `Efficiency Core` reduces EU demand.

### Powered Dehydrator

- Dehydrates fruit and mushrooms like the vanilla dehydrator.
- Still works without power, but power speeds it up.
- `Drying Rack Array` can improve batch output.
- `Heat Regulator` reduces dehydration time.
- `Efficiency Core` reduces EU demand.

## Power Conduits

Power Conduits link power between locations, such as from the Farm to a Shed or FarmHouse.

To link conduits:

1. Place one conduit in each location.
2. Connect each conduit to its local power network with cables.
3. Open the Power Tab and select one conduit, then the other.

You can also right-click one conduit, then right-click the other conduit. To unlink or cancel pairing, Shift + right-click a conduit.

## Bundled Guides

The zip also includes short player guides:

- `PLAYER_GUIDE.md` for English
- `GUIA_DO_JOGADOR.pt-BR.md` for Brazilian Portuguese

The bundled Spanish guide is still waiting on a human translation refresh for the 0.3 industrial processing update.

Use the player guides for the current recipes, unlock milestones, generator outputs, cable throughputs, battery capacities, machine power values, and upgrade summaries.

## Power Tab

Press `P` or `K` to open the Power Tab.

The Power Tab shows:

- overall power status
- networks by location
- generators and fuel
- machine power state
- conduit links
- battery storage

Use this tab when something is not receiving power. It is the easiest way to see whether the problem is fuel, cables, throughput, missing conduit links, or not enough generation.

## Compatibility

### Automate / Event Driven Automation

PowerGrid is designed to work alongside automation mods:

- automation moves items
- PowerGrid provides speed boosts
- fuel generators can draw supported fuel from connected chests

Cables, batteries, and conduits are not automation connectors.

### Generic Mod Config Menu

If GMCM is installed, PowerGrid settings are available in-game. Without GMCM, edit `config.json` in the mod folder after launching the game once.

### Grapes of Ferngill

Grapes of Ferngill provides the Hardwood Keg behavior used by Hard Iridium Keg. PowerGrid can load without it, but Hard Iridium Keg will behave like a vanilla Keg instead of a Hardwood Keg.

### Mod Author API

PowerGrid includes a public SMAPI API for registering compatible custom machines and querying tile power state. See `API.md` for the current contract, a minimum working example, common mistakes, and a beginner-oriented verification checklist.

For a simple integration, most third-party machine mods only need to call `RegisterConsumer(...)`.

### Multiplayer

The host runs the power simulation. Clients receive normal synced machine state from Stardew Valley.

## Testing Before A Real Save

If you want to try PowerGrid before using it on a real farm, use a test save first. That is the safest way to experiment with recipes, layouts, conduits, automation, and balance without risking a long-running save.

PowerGrid can be added to existing saves, but very packed automation-heavy rooms may need some layout adjustment.

## Troubleshooting

### My machine is not powered

- Check that cables connect the machine to a generator network.
- Check that the generator is fueled or producing power.
- Open the Power Tab and look at the machine's network.
- Make sure the machine is actively processing something if you expect a speed bonus.

### My new 0.3 machine panel says no compatible upgrade

- Hold a compatible upgrade item in your hands before opening the panel.
- Each new industrial machine only accepts specific upgrade types.
- Only one upgrade slot is available per machine in the current version.

### My generator is offline

- Fuel generators need a supported fuel.
- Wind Generator is passive and should not need fuel.
- Indoor Wind Generators do not produce EU.

### My conduit link is wrong

- Open the Power Tab and check the Conduits tab.
- Shift + right-click a conduit to unlink it or cancel pairing.
- Link conduits again from the Power Tab if needed.

### I do not see recipes

- Check your unlock progress.
- If needed, use GMCM to change recipe settings.
- You can also use the SMAPI console command `powergrid_unlock force`.

## Console Commands

| Command | Description |
| --- | --- |
| `powergrid_status` | Print power status for the current location |
| `powergrid_tab` | Open the Power Tab |
| `powergrid_debug` | Toggle debug overlay |
| `powergrid_conduit_reset` | Cancel pending conduit pairing |
| `powergrid_unlock [force]` | Grant recipes, optionally bypassing unlock checks |

## Building From Source

Players should download packaged releases from Nexus Mods. This section is only for people building from source.

From the repository root:

```powershell
.\scripts\build-mod.ps1 -Mods PowerGrid -Bump none
```

## Credits

- Hayato2236: Spanish translation.

## License

All rights reserved until a specific license is chosen for distribution.
