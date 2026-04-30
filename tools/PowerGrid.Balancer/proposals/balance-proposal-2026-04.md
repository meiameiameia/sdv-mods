# PowerGrid Balance Proposal - 2026-04

This proposal is based on the Balance Lab progression ladder and one anonymized mature Year 3 organized-production save.

The goal is not to make every late-game machine free to power. The goal is for each stage to feel like a satisfying infrastructure step instead of a resource tax, while still preserving progression and planning.

## Current Signal

Current defaults are good enough for a tiny starter setup, but fall off too sharply after that.

| Stage | Current result | Main problem |
| --- | --- | --- |
| First Powered Corner | 1 Steam Generator, 10 Coal/day, 6.2-day fuel window | Usable, but fuel is already a little hungry for a teaching setup. |
| Winter Y1 Partial Artisan Room | 4 Steam Generators, 40 Coal/day, 3.2-day fuel window | Too many generators and too much coal for a first real room. |
| Summer Y2 Mid Shed | 6 Combustion Generators, 40 Biofuel/day, 2.08-day fuel window | Combustion feels like a chore instead of a mid-game upgrade. |
| Winter Y2 First Big Shed | 12 Combustion Generators, 80 Biofuel/day, 3 cable zones | Too much clutter and maintenance for shed-scale production. |
| Mature Y3 Organized Conversion | 28 Combustion Generators, 186.67 Biofuel/day, 7 cable zones | Current tiers cannot support mature-save adoption gracefully. |
| Mature Y3 Cellar Expansion | 45 Combustion Generators, 300 Biofuel/day, 11 cable zones | Casks need premium/endgame handling, not baseline mass conversion. |

## Stage Feel Targets

These targets are the balance guardrails. They are intentionally about player feel, not just mathematical stability.

| Stage | Generator target | Fuel target | Cable target | Resource target |
| --- | ---: | ---: | ---: | --- |
| First Powered Corner | 1 generator | 10+ days from a normal stockpile | 1 zone | No hard gaps. |
| Winter Y1 Partial Artisan Room | 2-3 generators | 7-14 days | 1-2 zones | Minor iron pressure is okay; quartz should not block. |
| Summer Y2 Mid Shed | 2-4 generators | 10+ days | 1-2 zones | Mid-game resources should support one meaningful room. |
| Winter Y2 First Big Shed | 4-7 generators | 10+ days | 2-3 zones | Requires investment, but not a full grind reset. |
| Mature Y3 Organized Conversion | 4-8 late-game generators | 14+ days | 2-4 zones | Existing organized production should be convertible in phases. |
| Mature Y3 Cellar Expansion | Premium/endgame only | Not baseline | Separate cellar planning | Casks should remain special, not assumed. |

## Recommended 0.1.x Tuning

These changes fit the current feature set. They improve early and mid progression without pretending Combustion is the final tier.

### Steam Generator

Recommendation:

- Output: `50 -> 75 EU/tick`
- Coal fuel duration: `12 -> 24 ticks`
- Wood fuel duration: `4 -> 8 ticks`
- Hardwood fuel duration: `8 -> 16 ticks`

Why:

- A starter grid still uses one generator.
- Winter Y1 partial rooms drop from 4 Steam Generators to about 3.
- Coal burn becomes less punishing without making solid fuel infinite.
- Steam remains early-game and physical: it still needs fuel and space.

Risk:

- If Steam becomes too convenient, players may ignore Combustion. This is controlled by keeping Steam fuel manual/solid and lower-output than Combustion.

### Combustion Generator

Recommendation:

- Output: `120 -> 240 EU/tick`
- Biofuel duration: `18 -> 60 ticks`

Why:

- Mid shed drops from 6 generators to about 3.
- First big shed drops from 12 generators to about 6.
- Biofuel pressure becomes a planning loop instead of a constant refill chore.
- Combustion becomes the clear mid-game upgrade over Steam.

Risk:

- Combustion may become strong enough to delay the need for a late tier. That is acceptable for 0.1.x if mature-save conversion still points toward 0.2.0 late-game power.

### Biofuel Recipe

Recommendation:

- Keep the identity: plant matter + coal.
- Change craft output, not necessarily ingredients.
- Preferred first test: `Sap x30 + Coal x2 -> Biofuel x4`.

Why:

- Sap is a good thematic ingredient, but the current 1-output recipe collapses under real fuel demand.
- Batch output is easier for players to understand than tiny per-tick number changes.
- Coal remains relevant without becoming the whole bottleneck.

Risk:

- If Biofuel becomes too cheap, Combustion can replace all power tiers. The generator output and future late-game tier should carry that progression.

### Cable Throughput

Recommendation:

- Copper Cable: keep `50 EU/tick`
- Iron Cable: `150 -> 250 EU/tick`
- Iridium Cable: `500 -> 1000 EU/tick`

Why:

- Copper stays a starter/tutorial cable.
- Iron becomes useful longer for first rooms.
- Iridium can support bigger shed zones without requiring every serious production room to split immediately.

Risk:

