# PowerGrid Player Guide

This is a quick guide for playing with PowerGrid without having to learn everything by trial and error.

PowerGrid adds generators, batteries, cables, conduits, Biofuel, and powered artisan machines. Powered machines do not duplicate outputs or change what they make. They simply work faster when their network has enough EU.

## First Setup

Start small:

1. Place a Steam Generator.
2. Fuel it with Coal, Wood, or Hardwood.
3. Place Copper Cable next to the generator.
4. Place a powered machine next to the cable or next to another powered machine.
5. Add a Basic Power Battery when you can.
6. Open the Power Tab with `P` or `K` to inspect the network.

Machines and PowerGrid objects connect through up, down, left, and right. Diagonal tiles do not connect.

Powered machines can pass power to other powered machines touching them, so you do not need cable between every machine. Use cables like trunk lines.

## Important Rules

- Fuel generators produce their full EU while running.
- Extra EU is stored in batteries if the network has room.
- If there is no battery room, extra EU is wasted.
- Batteries help smooth demand and make generators much less wasteful.
- Wind Generators only produce power outdoors.
- Powered machines still work normally without power.
- Power is a speed bonus, not a replacement for normal machine behavior.

## Unlocks

| Bundle | Unlock Condition | Recipes |
| --- | --- | --- |
| Grid Starter | Mining 5 or know Lightning Rod | Copper Cable, Steam Generator, Basic Power Battery |
| Powered Artisan | Know Preserves Jar or Keg | Industrial Preserves Jar, Metal Keg |
| Fuel Tech | Mining 7 and know Lightning Rod | Biofuel, Iron Cable, Combustion Generator |
| Advanced Grid | Mining 9 and know Lightning Rod | Iridium Cable, Wind Generator, Iridium Power Battery, Power Conduit, Metal Cask, Hard Iridium Keg |
| High Density Grid | Mining 10, know Lightning Rod, know Solar Panel, and know Iridium Power Battery | Energized Iridium Cable, Radioisotope Generator |

If you use Generic Mod Config Menu, recipe unlock behavior can be changed in-game.

## Generators

| Generator | Output | Fuel | Notes |
| --- | ---: | --- | --- |
| Steam Generator | 75 EU/tick | Coal, Wood, Hardwood | Early generator. Good for small rooms. |
| Combustion Generator | 240 EU/tick | Biofuel | Midgame generator. Good for larger machine groups. |
| Radioisotope Generator | 900 EU/tick | Radioactive Bar | Late-game density generator for heavy machine rooms. |
| Wind Generator | 25 EU/tick base | none | Passive power, outdoors only, weather adjusted. |

Wind output changes with weather:

- Clear weather: normal output
- Rain or storm: higher output
- Snow: lower output

## Fuel

| Fuel | Used By | Notes |
| --- | --- | --- |
| Coal | Steam Generator | Strongest Steam fuel. |
| Wood | Steam Generator | Easy backup fuel. |
| Hardwood | Steam Generator | Better than Wood. |
| Biofuel | Combustion Generator | Crafted midgame fuel. |
| Radioactive Bar | Radioisotope Generator | Dense late-game fuel for very high EU output. |

Biofuel recipe:

| Recipe | Output |
| --- | ---: |
| Fiber x10, Wood x5, Coal x1 | Biofuel x8 |

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

## Power Conduits

Power Conduits link networks between locations, like Farm to Shed or Farm to FarmHouse.

To link conduits:

1. Place one conduit in each location.
2. Connect each conduit to its local network.
3. Open the Power Tab and select two conduits to link them.

You can also right-click one conduit and then right-click the other. Shift + right-click cancels pairing or unlinks a conduit.

## Powered Machines

| Machine | Recipe | Power Use | Max Bonus |
| --- | --- | ---: | ---: |
| Industrial Preserves Jar | Wood x30, Coal x4, Iron Bar x4, Refined Quartz x1 | 20 EU/tick | 20% faster |
| Metal Keg | Iron Bar x6, Copper Bar x4, Refined Quartz x1 | 10 EU/tick | 20% faster |
| Hard Iridium Keg | Iridium Bar x4, Iron Bar x2, Refined Quartz x1 | 30 EU/tick | 30% faster |
| Metal Cask | Hardwood x8, Iron Bar x6, Iridium Bar x2, Refined Quartz x1 | 40 EU/tick | 50% faster aging |

Hard Iridium Keg is designed around Grapes of Ferngill's Hardwood Keg behavior. Without Grapes of Ferngill, it falls back to vanilla Keg behavior.

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
- larger machine rooms

Later:

- Iridium Cable
- Energized Iridium Cable
- Radioisotope Generator
- Iridium Power Battery
- Power Conduits
- Wind Generators outdoors
- Metal Casks and Hard Iridium Kegs

## Troubleshooting

If a machine is not powered:

- Make sure it is connected by cable or touching another powered machine.
- Make sure the generator is fueled or producing power.
- Make sure the machine is actually processing something.
- Check the Power Tab for network, demand, storage, and machine status.

If a Wind Generator is offline:

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
