# Style Notes

## Baseline

Target vanilla Stardew Valley readability first.

The sprite should look like it belongs beside vanilla big craftables at normal gameplay zoom, not like promotional art or high-detail concept art.

## General Rules

- Keep silhouettes simple and readable at `1x`.
- Prefer 1-pixel decisions over soft gradients.
- Use top-left lighting and grounded lower-edge shadows consistent with vanilla craftables.
- Avoid anti-aliasing, painterly texture, glossy airbrushed highlights, and subpixel blur.
- Keep color saturation moderate; avoid neon and pure RGB primaries unless the asset specifically needs a tiny accent.
- Preserve transparent background and stay within the exact canvas.

## Big-Craftable Notes

For `16x32` machine-like sprites:

- The object should feel grounded on the bottom rows.
- Use Stardew's slight top-down / 3/4-ish machine read, not a flat front-on elevation.
- The top face should usually be visibly readable, even if it is only a thin `1-3 px` band.
- Reserve the upper area for readable silhouette markers, not dense noise.
- Use detail sparingly: one or two strong identity cues beat many tiny marks.
- Avoid making the machine look too thin or top-heavy unless the in-game role depends on that.

## Machine Perspective And Width

For machine-like `16x32` big craftables, derive perspective and width from the vanilla authority crops in `craftables-machine-authority-a/b/c.png` and the keg lineage references.

- Prefer a slight top-down / 3/4-ish read where the front face and top face are both legible.
- Avoid flat front-on boxes that read like a wall panel pasted onto the canvas.
- Keep the dominant machine body narrower than the full `16 px` canvas on most rows so side breathing room remains visible.
- For compact boxy machines such as generators, batteries, and relay devices, use a soft target of roughly `10-13 px` for the main body on most rows.
- For keg-family bodies, use a soft target of roughly `11-14 px` for the main barrel/body mass on most rows.
- Small accents may flare slightly wider than the main body, but the sprite should not read as a full-width `16 px` slab.
- Width discipline is a soft target, not a rigid per-row hard cap. Preserve the family silhouette first, but default back toward vanilla width if the generation drifts too broad.

## PowerGrid Cable Attachment Compatibility

For PowerGrid-adjacent machine sprites that sit beside cables:

- The silhouette should support believable cable adjacency from left, right, up, and down.
- Leave visually plausible attachment zones or clear edge transitions on all four sides, even if no explicit socket is drawn.
- Do not use one-sided overhangs, side skirts, or bulky piping that makes a neighboring cable look impossible on one side.
- Top-down perspective should still leave the top edge readable enough that an upward cable neighbor does not feel contradicted.
- Keep attachment plausibility stable across all state variants of the same machine family.

## State Variant Contract

When a sprite family needs multiple machine states:

- Keep the same canvas, overall footprint, perspective, and cable-attachment plausibility across every state.
- Change only the readable state cues: indicator glow, vent warmth, charge window, linked signal cue, rotor posture, or similar controlled differences.
- Do not make one state dramatically wider, taller, or noisier than the others.
- Deliver review-ready state files as separate labeled sprites using `BaseName__state.png`.
- The currently shipped runtime filename remains the primary/default path until gameplay code explicitly adopts state switching later.

## Modular Cable Notes

For connection-sensitive cable sheets:

- Prioritize edge alignment and continuity over decorative detail.
- The shared connection geometry must remain stable across the family.
- Material differences should come from color and small accent changes, not changing the tile connection footprint.
- The central anchor/junction should stay readable even when multiple adjacent cable pieces are present.

## Family Consistency

When a family has multiple tiers or variants:

- Keep the same overall footprint.
- Keep connection logic identical where the code expects identical geometry.
- Escalate tier identity through material, trim, and accent choices before changing silhouette.

Examples:

- `CopperCable` / `IronCable` / `IridiumCable` should read as the same cable family with different material tiers.
- `BasicBattery` / `IridiumBattery` should read as related storage units with a clear quality step.
- `MetalKeg` / `HardIridiumKeg` should read as related keg-family machines with distinct quality/material signals.
