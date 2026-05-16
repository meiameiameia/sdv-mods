# PowerGrid Player Guide

This is the quick practical guide for using PowerGrid without needing to learn the whole system by trial and error.

PowerGrid adds generators, batteries, cables, conduits, fuel items, powered artisan machines, and a small industrial processing tier. Powered machines do not duplicate items or skip inputs. They simply work faster when their network can supply enough EU.

## First Setup

Start small:

1. Place a `Steam Generator`.
2. Fuel it with `Coal`, `Wood`, or `Hardwood`.
3. Place `Copper Cable` next to the generator.
4. Place a powered machine next to the cable or next to another powered machine.
5. Add a `Basic Power Battery` when you can.
6. Open the Power Tab with `P` or `K` to inspect the network.

Machines and PowerGrid objects connect through up, down, left, and right. Diagonal tiles do not connect.

Powered machines can pass power to other powered machines touching them, so you do not need a cable between every machine. Use cables like trunk lines.

## Important Rules

- Fuel generators produce their full EU while running.
- Extra EU is stored in batteries if the network has room.
- If there is no battery room, extra EU is wasted.
- Batteries help smooth demand and make generators much less wasteful.
- Wind Generators only produce power outdoors.
- Most powered machines still work normally without power.
- Power is a speed bonus, not a replacement for normal machine behavior.
- `Electric Smelter` is the main exception: it needs a live powered network to start smelting.

## Unlocks

| Bundle | Unlock Condition | Recipes |
| --- | --- | --- |
| Grid Starter | Mining 5 or know Lightning Rod | Copper Cable, Steam Generator, Basic Power Battery |
| Powered Artisan | Know Preserves Jar or Keg | Industrial Preserves Jar, Metal Keg |
| Fuel Tech | Mining 7 and know Lightning Rod | Biofuel, Iron Cable, Combustion Generator, Electric Smelter, Heating Coil, Efficiency Core, Catalyst Chamber, Industrial Recycler, Sorting Magnet, Powered Dehydrator, Drying Rack Array, Heat Regulator |
| Advanced Grid | Mining 9 and know Lightning Rod | Iridium Cable, Wind Generator, Iridium Power Battery, Power Conduit, Metal Cask, Hard Iridium Keg |
| High Density Grid | Mining 10, know Lightning Rod, know Solar Panel, and know Iridium Power Battery | Energized Iridium Cable, Radioisotope Fuel, Radioisotope Generator |

If you use Generic Mod Config Menu, recipe unlock behavior can be changed in-game.

## Generators

| Generator | Output | Fuel | Notes |
| --- | ---: | --- | --- |
| Steam Generator | 75 EU/tick | Coal, Wood, Hardwood | Early generator. Good for small rooms. |
| Combustion Generator | 240 EU/tick | Biofuel | Midgame generator. Good for larger machine groups. |
| Radioisotope Generator | 900 EU/tick | Radioisotope Fuel, Radioactive Bar | Late-game density generator for heavy machine rooms. |
| Wind Generator | 25 EU/tick base | none | Passive power, outdoors only, weather adjusted. |

Generator recipes:

| Generator | Recipe |
| --- | --- |
| Steam Generator | Iron Bar x5, Copper Bar x2, Coal x6, Refined Quartz x1 |
| Combustion Generator | Steam Generator x1, Iron Bar x8, Gold Bar x5, Refined Quartz x3 |
| Radioisotope Generator | Combustion Generator x1, Radioactive Bar x3, Iridium Bar x6, Iridium Power Battery x1, Refined Quartz x4 |
| Wind Generator | Iron Bar x6, Refined Quartz x4, Battery Pack x1, Coal x4 |

Wind output changes with weather:

- Clear weather: normal output
- Rain or storm: higher output
- Snow: lower output

Right-click a `Steam Generator` while holding `Coal`, `Wood`, or `Hardwood` to fuel it. Right-click a `Combustion Generator` while holding `Biofuel` to fuel it. Right-click a `Radioisotope Generator` while holding `Radioisotope Fuel` or a `Radioactive Bar` to fuel it.

## Fuel

