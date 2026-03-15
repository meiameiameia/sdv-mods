# PowerGrid Basic Battery

## Identity

- Mod: `PowerGrid`
- Output path: `[SMAPI] PowerGrid/Assets/BasicBattery.png`
- Output filename: `BasicBattery.png`
- Asset family: `PowerGrid battery`
- Current shipped status: placeholder-style battery sprite exists

## Output Contract

- Canvas size: `16x32`
- Layout: single big-craftable sprite
- State variants: none
- Animation: none

## In-Game Role

- What the player should immediately read this as: small entry-tier power storage unit
- What must stay recognizable from the current gameplay identity: battery silhouette, stored-energy feel, lower-tier green/copper identity

## Family Consistency Rules

- Shared silhouette rules: should read as a sibling to `IridiumBattery`
- Shared connection or placement rules: same grounded big-craftable scale and overall family footprint
- Variant differentiation rules: basic battery should feel cheaper, simpler, and less premium than the iridium battery

## Reference Hierarchy

1. Vanilla style authority: `Craftables.png` for big-craftable shading and readable battery-like machine treatment
2. Compatibility references only: current repo `BasicBattery.png`, current `IridiumBattery.png`
3. Inspiration only: compact industrial battery cabinets, terminals, charge indicators
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Pixel readability requirements: charge-storage identity should read in one glance
- Edge / connection requirements: stay within `16x32`
- Forbidden output mistakes: modern photoreal battery decals, huge glowing indicators, clutter that overwhelms the silhouette

## Assumptions

- The battery has no dynamic charged/uncharged art state in code today.
- The visual quality step between basic and iridium should be obvious without changing footprint.

## Done Criteria

- Reads as a battery at vanilla gameplay scale.
- Pairs cleanly with `IridiumBattery` as the lower-tier member.
- Can replace the current sprite without extra runtime handling.
