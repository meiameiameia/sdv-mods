# Reference Usage

## Authority Order

Use references in this order unless a spec says otherwise:

1. Vanilla Stardew Valley references
2. Current repo sprite files and shipped mod sprites
3. External concept art or industrial references

## Reference Meanings

### Vanilla Stardew References

Vanilla references are the style authority.

They control:

- silhouette readability,
- cluster size,
- outline treatment,
- shading logic,
- pixel density,
- visual noise budget,
- material readability at game scale.

### Current Mod Sprites

Current mod sprites are compatibility references only.

Use them to preserve:

- gameplay identity,
- rough silhouette footprint,
- material category,
- family color coding,
- player recognition of what the object is supposed to be.

Do not preserve current rendering if it conflicts with vanilla readability or vanilla shading logic.

This matters especially for placeholder art, tint-only sprites, and overly noisy or off-style rendering.

### External Concept Art / Industrial References

External references are inspiration only.

Use them for:

- motif ideas,
- hardware details,
- material cues,
- industrial plausibility.

Do not let them override vanilla readability, palette discipline, or tile-scale clarity.

## File-Specific Guidance

### `Craftables.png`

`Craftables.png` is the authority for:

- big craftables,
- machine-like objects,
- keg-like objects,
- generators,
- batteries,
- standalone relay/conduit devices.

Use it to match vanilla big-craftable silhouette, shading, grounding, and detail density.

### `Floors.png`, `Flooring.png`, `Flooring_winter.png`

These are the authority for:

- modular tile logic,
- bitmask or connection-sensitive assets,
- clean edge-to-edge continuity,
- low-noise repeated pieces.

Use them for cable sheets and any future modular connection sprite, even if the asset is technically a big craftable.

### `master_64.txt`

`master_64.txt` is palette discipline only.

Use it to keep:

- saturation under control,
- value ramps readable,
- hue shifts believable,
- colors compatible with vanilla-feeling pixel art.

Do not treat it as a mandatory exact palette map and do not let it override vanilla shading logic from the actual game sprites.

## Repo Reality Notes

- `PowerGrid` cable sheets are currently functional placeholder-style assets. Preserve the connection logic and footprint, not the crude rendering.
- `PowerGrid` machine sprites preserve gameplay categories but should be reinterpreted through vanilla big-craftable styling when upgraded.
- `Metal Kegs/assets/templates/*.png` are useful compatibility references because they show the template lineage of the two keg-family machines.
- Curated in-repo reference-only copies live under `mod-context/art/references/compatibility/`.
- Vanilla style-authority crops are intentionally not fully dumped in-repo; use `mod-context/art/references/vanilla/minimum-crop-set.md` to prepare the minimum local crop bundle for each generation run.