| Fuel | Used By | Notes |
| --- | --- | --- |
| Coal | Steam Generator | Strongest Steam fuel. |
| Wood | Steam Generator | Easy backup fuel. |
| Hardwood | Steam Generator | Better than Wood. |
| Biofuel | Combustion Generator | Crafted midgame fuel. |
| Radioisotope Fuel | Radioisotope Generator | Crafted late-game fuel. Much more efficient than raw bars. |
| Radioactive Bar | Radioisotope Generator | Legacy raw fuel. Less efficient than Radioisotope Fuel. |

Biofuel recipe:

| Recipe | Output |
| --- | ---: |
| Fiber x10, Wood x5, Coal x1 | Biofuel x8 |

Radioisotope Fuel recipe:

| Recipe | Output |
| --- | ---: |
| Radioactive Bar x1, Refined Quartz x1 | Radioisotope Fuel x7 |

## Cables

| Cable | Recipe | Throughput |
| --- | --- | ---: |
| Copper Cable | Copper Bar x3 | 10 cables, 50 EU/tick |
| Iron Cable | Iron Bar x3 | 10 cables, 250 EU/tick |
| Iridium Cable | Iridium Bar x2, Refined Quartz x1 | 10 cables, 1,000 EU/tick |
| Energized Iridium Cable | Iridium Bar x4, Radioactive Bar x1, Battery Pack x1, Refined Quartz x2 | 10 cables, 3,000 EU/tick |

Throughput is the amount of EU a network can move each tick. If a network mixes cable tiers, the weakest cable limits the network.

## Batteries

| Battery | Recipe | Capacity |
| --- | --- | ---: |
| Basic Power Battery | Battery Pack x1, Copper Bar x4, Refined Quartz x1 | 500 EU |
| Iridium Power Battery | Battery Pack x2, Iridium Bar x2, Refined Quartz x3 | 2,000 EU |

Batteries are worth building early. They store surplus EU, cover demand spikes, and keep machines powered when generators run out of fuel or wind output drops.

Stored EU stays on the battery item when you pick it up and place it somewhere else.

## Power Conduits

Power Conduits link networks between locations, like Farm to Shed or Farm to FarmHouse.

| Item | Recipe |
| --- | --- |
| Power Conduit | Iridium Bar x1, Battery Pack x1, Refined Quartz x1 |

To link conduits:

1. Place one conduit in each location.
2. Connect each conduit to its local network.
3. Open the Power Tab and select two conduits to link them.

You can also right-click one conduit and then right-click the other. Shift + right-click cancels pairing or unlinks a conduit.

## Powered Artisan Machines

| Machine | Recipe | Power Use | Max Bonus |
| --- | --- | ---: | ---: |
| Industrial Preserves Jar | Wood x30, Coal x4, Iron Bar x4, Refined Quartz x1 | 20 EU/tick | 20% faster |
| Metal Keg | Iron Bar x6, Copper Bar x4, Refined Quartz x1 | 10 EU/tick | 20% faster |
| Hard Iridium Keg | Iridium Bar x4, Iron Bar x2, Refined Quartz x1 | 30 EU/tick | 30% faster |
| Metal Cask | Hardwood x8, Iron Bar x6, Iridium Bar x2, Refined Quartz x1 | 40 EU/tick | 50% faster aging |

Hard Iridium Keg is designed around Grapes of Ferngill's Hardwood Keg behavior. Without Grapes of Ferngill, it falls back to vanilla Keg behavior.

## 0.3 Industrial Processing Machines

### Electric Smelter

| Machine | Recipe | Power Use | Max Bonus |
| --- | --- | ---: | ---: |
| Electric Smelter | Iron Bar x8, Gold Bar x4, Refined Quartz x3, Battery Pack x1 | 40 EU/tick | 50% faster |

What it does:

- Smelts ore into bars.
- Needs a live powered network before it can start.
- If power connection is lost mid-process, the ore is returned instead of finishing for free.

Supported base recipes:

| Input | Output | Base Time |
| --- | --- | ---: |
| Copper Ore x5 | Copper Bar x1 | 30m |
| Iron Ore x5 | Iron Bar x1 | 120m |
| Gold Ore x5 | Gold Bar x1 | 300m |
| Iridium Ore x5 | Iridium Bar x1 | 480m |
| Radioactive Ore x5 | Radioactive Bar x1 | 600m |

Base bonus bar chance: 5%

### Industrial Recycler

