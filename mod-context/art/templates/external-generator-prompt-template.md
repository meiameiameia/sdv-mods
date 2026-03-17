# Asset Prompt Template

Attach references before generation when available.

Recommended attachments:

- Vanilla authority reference crop(s)
- Current repo sprite or template reference
- Optional inspiration board
- Optional `master_64.txt`

Ready-to-paste prompt:

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: [asset name]
Output filename: [filename]
Canvas: [dimensions]
Layout: [layout]
State variants: [state rules]
Animation: [yes/no and rules]

If multiple states are listed, deliver one separate labeled sprite per state for review using `[BaseName]__[state].png` filenames while keeping the same canvas, silhouette family, and attachment zones across states.

Use attached references in this priority order:
1. Vanilla Stardew Valley references are the style authority.
2. Current repo sprite references are compatibility references only. Preserve gameplay identity where useful, but do not preserve off-style rendering if it conflicts with vanilla readability or shading logic.
3. External concept art or industrial references are inspiration only.
4. `master_64.txt` is palette discipline only.

Subject brief:
[brief]

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- for machine-like `16x32` big craftables, use Stardew's slight top-down / 3/4-ish read with a clearly readable top plane; do not render the object as a flat front-on rectangle
- for machine-like `16x32` big craftables, expose enough top-face area that it still reads at `1x`; do not reduce the top to a token sliver
- for machine-like `16x32` big craftables, keep the dominant body width within vanilla-feeling proportions instead of filling the full `16` px width on most rows, but also avoid overly thin or spindly machine bodies unless explicitly intended
- for machine-like `16x32` big craftables, give the lower body enough mass that the machine reads as sturdy and grounded beside vanilla big craftables
- for machine-like `16x32` big craftables, keep the lowest visible base/body pixels touching or nearly touching the bottom row of the canvas unless floating is intentionally justified
- for PowerGrid-adjacent machine sprites, leave visually plausible cable attachment zones on the left, right, top, and bottom adjacency sides; do not let overhangs or asymmetrical bulk imply that one side cannot connect
- match vanilla Stardew Valley big-craftable or modular-tile readability as appropriate
- stay strictly inside the exact canvas

Family consistency rules:
[family rules]

Do not:
- output multiple options in one image
- add text labels
- add drop shadows outside the sprite footprint
- invent extra states not listed above
- stylize away from vanilla readability
```
