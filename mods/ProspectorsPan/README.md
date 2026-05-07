# Prospector's Pan

Prospector's Pan is a lightweight Stardew Valley 1.6+ panning improvement mod.

It makes the Copper Pan easier to justify carrying by making panning spots more reliable, keeping them reachable, adding modest progression-aware bonus rewards, and showing an optional pan hint.

## Features

- More reliable panning spots.
- Reachable spot assist while holding the Copper Pan near water.
- Conservative spot cooldowns so lakes do not become nonstop farming loops.
- Original bonus rewards on top of vanilla panning rewards.
- Progression-aware reward pools.
- Optional compact pan hint.
- Draggable corner hint for UI-heavy modpacks.
- Optional Generic Mod Config Menu support.

## Balance

Prospector's Pan is designed to make panning useful, not replace mining.

The default `Standard` reward setting adds modest bonus rewards while preserving vanilla panning results. The `Vanilla` reward setting disables bonus rewards entirely. The `Generous` setting is intentionally richer for players who prefer a stronger panning loop.

## Hint controls

The default hint appears as a compact corner HUD message while holding the Copper Pan near a panning spot.

To move the corner hint:

1. Hold the Copper Pan near a panning spot.
2. Hold `Left Shift` or `Right Shift`.
3. Hold left mouse on the hint and drag it.
4. Release the mouse to save the new position.

Console command:

```text
prospectorspan_hint_reset
```

This resets the corner hint position.

## Configuration

If Generic Mod Config Menu is installed, open it from the title screen or in-game mod options.

Important options:

- `Improve Panning Spots`: enables spot tuning.
- `Reachable Spot Assist`: helps create reachable spots while holding the Copper Pan.
- `Assist Cooldown Minutes`: controls assisted spot pacing in each location.
- `Bonus Rewards`: enables original bonus rewards.
- `Reward Generosity`: choose `Vanilla`, `Standard`, or `Generous`.
- `Show Pan Hint`: enables the pan hint.
- `Hint Position`: choose compact corner hint or tiny world marker.

The mod also works without Generic Mod Config Menu by editing `config.json` after the first launch.

## Compatibility

Prospector's Pan has no hard dependency beyond SMAPI.

Generic Mod Config Menu is optional.

The mod is intended to coexist with large modpacks and UI mods. If the corner hint overlaps another UI element, drag it to another place or switch the hint to world marker mode.

## Install

1. Install SMAPI.
2. Put the `Prospector's Pan` mod folder inside your Stardew Valley `Mods` folder.
3. Launch the game through SMAPI.

The installed folder should look like:

```text
Stardew Valley/Mods/Prospector's Pan/manifest.json
```
