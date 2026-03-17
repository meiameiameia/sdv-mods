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
- State deliverable convention: review-ready separate files `IridiumBattery__low.png` and `IridiumBattery__charged.png`; runtime loads those optional state files when present and otherwise falls back to `IridiumBattery.png`
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
- Perspective / view rule: use Stardew's slight top-down / 3/4-ish machine perspective with a clearly readable top plane; do not make it a flat front-on battery locker or hide the top in a token sliver
- Width / silhouette discipline: soft target `11-14 px` for the main battery body on most rows, with modest trim allowed to flare slightly wider; avoid both thin locker-strip reads and a full-width `16 px` block
- Body-mass / sturdiness rule: the battery should read as a compact premium storage unit with enough mass to feel sturdy and valuable at `1x`
- Grounding / floor-contact rule: the lowest visible base/body pixels should touch or nearly touch the bottom row; avoid giving the cabinet a levitating look
- Pixel readability requirements: premium tier should read through material and trim, not effects spam
- Edge / connection requirements: stay inside `16x32` and preserve plausible cable adjacency on left/right/up/down sides of the machine body
- Forbidden output mistakes: glowing crystal battery, neon techno battery, detail density beyond vanilla craftables

## Assumptions

- Runtime state switching now supports optional `low` and `charged` sprites with base-sprite fallback when either state file is missing.
- Tier upgrade should be obvious next to the basic battery.

## Done Criteria

- Reads as the higher-tier sibling of `BasicBattery`.
- Stays grounded in vanilla big-craftable readability.
- Top plane and lower-body grounding remain obvious at `1x` instead of drifting into a thin front-on locker.
- Low and charged variants keep the same attachment-compatible footprint and perspective.
