# Metal Cask

## Identity

- Mod: `Metal Kegs`
- Output path: `[SMAPI] Metal Kegs/assets/MetalCask.png`
- Output filename: `MetalCask.png`
- Asset family: `hybrid industrial cask-family machine`
- Current shipped status: gameplay/runtime identity exists; no finalized generated sprite is locked yet

## Output Contract

- Canvas size: `16x32`
- Layout: single big-craftable sprite
- State variants: none
- State deliverable convention: single-state only for now; do not prepare powered/unpowered companion deliverables yet
- Animation: none

## In-Game Role

- What the player should immediately read this as: a closed industrial aging cask suitable for wine, cheese, and other sealed aging goods
- What must stay recognizable from the current gameplay identity: cask-family aging role, heavier reinforced construction, usable outside the vanilla-cellar-only feel without becoming an open vat, bowl, or generic machine box

## Family Consistency Rules

- Shared silhouette rules: preserve vanilla cask lineage first, then add reinforced machine-like body mass and metal construction cues
- Shared connection or placement rules: grounded vanilla big-craftable footprint with plausible cable adjacency around the body
- Variant differentiation rules: should read as more industrial and reinforced than a vanilla cask, but not as a sibling to the keg family before it reads as a cask

## Reference Hierarchy

1. Vanilla style authority: `Craftables.png` cask and other machine-like big craftables
2. Compatibility references only: current repo `MetalKeg.png`, current repo `HardIridiumKeg.png`, and any local vanilla cask crop prepared for the run
3. Inspiration only: reinforced aging vessels, steel-banded cellar casks, industrial food-safe vats
4. Palette discipline only: `master_64.txt`

## Technical Constraints

- Transparent background: yes
- No anti-aliasing: yes
- Perspective / view rule: preserve Stardew's slight top-down / 3/4-ish cask perspective with a clearly readable top ellipse or lid plane; do not render it flat front-on
- Width / silhouette discipline: soft target `12-14 px` for the main cask/body mass on most rows; avoid both thin/spindly cask reads and a square full-width `16 px` slab
- Body-mass / sturdiness rule: the lower body should read as a sturdy reinforced aging vessel with enough mass to feel grounded and durable at `1x`
- Grounding / floor-contact rule: the lowest visible body/base pixels should touch or nearly touch the bottom row; avoid empty air beneath the cask
- Pixel readability requirements: must read as cask-family first, with industrial reinforcement second, and should show a clearly enclosed storage volume rather than an open container
- Edge / connection requirements: stay within `16x32` and preserve plausible left/right/up/down cable adjacency around the body without making the sprite feel like a cable machine first
- Forbidden output mistakes: open-top basin, bowl, cauldron, chalice, pedestal-hoop silhouette, visible liquid surface, front-on barrel, generic machine cabinet, sci-fi fermenter, over-detailed steel drum, magical glow treatment

## Assumptions

- `Metal Cask` should be treated as a hybrid family: cask lineage first, machine-like reinforcement second.
- Runtime state switching is not in scope for this asset yet, so the sprite remains single-state for now.

## Done Criteria

- Reads clearly as a closed industrial aging cask at vanilla gameplay scale.
- Feels grounded beside vanilla big craftables instead of floating, thin, front-on, or bowl-like.
- Keeps cask lineage while still looking like a meaningful reinforced upgrade path inside `Metal Kegs`.