| Machine | Recipe | Power Use | Max Bonus |
| --- | --- | ---: | ---: |
| Industrial Recycler | Iron Bar x6, Refined Quartz x4, Copper Bar x4, Coal x4 | 20 EU/tick | 35% faster |

What it does:

- Recycles supported trash into useful materials.
- Still works without power, but power speeds it up.
- Base process time is 60m.

Supported base outputs:

| Input | Output |
| --- | --- |
| Trash | Stone x2 |
| Driftwood | Wood x2 |
| Broken Glasses | Refined Quartz x1 |
| Broken CD | Refined Quartz x1 |
| Soggy Newspaper | Fiber x3 |
| Joja Cola | Coal x1 |

### Powered Dehydrator

| Machine | Recipe | Power Use | Max Bonus |
| --- | --- | ---: | ---: |
| Powered Dehydrator | Iron Bar x6, Hardwood x10, Refined Quartz x3, Battery Pack x1 | 20 EU/tick | 40% faster |

What it does:

- Processes fruit and mushrooms like the vanilla Dehydrator.
- Still works without power, but power speeds it up.

## Machine Panel And Upgrades

Open the machine panel with `Shift + right-click` on:

- `Electric Smelter`
- `Industrial Recycler`
- `Powered Dehydrator`

How to use it:

1. Hold a compatible upgrade item.
2. Open the panel with `Shift + right-click`.
3. Install the upgrade from the panel.
4. Use the same panel later if you want to remove it.

Each new industrial machine currently has one upgrade slot.

### Upgrade Recipes

| Upgrade | Recipe |
| --- | --- |
| Heating Coil | Gold Bar x3, Refined Quartz x2, Coal x6 |
| Efficiency Core | Refined Quartz x4, Battery Pack x1, Gold Bar x2 |
| Catalyst Chamber | Iridium Bar x2, Gold Bar x4, Refined Quartz x4 |
| Sorting Magnet | Iron Bar x4, Refined Quartz x2 |
| Drying Rack Array | Wood x20, Hardwood x4, Gold Bar x1 |
| Heat Regulator | Gold Bar x2, Refined Quartz x2, Coal x4 |

### Upgrade Effects

| Upgrade | Machine | Effect |
| --- | --- | --- |
| Heating Coil | Electric Smelter | Reduces smelting time and increases EU demand. |
| Efficiency Core | Smelter / Recycler / Dehydrator | Reduces EU demand. |
| Catalyst Chamber | Electric Smelter | Raises extra bar chance and raises EU demand. |
| Sorting Magnet | Industrial Recycler | Adds better metal salvage chances and raises EU demand. |
| Drying Rack Array | Powered Dehydrator | Can improve output batch size and raises EU demand. |
| Heat Regulator | Powered Dehydrator | Reduces dehydration time and raises EU demand. |

## Suggested Progression

Early:

- Steam Generator
- Copper Cable
- Basic Power Battery
- a few Industrial Preserves Jars or Metal Kegs

Midgame:

- Biofuel
- Combustion Generator
- Iron Cable
- Electric Smelter
- Industrial Recycler
- Powered Dehydrator
- first upgrade items

Later:

- Iridium Cable
- Energized Iridium Cable
- Radioisotope Generator
- Iridium Power Battery
- Power Conduits
- Wind Generators outdoors
- Metal Casks and Hard Iridium Kegs
- late-game upgrade tuning

## Troubleshooting

If a machine is not powered:

- Make sure it is connected by cable or touching another powered machine.
- Make sure the generator is fueled or producing power.
- Make sure the machine is actually processing something if you expect a speed bonus.
- Check the Power Tab for network, demand, storage, and machine status.

If `Electric Smelter` says it needs power:

- It cannot start smelting from a dead or disconnected network.
- Check live generation, fuel, batteries, and cable adjacency first.

If an upgrade does not install:

- Hold the upgrade item before opening the machine panel.
- Check that the upgrade matches that machine.
- Remember that each machine currently has one upgrade slot.

If a `Wind Generator` is offline:

- Make sure it is outdoors.

If fuel seems wasted:

- Add batteries to catch surplus EU.

If a conduit link is wrong:

- Use the Conduits tab in the Power Tab.
- Shift + right-click a conduit to unlink or cancel pairing.

## Recommended Mods

PowerGrid can be played by itself, but it feels better with:

- Grapes of Ferngill
- Automate or Event Driven Automation
- Generic Mod Config Menu
