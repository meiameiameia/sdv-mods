using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Meiameiameia.PowerGrid.Core;

internal sealed class ConduitLink
{
    public string LocationA { get; init; } = "";
    public Vector2 TileA { get; init; }
    public string LocationB { get; init; } = "";
    public Vector2 TileB { get; init; }
}

internal sealed class ConduitManager
{
    private readonly IMonitor monitor;
    private readonly List<ConduitLink> links = new();

    // Pending pairing: first conduit placed awaits a partner
    private (string Location, Vector2 Tile)? pendingConduit;

    public ConduitManager(IMonitor monitor)
    {
        this.monitor = monitor;
    }

    public bool HasPending => pendingConduit != null;

    public void StartPairing(string locationName, Vector2 tile)
    {
        pendingConduit = (locationName, tile);
        monitor.Log($"Conduit pairing started at {locationName} ({tile.X},{tile.Y}). Place or interact with another conduit to link.", LogLevel.Info);
    }

    public bool TryCompletePairing(string locationName, Vector2 tile)
    {
        if (pendingConduit == null)
            return false;

        var pending = pendingConduit.Value;
        bool linked = LinkConduits(pending.Location, pending.Tile, locationName, tile);
        pendingConduit = null;
        return linked;
    }

    public void CancelPairing()
    {
        pendingConduit = null;
    }

    public bool RemoveConduitState(string locationName, Vector2 tile)
    {
        bool hadPending = pendingConduit is { } pending
            && pending.Location == locationName
            && pending.Tile == tile;

        if (hadPending)
            pendingConduit = null;

        int removed = links.RemoveAll(l =>
            (l.LocationA == locationName && l.TileA == tile) ||
            (l.LocationB == locationName && l.TileB == tile));

        return hadPending || removed > 0;
    }

    public ConduitLink? GetLink(string locationName, Vector2 tile)
    {
        foreach (var link in links)
        {
            if ((link.LocationA == locationName && link.TileA == tile) ||
                (link.LocationB == locationName && link.TileB == tile))
                return link;
        }
        return null;
    }

    public (string Location, Vector2 Tile)? GetPartner(string locationName, Vector2 tile)
    {
        var link = GetLink(locationName, tile);
        if (link == null)
            return null;

        if (link.LocationA == locationName && link.TileA == tile)
            return (link.LocationB, link.TileB);
        return (link.LocationA, link.TileA);
    }

    public void RemoveLinksInvolving(string locationName, Vector2 tile)
    {
        links.RemoveAll(l =>
            (l.LocationA == locationName && l.TileA == tile) ||
            (l.LocationB == locationName && l.TileB == tile));
    }

    public bool LinkConduits(string locationA, Vector2 tileA, string locationB, Vector2 tileB)
    {
        // Don't link to itself
        if (locationA == locationB && tileA == tileB)
            return false;

        var link = new ConduitLink
        {
            LocationA = locationA,
            TileA = tileA,
            LocationB = locationB,
            TileB = tileB
        };

        // Keep one link per conduit endpoint.
        RemoveLinksInvolving(locationA, tileA);
        RemoveLinksInvolving(locationB, tileB);

        links.Add(link);
        monitor.Log($"Conduit linked: {link.LocationA} ({link.TileA.X},{link.TileA.Y}) <-> {link.LocationB} ({link.TileB.X},{link.TileB.Y})", LogLevel.Info);
        return true;
    }

    public List<ConduitLink> GetAllLinks() => new(links);

    public void ImportState(List<ConduitLink>? state)
    {
        links.Clear();
        if (state != null)
            links.AddRange(state);
    }

    public int PruneToEndpoints(ISet<string> validEndpoints)
    {
        bool IsValid(string locationName, Vector2 tile)
        {
            string endpointKey = $"{locationName}|{tile.X}|{tile.Y}";
            return validEndpoints.Contains(endpointKey);
        }

        int before = links.Count;
        links.RemoveAll(link =>
            !IsValid(link.LocationA, link.TileA) ||
            !IsValid(link.LocationB, link.TileB));

        if (pendingConduit is { } pending)
        {
            string pendingKey = $"{pending.Location}|{pending.Tile.X}|{pending.Tile.Y}";
            if (!validEndpoints.Contains(pendingKey))
                pendingConduit = null;
        }

        return before - links.Count;
    }

    public List<ConduitLink> ExportState() => new(links);
}
