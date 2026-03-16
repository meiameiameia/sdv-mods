# Reference Assets

This folder stores reference-only support materials for external sprite generation runs.

It is not a gameplay asset location.

Do not point runtime code at files in this folder.

## Git Tracking Policy

Tracked in Git:

- [vanilla/README.md](C:/Users/darth/Projects/sdv-mod/mod-context/art/references/vanilla/README.md)
- [vanilla/minimum-crop-set.md](C:/Users/darth/Projects/sdv-mod/mod-context/art/references/vanilla/minimum-crop-set.md)
- [reference-map.md](C:/Users/darth/Projects/sdv-mod/mod-context/art/reference-map.md)

Local only and ignored by Git:

- raw compatibility reference PNGs under `references/compatibility/`
- raw vanilla authority crop PNGs under `references/vanilla/`

These files are useful for local external-generation runs, but they are not runtime assets and are too easy to bloat or drift if treated as committed source.

## Still External

Vanilla source material still originates from local game files, and palette support still depends on:

- local `Craftables` / `Floors` / `Flooring` assets used to rebuild the crop set
- `master_64.txt` palette discipline reference

Use [minimum-crop-set.md](C:/Users/darth/Projects/sdv-mod/mod-context/art/references/vanilla/minimum-crop-set.md) when rebuilding or refreshing vanilla crops.
