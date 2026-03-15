using Microsoft.Xna.Framework;

namespace Darth.PowerGrid.Core;

internal sealed class PowerNode
{
    public PowerNodeType NodeType { get; init; }
    public string LocationName { get; init; } = "";
    public Vector2 Tile { get; init; }
    public string ItemId { get; init; } = "";

    // Cable-specific
    public CableTier CableTier { get; init; }
    public int ThroughputCap { get; init; }
    public int ConnectionMask { get; set; } // 0-15 bitmask for drawing connected cables

    // Generator-specific
    public int GenerationPerTick { get; init; }
    public bool RequiresFuel { get; init; }

    // Battery-specific
    public int Capacity { get; init; }

    // Consumer-specific
    public int DemandPerTick { get; init; }
    public float MaxSpeedupFraction { get; init; }
    public int Priority { get; init; }

    // Conduit-specific (links to another location)
    public string? LinkedLocationName { get; set; }
    public Vector2? LinkedTile { get; set; }

    public string UniqueKey => PowerConstants.MakeNodeKey(LocationName, Tile, ItemId);
}
