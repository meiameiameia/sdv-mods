# Fish Helper UI 1.2.1 Unofficial Maintenance Release

This is an unofficial community bugfix continuation of Fish Helper UI by Script Kitty / LetsTussleBoiz.

It exists to address a small set of validated bugs while the original Nexus release appears stale relative to the latest public source. Full credit for the original mod belongs to the original author.

If the original author returns and wants this page removed, transferred, or replaced by an official update, I will gladly do so.

## Scope

This release is intentionally narrow:
- validated bugfixes
- compatibility fixes
- no broad redesign
- no speculative feature work

## Included fixes

- Fixed the mines elevator being fully unlocked early on new saves.
- Fixed fish handling for entries that use `RandomItemId`.
- Fixed fish pond tag matching when `RequiredTags` is missing.
- Fixed `LOCATION_Season Here` parsing for modded fish data.
- Fixed fish pond reward processing when a produced item ID is null or blank.
- Fixed null difficulty values in modded fish data from logging warnings.

## Tested against

- Fish Helper UI
- StardewUI
- Vanilla Plus Professions
- Stardew Valley Expanded
- Cornucopia - Cooking Recipes
- The Cowboy Life Expanded

The final sandbox validation passed without Fish Helper UI warnings or exceptions. Separate Vanilla Plus Professions errors were still present and appear unrelated to Fish Helper UI.
