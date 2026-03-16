# PowerGrid Iridium Battery

## Identity

- Mod: `PowerGrid`
- Output path: `[SMAPI] PowerGrid/Assets/IridiumBattery.png`
- Output filename: `IridiumBattery.png`
- Asset family: `PowerGrid battery`
- Current shipped status: placeholder-style battery sprite exists

## Output Contract

- Canvas size: `16x32`
- Layout: single big-craftable sprite
- State variants: `low`, `charged`
- State deliverable convention: review-ready separate files `IridiumBattery__low.png` and `IridiumBattery__charged.png`; current shipped path stays `IridiumBattery.png` until runtime switching exists
- Animation: none

## In-Game Role

- What the player should immediately read this as: premium high-capacity power storage unit
- What must stay recognizable from the current gameplay identity: battery role, iridium-quality upgrade signal, same family as the basic battery

## Family Consistency Rules

- Shared silhouette rules: should clearly relate to `BasicBattery`
- Shared connection or placement rules: same vanilla big-craftable footprint and grounding logic
- Variant differentiation rules: iridium battery should look cleaner, richer, and more advanced than the basic battery without becoming sci-fi or magical

## Reference Hierarchy

1. Vanilla style authority: `Craftables.png` for big-craftable shading and value control
2. Compatibility references only: current repo `IridiumBattery.png`, current `BasicBattery.png`
3. Inspiration only: premium industrial battery housing, iridium-toned trim, contained energy hardware
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Perspective / view rule: use Stardew's slight top-down / 3/4-ish machine perspective with a readable top face; do not make it a flat front-on battery locker
- Width / silhouette discipline: soft target `10-13 px` for the main battery body on most rows, with modest trim allowed to flare slightly wider; avoid a full-width `16 px` block
- Pixel readability requirements: premium tier should read through material and trim, not effects spam
- Edge / connection requirements: stay inside `16x32` and preserve plausible cable adjacency on left/right/up/down sides of the machine body
- Forbidden output mistakes: glowing crystal battery, neon techno battery, detail density beyond vanilla craftables

## Assumptions

- The sprite remains static regardless of charge level today, but the art contract should prepare `low` and `charged` variants now.
- Tier upgrade should be obvious next to the basic battery.

## Done Criteria

- Reads as the higher-tier sibling of `BasicBattery`.
- Stays grounded in vanilla big-craftable readability.
- Low and charged variants keep the same attachment-compatible footprint and perspective.
