# PowerGrid

PowerGrid adds a small electrical system to Stardew Valley. You can build generators, run cables, store extra power in batteries, and use powered artisan machines that work faster when your grid can support them.

The goal is to make production rooms a little more interesting to plan without replacing Stardew's normal crafting and farming loop. Power does not duplicate items, change machine outputs, or skip input requirements.

## Requirements

- Stardew Valley 1.6+
- SMAPI 4.0.0+

## Strongly Recommended

- Grapes of Ferngill for Hard Iridium Keg to use Hardwood Keg behavior
- Automate or Event Driven Automation for chest-based machine logistics

## Optional

- Generic Mod Config Menu for in-game settings

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
- `Wind Generator`
- `Basic Power Battery`
- `Iridium Power Battery`
- `Power Conduit`
- `Biofuel`

### Powered Machines

- `Industrial Preserves Jar`
- `Metal Keg`
- `Hard Iridium Keg`
- `Metal Cask`

## Basic Use

The zip also includes a short player guide in English and Brazilian Portuguese:

- `PLAYER_GUIDE.md`
- `GUIA_DO_JOGADOR.pt-BR.md`

1. Place a generator.
2. Add fuel if the generator needs fuel.
3. Place cables next to the generator and the machines you want to power.
4. Add batteries if you want to store extra power.
5. Open the Power Tab with `P` or `K` to inspect your grid.

Power networks connect through 4-directional adjacency: up, down, left, and right. Diagonal tiles do not connect.

Powered machines can pass power to other powered machines next to them. You do not need to place a cable between every machine. A good rule of thumb is to use cables like trunk lines: bridge gaps, reach generators or batteries, and route power around the room.

Cables currently occupy their own tile. I know that farm space and tidy layouts matter, so a future wiring polish pass may explore cable-underlay or buried-cable behavior. For now, plan cables as visible infrastructure.

## Generators And Fuel

| Generator | Output | Fuel |
| --- | ---: | --- |
| Steam Generator | 75 EU/tick | Coal, Wood, Hardwood |
| Combustion Generator | 240 EU/tick | Biofuel |
| Wind Generator | 25 EU/tick base | Passive, outdoors only |

Wind Generators only produce EU outdoors. Indoor wind generators stay offline. Outdoor Wind Generator output changes with weather:

- Clear weather: normal output
- Rain or storm: higher output
- Snow: lower output

Right-click a Steam Generator with Coal, Wood, or Hardwood to fuel it. Right-click a Combustion Generator with Biofuel to fuel it.

## Biofuel

Biofuel is a crafted midgame fuel for the Combustion Generator.

| Item | Recipe |
| --- | --- |
| Biofuel | Fiber x10, Wood x5, Coal x1, crafts 8 |

## Cables

| Cable | Recipe | Throughput |
| --- | --- | ---: |
| Copper Cable | Copper Bar x3, crafts 10 | 50 EU/tick |
| Iron Cable | Iron Bar x3, crafts 10 | 250 EU/tick |
| Iridium Cable | Iridium Bar x2, Refined Quartz x1, crafts 10 | 1,000 EU/tick |
| Energized Iridium Cable | Iridium Bar x4, Radioactive Bar x1, Battery Pack x2, Refined Quartz x3, crafts 10 | 3,000 EU/tick |

Throughput is how much power a network can move each tick. If a network contains multiple cable tiers, the weakest cable limits the network.

## Batteries

| Battery | Recipe | Capacity |
| --- | --- | ---: |
| Basic Power Battery | Battery Pack x1, Copper Bar x5, Refined Quartz x2 | 500 EU |
| Iridium Power Battery | Battery Pack x3, Iridium Bar x2, Refined Quartz x5 | 2,000 EU |

Batteries store extra power and help cover demand when generators cannot keep up. They are especially useful when your wind output drops, fuel runs out, or a group of machines starts working at once. By default, batteries leak a small amount of stored energy each morning.

Battery Packs are currently used in the recipes for Wind Generators, Basic Power Batteries, Iridium Power Batteries, and Power Conduits. Future PowerGrid updates may give Battery Packs more electrical uses, but they are not direct generator fuel in the current release.

