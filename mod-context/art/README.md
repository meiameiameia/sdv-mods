# Art Workflow

This folder is the repo-owned source of truth for sprite requirements and external generation prompts.

Use it when a repo-owned mod needs new or replacement sprite art.

## Scope

This workflow is for:

- sprite requirements,
- reference hierarchy,
- external AI image-generation prompts,
- locked technical specs for shipped asset files.

This workflow is not for:

- generating final PNGs inside this repo,
- broad concept-art exploration,
- replacing gameplay code or runtime testing.

## Current Audited Sprite Surface

Repo-owned mods with current sprite targets:

- `PowerGrid`
  - `CopperCable.png`
  - `IronCable.png`
  - `IridiumCable.png`
  - `SteamGenerator.png`
  - `WindGenerator.png`
  - `BasicBattery.png`
  - `IridiumBattery.png`
  - `PowerConduit.png`
- `Metal Kegs`
  - `MetalKeg.png`
  - `HardIridiumKeg.png`

Repo-owned files that are reference-only, not generation targets:

- `Metal Kegs/assets/templates/VanillaKeg.png`
- `Metal Kegs/assets/templates/HardwoodKeg.png`

Repo-owned mods with no current sprite target:

- `FishSmoker Recipe`
- `Farm Terminal` MVP

No spec or prompt should be created for a new asset family unless the repo actually needs that shipped file.

## Workflow

1. Confirm the asset is a real repo need and identify the exact output filename and destination path.
2. Create or update one locked spec in `specs/`.
3. Create or update one matching ready-to-paste prompt in `prompts/`.
4. Generate exactly one sprite at a time in the external image model.
5. Clean up and final-pixel the output outside this repo if needed, then save the final PNG into the real mod asset path.
6. Runtime-test the sprite in game after the PNG exists.

## One-Sprite Rule

Prompts in `prompts/` are intentionally split one sprite at a time.

Do not batch multiple unrelated sprite targets into one generation request if the final output filenames differ.

## Reference Hierarchy

Use the hierarchy in [reference-usage.md](C:/Users/darth/Projects/sdv-mod/mod-context/art/reference-usage.md) for every spec and prompt.

In short:

1. Vanilla Stardew references are style authority.
2. Current mod sprites are compatibility references only.
3. External concept art or industrial references are inspiration only.

## Templates

- [sprite-spec-template.md](C:/Users/darth/Projects/sdv-mod/mod-context/art/templates/sprite-spec-template.md)
- [external-generator-prompt-template.md](C:/Users/darth/Projects/sdv-mod/mod-context/art/templates/external-generator-prompt-template.md)

## Reference Assets

Reference-only workflow docs live under:

- [references/README.md](C:/Users/darth/Projects/sdv-mod/mod-context/art/references/README.md)

The workflow docs and mapping files belong in Git.

Raw reference images under `mod-context/art/references/` are local-only support files and should remain ignored by Git unless a later workflow change makes a specific tracked image materially necessary.

## Current Specs

See `specs/` for locked asset requirements and `prompts/` for matching ready-to-paste external prompts.

For exact prompt + upload file lists per sprite target, use:

- [reference-map.md](C:/Users/darth/Projects/sdv-mod/mod-context/art/reference-map.md)
