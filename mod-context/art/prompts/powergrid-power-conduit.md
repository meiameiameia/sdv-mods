# PowerGrid Power Conduit Prompt

Attach these references if available before generation:

- Vanilla machine-like big craftable crops from `Craftables.png`
- Current repo sprite: `[SMAPI] PowerGrid/Assets/PowerConduit.png`
- Optional relay, coupler, or signal-device references
- `master_64.txt`

```text
Generate exactly one final game-ready pixel-art sprite for Stardew Valley.

Asset name: PowerGrid Power Conduit
Output filename: PowerConduit.png
Canvas: 16x32 pixels
Layout: single big-craftable sprite
State variants: none
Animation: none

Use attached references in this priority order:
1. Vanilla Stardew Valley Craftables references are the style authority.
2. The current repo PowerConduit sprite is a compatibility reference only. Preserve gameplay identity, but do not preserve placeholder-style rendering if it conflicts with vanilla readability.
3. External relay or coupler references are inspiration only.
4. master_64.txt is palette discipline only.

Subject brief:
Create a compact relay device that reads as a special network-link object rather than a generic machine. It should suggest pairing, routing, or signal transfer through one strong relay-style visual cue while still fitting the handcrafted Stardew machine world.

Technical requirements:
- transparent background
- no mockup, no scene, no UI frame
- no anti-aliasing or blurred edges
- clean readable silhouette at gameplay scale
- grounded lower edge
- stay strictly inside the exact canvas

Family consistency rules:
- belongs to the same PowerGrid hardware family as generators and batteries
- should feel more specialized and link-oriented than a storage or generation device
- remain readable and grounded, not magical or portal-like

Do not:
- output multiple options
- add portal effects, floating runes, lightning arcs, or animation cues
- make it too tall or antenna-heavy for 16x32
- let it read like a battery box
- drift away from vanilla big-craftable style
```
