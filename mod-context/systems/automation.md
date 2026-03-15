# Automation Systems

## Runtime Automation Layer
- **Automate**: chest-connected machine processing backbone.
- **Better Junimos**: automated field labor and farm task delegation.
- **WorkbenchHelper**: expanded chest reach for crafting flows.
- **Chests Anywhere**: remote inventory access, changes machine refill logistics.
- **Integrated Minecarts**: transport convenience that affects production routing.
- **Tractor Mod / AutoGate**: movement throughput improvements around production zones.

## Interaction With Your Mods

### PowerGrid
- Current behavior:
  - accelerates processing by reducing `MinutesUntilReady`.
  - does not replace machine IO or crafting logic.
- Net effect:
  - complements Automate/Junimo systems instead of competing with them.

### Metal Kegs
- Current behavior:
  - registered as standard big-craftable machines via `Data/Machines`.
- Net effect:
  - expected to plug into Automate flows like normal kegs.

### FishSmoker Recipe
- Current behavior:
  - recipe rebalance only.
- Net effect:
  - influences when players enter automated fish-smoking loops.

## Integration Opportunities
- Power-aware automation policies:
  - let automation prioritize powered machines first.
- Operational visibility:
  - expose powered/unpowered states in shared overlays/tooltips.
- Balance alignment:
  - validate throughput and recipe pacing once automation + speedup are combined.