- Higher caps reduce network-planning pressure. This is acceptable because mature conversion still exceeds one Iridium line and future late-game distribution can add more planning.

### Powered Machine Recipes

Recommendation:

- Do not lower the fantasy of metal machines.
- Reduce mass-conversion pain by lowering repeated iron/refined quartz pressure.

Candidate recipe changes:

| Machine | Current | Proposed first test |
| --- | --- | --- |
| Industrial Preserves Jar | Wood x30, Coal x10, Iron Bar x8, Refined Quartz x1 | Wood x30, Coal x6, Iron Bar x6, Refined Quartz x1 |
| Metal Keg | Iron Bar x12, Copper Bar x6, Refined Quartz x1 | Iron Bar x8, Copper Bar x6, Refined Quartz x1 |
| Hard Iridium Keg | Iridium Bar x5, Iron Bar x8, Refined Quartz x2 | Iridium Bar x5, Iron Bar x4, Refined Quartz x1 |
| Metal Cask | Hardwood x10, Iron Bar x12, Iridium Bar x3, Refined Quartz x1 | Hardwood x10, Iron Bar x8, Iridium Bar x3, Refined Quartz x1 |

Why:

- Iron is currently the biggest conversion wall, even in a mature save.
- Refined Quartz should matter because these are powered machines, but not block every mass conversion.
- Coal on Industrial Preserves Jar should be present, not dominant.

Risk:

- Lowering recipes too much could make vanilla machines obsolete too quickly. Unlock conditions and power infrastructure costs still gate this.

## Required 0.2.0 Progression Additions

These should not be solved by only buffing existing 0.1.x objects.

### Late-Game Generator Tier

PowerGrid needs a late-game generator after Combustion.

Target:

- Output: roughly `700-900 EU/tick`
- Mature organized conversion should need around `4-6` late generators, not `28` Combustion Generators.
- Fuel should be expensive/advanced, but not daily-grindy.

Theme candidates:

- Radioisotope Generator using Radioactive Ore/Bar and Power Cells.
- Geothermal Generator using late-game mining materials.
- Advanced Turbine using batteries/cells and high-tier metals.

Recommended first direction:

- Radioactive/Power Cell path.
- It uses radioactive resources as progression, but batteries/power cells as the controllable energy-storage fantasy.
- It should be safe and stylized, not dark nuclear industry.

### Late-Game Distribution Tier

PowerGrid needs a way to move `2000+ EU/tick` without forcing every mature shed into many tiny zones.

Target:

- Add one late cable or transformer-like object.
- Keep Iridium Cable useful, but let late-game networks consolidate.

Candidate:

- Energized Iridium Cable: `2000 EU/tick`
- Transformer/Power Relay: bridges high-throughput backbones to local lower-tier cable zones

Recommended first direction:

- Start with a higher-tier cable for simplicity.
- Consider transformers later if network gameplay needs more depth.

### Battery Pack / Power Cell System

The Power Cell idea fits well.

Target:

- Battery Packs become a base ingredient for Power Cells.
- Batteries can hold a Power Cell slot later.
- Power Cells act as emergency reserve or high-tier fuel/upgrade material.

Suggested first implementation:

- Add Power Cell item.
- Recipe: Battery Pack + Refined Quartz + metal tier ingredient.
- Use Power Cell as an ingredient in late generators/batteries first.
- Add actual battery slot behavior later, after the balance tool can model it.

## Cask Policy

Metal Casks should not be balanced around converting all casks in a mature cellar.

Recommendation:

- Treat powered casks as premium accelerators.
- Balance for small batches first: `8-50` Metal Casks.
- Full-cellar conversion should require late-game power and be intentionally expensive.

Why:

- The mature benchmark has 417 casks. At 40 EU/tick, full conversion is `16680 EU/tick`, which would dominate the whole mod.
- Casks are day-based and already slow. It is okay if powered casks are a special project rather than the default way to age everything.

## First Test Patch

The first patch to simulate should be:

| Setting | Current | Test value |
| --- | ---: | ---: |
| Steam Generator output | 50 | 75 |
| Combustion Generator output | 120 | 240 |
| Coal ticks | 12 | 24 |
| Wood ticks | 4 | 8 |
| Hardwood ticks | 8 | 16 |
| Biofuel ticks | 18 | 60 |
| Biofuel craft output | 1 | 4 |
| Iron Cable cap | 150 | 250 |
| Iridium Cable cap | 500 | 1000 |

Also test the softer machine recipes listed above.

Expected result:

- Early game becomes friendlier.
- Winter Y1 room becomes plausible.
- Mid-game combustion becomes satisfying.
- First big shed becomes a planned build, not a refill treadmill.
- Mature Year 3 conversion still requires a future late-game tier, which is correct.

## Decision

Recommended path:

1. Add Balance Lab support for alternate config files so this proposal can be simulated side-by-side against current defaults.
2. Create `powergrid-0.1.x-test.json` with the first test patch.
3. Run progression comparison.
4. If early/mid stages look good, apply the safe subset to the mod config defaults.
5. Plan 0.2.0 around late generator, late cable/distribution, and Power Cell mechanics.
