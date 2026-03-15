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
- State variants: none
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
- Pixel readability requirements: premium tier should read through material and trim, not effects spam
- Edge / connection requirements: stay inside `16x32`
- Forbidden output mistakes: glowing crystal battery, neon techno battery, detail density beyond vanilla craftables

## Assumptions

- The sprite remains static regardless of charge level.
- Tier upgrade should be obvious next to the basic battery.

## Done Criteria

- Reads as the higher-tier sibling of `BasicBattery`.
- Stays grounded in vanilla big-craftable readability.
- Requires no code or layout change to ship.
