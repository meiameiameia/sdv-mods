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
- Perspective / view rule: use Stardew's slight top-down / 3/4-ish machine perspective with a clearly readable top cap or top plane; do not render it as a flat front-on panel or sign
- Width / silhouette discipline: soft target `11-14 px` for the main conduit body on most rows, with specialized relay details allowed to extend slightly; avoid both spindly center-column reads and a full-width `16 px` block
- Body-mass / sturdiness rule: the main body should read as a compact relay device with enough lower-body mass to feel grounded and buildable at `1x`
- Grounding / floor-contact rule: the lowest visible body/base pixels should touch or nearly touch the bottom row; avoid giving the conduit a hovering-sign look
- Pixel readability requirements: one clear relay/link cue such as paired antenna, coupler ring, or signal node
- Edge / connection requirements: stay within `16x32` and preserve plausible cable adjacency on left/right/up/down sides of the device
- Forbidden output mistakes: magical portal device, oversized antenna forest, visual effects that imply animation not present in code

## Assumptions

- Runtime state switching now supports optional `unpaired` and `linked` sprites with base-sprite fallback when either state file is missing.
- The conduit should still feel grounded in Stardew’s crafted-machine language.

## Done Criteria

- Reads as a special link device at vanilla scale.
- Stands apart from batteries and generators without leaving the same visual world.
- Top cap and grounded lower body remain readable so the device does not drift into a thin front-on sign.
- Unpaired and linked variants keep the same attachment-compatible footprint and perspective.
