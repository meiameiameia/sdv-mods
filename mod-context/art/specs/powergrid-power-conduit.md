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
- State variants: `unpaired`, `linked`
- State deliverable convention: review-ready separate files `PowerConduit__unpaired.png` and `PowerConduit__linked.png`; runtime loads those optional state files when present and otherwise falls back to `PowerConduit.png`
- Animation: none

## In-Game Role

- What the player should immediately read this as: a compact relay / link device for power routing
- What must stay recognizable from the current gameplay identity: special conduit/relay role, smaller and more device-like than a battery or generator, not a battery box, sign, antenna forest, or decorative prop

## Family Consistency Rules

- Shared silhouette rules: belongs to the PowerGrid hardware family but can be slightly more specialized
- Shared connection or placement rules: grounded `16x32` big-craftable footprint with believable cable adjacency on all four sides
- Variant differentiation rules: should feel like a relay or paired-link device, not just another storage box

## Reference Hierarchy

1. Vanilla style authority: `Craftables.png` for big-craftable form and shading
2. Compatibility references only: current repo `PowerConduit.png`, current PowerGrid sibling machine sprites
3. Inspiration only: relay couplers, compact signal devices, industrial link hardware
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Perspective / view rule: use Stardew's slight top-down / 3/4-ish machine perspective with a clearly readable top cap or top plane; do not render it as a flat front-on panel or sign
- Width / silhouette discipline: soft target `11-14 px` for the main conduit body on most rows, with specialized relay details allowed to extend slightly; avoid both spindly center-column reads and a full-width `16 px` block
- Body-mass / sturdiness rule: the main body must read as a compact grounded device, not a thin post, pedestal, or oversized tower
- Grounding / floor-contact rule: the lowest visible body/base pixels should touch or nearly touch the bottom row; avoid giving the conduit a hovering-sign look
- Pixel readability requirements: one clear relay/link cue such as paired coupler, node, or signal cap must read immediately, while the device body still stays compact
- Edge / connection requirements: stay within `16x32` and preserve plausible cable adjacency on left/right/up/down sides of the device; no one-sided bulk that visually blocks a cable neighbor
- Forbidden output mistakes: magical portal device, oversized antenna forest, battery-box read, pedestal device, sign-like panel, visual effects that imply animation not present in code

## Assumptions

- Runtime state switching now supports optional `unpaired` and `linked` sprites with base-sprite fallback when either state file is missing.
- The conduit should still feel grounded in Stardew’s crafted-machine language.

## Done Criteria

- Reads as a compact link device at vanilla scale.
- Stands apart from batteries and generators without leaving the same visual world.
- Four-side cable plausibility remains obvious instead of being contradicted by the silhouette.
- Unpaired and linked variants keep the same attachment-compatible footprint and perspective.