## Power Conduits

Power Conduits link power between locations, such as from the Farm to a Shed or FarmHouse.

To link conduits:

1. Place one conduit in each location.
2. Connect each conduit to its local power network with cables.
3. Open the Power Tab and select one conduit, then the other.

You can also right-click one conduit, then right-click the other conduit. To unlink or cancel pairing, Shift + right-click a conduit.

## Powered Machines

| Machine | Recipe | Power Use | Max Power Bonus |
| --- | --- | ---: | ---: |
| Industrial Preserves Jar | Wood x30, Coal x6, Iron Bar x6, Refined Quartz x1 | 20 EU/tick | 20% faster |
| Metal Keg | Iron Bar x8, Copper Bar x6, Refined Quartz x1 | 10 EU/tick | 20% faster |
| Hard Iridium Keg | Iridium Bar x5, Iron Bar x4, Refined Quartz x1 | 30 EU/tick | 30% faster |
| Metal Cask | Hardwood x10, Iron Bar x8, Iridium Bar x3, Refined Quartz x1 | 40 EU/tick | 50% faster aging |

Powered machines still work normally without power. PowerGrid is a bonus layer, not a punishment layer. When a powered machine has enough EU, it gets a speed bonus. Metal Casks use power for faster aging progress.

Hard Iridium Keg is designed to use Grapes of Ferngill's Hardwood Keg behavior. If Grapes of Ferngill is not installed, PowerGrid falls back to vanilla Keg behavior so the machine can still work.

By default, the bonuses are:

- Industrial Preserves Jar: up to 20% faster
- Metal Keg: up to 20% faster
- Hard Iridium Keg: up to 30% faster
- Metal Cask: up to 50% faster aging bonus

If Generic Mod Config Menu is installed, you can tune each powered machine's EU demand, maximum speed bonus, and priority in-game.

## Recipe Unlocks

By default, PowerGrid grants recipes as you progress:

| Bundle | Unlock |
| --- | --- |
| Grid Starter | Mining 5 or know Lightning Rod |
| Powered Artisan | know Preserves Jar or Keg |
| Fuel Tech | Mining 7 and know Lightning Rod |
| Advanced Grid | Mining 9 and know Lightning Rod |
| High Density Grid | Mining 10, know Lightning Rod, and know Solar Panel |

This can be changed in the config.

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

- Automate-style mods move items.
- PowerGrid provides speed boosts.
- Steam Generators can draw supported fuel from connected chests.
- Combustion Generators can draw Biofuel from connected chests.

Cables, batteries, and conduits are not automation connectors.

### Generic Mod Config Menu

If GMCM is installed, PowerGrid settings are available in-game. Without GMCM, edit `config.json` in the mod folder after launching the game once.

### Grapes of Ferngill

Grapes of Ferngill provides the Hardwood Keg behavior used by Hard Iridium Keg. PowerGrid can load without it, but Hard Iridium Keg will behave like a vanilla Keg instead of a Hardwood Keg.

### Multiplayer

The host runs the power simulation. Clients receive normal synced machine state from Stardew Valley.

## Testing Before A Real Save

If you want to try PowerGrid before using it on your real farm, use Cinderleaf's sandbox capabilities and a test save. That is the recommended way to experiment with recipes, layouts, conduits, automation, and balance without risking your main save.

PowerGrid can be added to existing saves, but very packed Automate buildings may need some layout adjustment. The mod is easiest to learn on a new or test save where power rooms and machine rows can be planned from the start.

## Troubleshooting

### My machine is not powered

- Check that cables connect the machine to a generator network.
- Check that the generator is fueled or producing power.
- Open the Power Tab and look at the machine's network.
- Make sure the machine is actively processing something.

### My generator is offline

- Steam Generator needs Coal, Wood, or Hardwood.
- Combustion Generator needs Biofuel.
- Wind Generator is passive and should not need fuel.

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

The local zip is written to `artifacts/mod-zips/`.

## Credits

- Hayato2236: Spanish translation.

## License

All rights reserved until a specific license is chosen for distribution.
