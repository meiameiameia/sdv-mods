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
- Reserve the upper area for readable silhouette markers, not dense noise.
- Use detail sparingly: one or two strong identity cues beat many tiny marks.
- Avoid making the machine look too thin or top-heavy unless the in-game role depends on that.

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
