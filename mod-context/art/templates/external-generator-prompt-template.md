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
