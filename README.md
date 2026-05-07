# Stardew Valley Mods

Stardew Valley mods by meiameiameia.

If you just want to play the mods, download the packaged versions from Nexus Mods. This GitHub repository is mainly here for source code, changelogs, and transparency.

## Released Mods

| Mod | Type | Summary | Downloads |
| --- | --- | --- | --- |
| `PowerGrid` | SMAPI mod | Electrical infrastructure for Stardew Valley: generators, batteries, cables, conduits, powered artisan machines, automation-friendly fuel handling, and a public integration API. | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/45572) |
| `ProspectorsPan` | SMAPI mod | Lightweight panning improvements: more reliable reachable spots, modest progression-aware bonus rewards, configurable hints, and optional GMCM support. | [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/45921) |
| `FishSmokerRecipe` | Content Patcher pack | Small recipe tweak for the Fish Smoker. | Source only |

## Stewarded Mods

These are community mods maintained or preserved here with clear provenance and a narrow maintenance-first posture.

| Mod | Summary |
| --- | --- |
| `MatrixFishingUI` | Stewarded maintenance copy of Matrix Fishing UI by Script Kitty / LetsTussleBoiz. See `stewarded/MatrixFishingUI` for source notes and release context. |

## Downloads

Use the Nexus links above for normal installation.

GitHub's "Download ZIP" button gives you the source code, not a ready-to-install mod package.

## Requirements

Most SMAPI mods here target:

- Stardew Valley 1.6+
- SMAPI 4.0.0+

Check each mod page or mod folder for its own install notes, compatibility notes, and optional integrations.

## For Mod Authors And Source Readers

The source is available here for reference, compatibility work, and local builds.

PowerGrid includes a small public SMAPI API for machine mods that want to register their own big craftables as powered consumers. See [`mods/PowerGrid/API.md`](mods/PowerGrid/API.md) for the interface, minimum integration example, and beginner-friendly notes.

For simple PowerGrid support, most machine mods only need to call `RegisterConsumer(...)` with their machine's qualified item ID. Advanced mods can also query whether a tile is powered and what speedup it received.

Repository layout:

```text
mods/
  FishSmokerRecipe/
  PowerGrid/
  ProspectorsPan/
stewarded/
  MatrixFishingUI/
scripts/
  build-mod.ps1
```

Build all supported SMAPI projects through the repo helper when appropriate:

```powershell
.\scripts\build-mod.ps1 -Mods PowerGrid -Bump none
```

Or build an individual project directly:

```powershell
dotnet build .\mods\ProspectorsPan\ProspectorsPan.csproj -c Release
```
