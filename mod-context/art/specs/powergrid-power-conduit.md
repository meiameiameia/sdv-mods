# PowerGrid Power Conduit

## Identity

- Mod: `PowerGrid`
- Output path: `[SMAPI] PowerGrid/Assets/PowerConduit.png`
- Output filename: `PowerConduit.png`
- Asset family: `PowerGrid relay / link device`
- Current shipped status: placeholder-style conduit sprite exists

## Output Contract

- Canvas size: `16x32`
- Layout: single big-craftable sprite
- State variants: none
- Animation: none

## In-Game Role

- What the player should immediately read this as: special relay device that links two power networks across locations
- What must stay recognizable from the current gameplay identity: distinct conduit/relay role, more special than a normal battery or generator, not a generic machine

## Family Consistency Rules

- Shared silhouette rules: belongs to the PowerGrid hardware family but can be slightly more specialized
- Shared connection or placement rules: grounded `16x32` big-craftable footprint
- Variant differentiation rules: should feel like a relay or paired-link device, not just another storage box

## Reference Hierarchy

1. Vanilla style authority: `Craftables.png` for big-craftable form and shading
2. Compatibility references only: current repo `PowerConduit.png`, current PowerGrid sibling machine sprites
3. Inspiration only: relay pylons, signal couplers, compact industrial link hardware
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Pixel readability requirements: one clear relay/link cue such as paired antenna, coupler ring, or signal node
- Edge / connection requirements: stay within `16x32`
- Forbidden output mistakes: magical portal device, oversized antenna forest, visual effects that imply animation not present in code

## Assumptions

- Link state is not rendered through alternate sprites today.
- The conduit should still feel grounded in Stardew’s crafted-machine language.

## Done Criteria

- Reads as a special link device at vanilla scale.
- Stands apart from batteries and generators without leaving the same visual world.
- Drops into the current file path unchanged.
