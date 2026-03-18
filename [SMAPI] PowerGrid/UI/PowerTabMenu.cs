using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using Darth.PowerGrid.Core;
using DarthMods.API.Power;

namespace Darth.PowerGrid.UI;

internal sealed class PowerTabMenu : IClickableMenu
{
    private readonly PowerManager powerMgr;
    private readonly PowerQueryService powerQuery;
    private readonly BatteryStateManager batteryState;
    private readonly FuelManager fuelMgr;
    private readonly ConduitManager conduitMgr;

    private int scrollOffset;
    private readonly int maxVisibleLines = 20;
    private readonly List<string> lines = new();
    private readonly Dictionary<int, string> conduitLineToKey = new();
    private readonly Dictionary<string, ConduitEntry> conduitsByKey = new();
    private string? selectedConduitKey;

    private sealed class ConduitEntry
    {
        public string LocationName { get; init; } = "";
        public Vector2 Tile { get; init; }
    }

    public PowerTabMenu(PowerManager powerMgr, PowerQueryService powerQuery, BatteryStateManager batteryState, FuelManager fuelMgr, ConduitManager conduitMgr)
        : base(
            (int)(Game1.uiViewport.Width * 0.08f),
            (int)(Game1.uiViewport.Height * 0.05f),
            (int)(Game1.uiViewport.Width * 0.84f),
            (int)(Game1.uiViewport.Height * 0.9f),
            showUpperRightCloseButton: true)
    {
        this.powerMgr = powerMgr;
        this.powerQuery = powerQuery;
        this.batteryState = batteryState;
        this.fuelMgr = fuelMgr;
        this.conduitMgr = conduitMgr;
        BuildLines();
    }

    private int LineHeight => (int)(Game1.smallFont.LineSpacing * 1.1f);
    private int TextLeft => xPositionOnScreen + 32;
    private int TextTop => yPositionOnScreen + 32;

    private static string MakeConduitKey(string locationName, Vector2 tile)
    {
        return $"{locationName}|{tile.X}|{tile.Y}";
    }

    private static List<GameLocation> CollectLocations()
    {
        var result = new List<GameLocation>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (GameLocation location in Game1.locations)
        {
            string key = location.NameOrUniqueName;
            if (seen.Add(key))
                result.Add(location);
        }

        return result;
    }

    private static string FormatLocationScope(IReadOnlyList<string> locationNames)
    {
        if (locationNames.Count == 0)
            return "No locations";

        List<string> ordered = locationNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return ordered.Count switch
        {
            0 => "No locations",
            1 => ordered[0],
            2 => $"{ordered[0]} + {ordered[1]}",
            _ => $"{ordered[0]} + {ordered[1]} +{ordered.Count - 2} more"
        };
    }

    private static IEnumerable<string> FormatConsumerLines(PowerConsumerSnapshot consumer)
    {
        string headerStatus;
        string detailLine;

        if (consumer.ProgressMode == "days" && !string.IsNullOrWhiteSpace(consumer.ProgressText))
        {
            headerStatus = consumer.IsPowered ? "Powered / Aging" : "Aging / Unpowered";
            detailLine = $"      Power EU {consumer.EUAllocated}/{consumer.DemandPerTick} | Speed {consumer.SpeedupFraction:P0} | {consumer.ProgressText}";
        }
        else if (consumer.IsProcessing)
        {
            headerStatus = consumer.IsPowered ? "Powered / Active" : "Waiting For Power";
            detailLine = $"      Power EU {consumer.EUAllocated}/{consumer.DemandPerTick} | Speed {consumer.SpeedupFraction:P0} | Accel {consumer.MinutesAccelerated}m | ~{consumer.MinutesRemaining}m left";
        }
        else if (consumer.IsPowered)
        {
            headerStatus = "Powered / Ready";
            detailLine = $"      Power EU {consumer.EUAllocated}/{consumer.DemandPerTick} | Speed {consumer.SpeedupFraction:P0} | Ready for work";
        }
        else
        {
            headerStatus = "Idle / Unpowered";
            detailLine = $"      Power EU {consumer.EUAllocated}/{consumer.DemandPerTick} | No allocation on latest tick";
        }

        yield return $"    Cons ({consumer.TileX},{consumer.TileY}) {consumer.DisplayName}";
        yield return $"      {headerStatus} | Net #{consumer.NetworkId} | {consumer.LocationName} | Pri {consumer.Priority}";
        yield return detailLine;
    }

    private void BuildLines()
    {
        lines.Clear();
        conduitLineToKey.Clear();
        conduitsByKey.Clear();

        var locations = CollectLocations();
        IReadOnlyList<PowerNetworkSnapshot> networkSnapshots = powerQuery.GetNetworkSnapshots();
        IReadOnlyList<PowerConsumerSnapshot> consumerSnapshots = powerQuery.GetConsumerSnapshots();
        IReadOnlyList<PowerGeneratorSnapshot> generatorSnapshots = powerQuery.GetGeneratorSnapshots();
        IReadOnlyList<PowerBatterySnapshot> batterySnapshots = powerQuery.GetBatterySnapshots();

        int totalNetworks = networkSnapshots.Count;
        int totalGenerators = networkSnapshots.Sum(net => net.GeneratorCount);
        int totalCables = networkSnapshots.Sum(net => net.CableCount);
        int totalBatteries = networkSnapshots.Sum(net => net.BatteryCount);
        int totalConsumers = networkSnapshots.Sum(net => net.ConsumerCount);
        int totalConduits = networkSnapshots.Sum(net => net.ConduitCount);
        int totalGeneration = networkSnapshots.Sum(net => net.TotalGenerationPerTick);
        int totalDemand = networkSnapshots.Sum(net => net.TotalDemandPerTick);
        int totalCapacity = networkSnapshots.Sum(net => net.TotalBatteryCapacity);
        int activeLocations = networkSnapshots
            .SelectMany(net => net.LocationNames)
            .Distinct(StringComparer.Ordinal)
            .Count();

        foreach (GameLocation location in locations)
        {
            foreach (PowerNetwork net in powerMgr.GetNetworks(location))
            {
                foreach (PowerNode conduit in net.Conduits)
                {
                    string conduitKey = MakeConduitKey(conduit.LocationName, conduit.Tile);
                    if (!conduitsByKey.ContainsKey(conduitKey))
                    {
                        conduitsByKey[conduitKey] = new ConduitEntry
                        {
                            LocationName = conduit.LocationName,
                            Tile = conduit.Tile
                        };
                    }
                }
            }
        }

        if (selectedConduitKey != null && !conduitsByKey.ContainsKey(selectedConduitKey))
            selectedConduitKey = null;

        lines.Add("=== Power Tab ===");
        lines.Add("Click conduit line A, then conduit line B to link. Press Delete to unlink selected. Press R to refresh.");
        lines.Add("");
        lines.Add($"Locations scanned: {locations.Count}  |  Active locations: {activeLocations}  |  Networks: {totalNetworks}");
        lines.Add($"Generators: {totalGenerators}  |  Cables: {totalCables}  |  Batteries: {totalBatteries}  |  Consumers: {totalConsumers}  |  Conduits: {totalConduits}");
        lines.Add($"Generation: {totalGeneration} EU/tick  |  Demand: {totalDemand} EU/tick  |  Battery capacity: {totalCapacity} EU");
        lines.Add($"Battery charge tracked: {batteryState.TotalStoredEU()} EU");
        lines.Add("");

        lines.Add("=== Networks ===");
        if (networkSnapshots.Count == 0)
        {
            lines.Add("No active networks detected.");
            lines.Add("");
        }
        else
        {
            foreach (PowerNetworkSnapshot net in networkSnapshots.OrderBy(net => net.NetworkId))
            {
                string throughput = net.CableThroughputCap <= 0 ? "unlimited" : $"{net.CableThroughputCap} EU";
                lines.Add($"--- Net #{net.NetworkId}: {FormatLocationScope(net.LocationNames)} ---");
                lines.Add($"  Scope: {string.Join(", ", net.LocationNames.OrderBy(name => name, StringComparer.Ordinal))}");
                lines.Add($"  Nodes: {net.GeneratorCount} gen | {net.CableCount} cable | {net.BatteryCount} batt | {net.ConsumerCount} cons | {net.ConduitCount} conduit");
                lines.Add($"  Flow: gen {net.LastTickGenerated} | used {net.LastTickConsumed} | batt out {net.LastTickFromBatteries} | batt in {net.LastTickStoredInBatteries} | throughput {throughput}");

                foreach (string locationName in net.LocationNames.OrderBy(name => name, StringComparer.Ordinal))
                {
                    List<PowerGeneratorSnapshot> networkGenerators = generatorSnapshots
                        .Where(gen => gen.NetworkId == net.NetworkId && string.Equals(gen.LocationName, locationName, StringComparison.Ordinal))
                        .OrderBy(gen => gen.TileY)
                        .ThenBy(gen => gen.TileX)
                        .ToList();

                    List<PowerConsumerSnapshot> networkConsumers = consumerSnapshots
                        .Where(c => c.NetworkId == net.NetworkId && string.Equals(c.LocationName, locationName, StringComparison.Ordinal))
                        .OrderBy(c => c.TileY)
                        .ThenBy(c => c.TileX)
                        .ToList();

                    List<PowerBatterySnapshot> networkBatteries = batterySnapshots
                        .Where(b => b.NetworkId == net.NetworkId && string.Equals(b.LocationName, locationName, StringComparison.Ordinal))
                        .OrderBy(b => b.TileY)
                        .ThenBy(b => b.TileX)
                        .ToList();

                    if (networkGenerators.Count == 0 && networkConsumers.Count == 0 && networkBatteries.Count == 0)
                        continue;

                    lines.Add($"  [{locationName}]");

                    foreach (PowerGeneratorSnapshot gen in networkGenerators)
                    {
                        string fuelInfo = gen.RequiresFuel
                            ? $"fuel {gen.FuelTicksRemaining}"
                            : "passive";
                        lines.Add($"    Gen ({gen.TileX},{gen.TileY}) {gen.DisplayName} | {gen.GenerationPerTick} EU/tick | {fuelInfo}");
                    }

                    foreach (PowerConsumerSnapshot consumer in networkConsumers)
                    {
                        foreach (string consumerLine in FormatConsumerLines(consumer))
                            lines.Add(consumerLine);
                    }

                    foreach (PowerBatterySnapshot battery in networkBatteries)
                        lines.Add($"    Bat ({battery.TileX},{battery.TileY}) {battery.DisplayName} | {battery.Charge}/{battery.Capacity} EU");
                }

                lines.Add("");
            }
        }

        lines.Add("=== Conduits ===");
        if (conduitsByKey.Count == 0)
        {
            lines.Add("No conduits found.");
            return;
        }

        foreach (var pair in conduitsByKey.OrderBy(kvp => kvp.Value.LocationName).ThenBy(kvp => kvp.Value.Tile.Y).ThenBy(kvp => kvp.Value.Tile.X))
        {
            string conduitKey = pair.Key;
            ConduitEntry conduit = pair.Value;
            var partner = conduitMgr.GetPartner(conduit.LocationName, conduit.Tile);
            string selected = selectedConduitKey == conduitKey ? "[*]" : "[ ]";
            string partnerText = partner == null
                ? "unlinked"
                : $"linked -> {partner.Value.Location} ({partner.Value.Tile.X},{partner.Value.Tile.Y})";
            string line = $"{selected} {conduit.LocationName} ({conduit.Tile.X},{conduit.Tile.Y}) {partnerText}";

            int lineIndex = lines.Count;
            lines.Add(line);
            conduitLineToKey[lineIndex] = conduitKey;
        }
    }

    private void HandleConduitClick(string conduitKey)
    {
        if (!conduitsByKey.TryGetValue(conduitKey, out ConduitEntry? clicked))
        {
            selectedConduitKey = null;
            BuildLines();
            return;
        }

        if (selectedConduitKey == null)
        {
            selectedConduitKey = conduitKey;
            Game1.addHUDMessage(new HUDMessage($"Selected conduit at {clicked.LocationName} ({clicked.Tile.X},{clicked.Tile.Y}). Select another conduit to link.", HUDMessage.newQuest_type));
            BuildLines();
            return;
        }

        if (selectedConduitKey == conduitKey)
        {
            selectedConduitKey = null;
            BuildLines();
            return;
        }

        if (!conduitsByKey.TryGetValue(selectedConduitKey, out ConduitEntry? first))
        {
            selectedConduitKey = conduitKey;
            BuildLines();
            return;
        }

        bool linked = conduitMgr.LinkConduits(first.LocationName, first.Tile, clicked.LocationName, clicked.Tile);
        if (linked)
        {
            Game1.addHUDMessage(new HUDMessage("Conduits linked from Power Tab.", HUDMessage.achievement_type));
            powerMgr.MarkAllDirty();
        }
        else
        {
            Game1.addHUDMessage(new HUDMessage("Could not link those conduits.", HUDMessage.error_type));
        }

        selectedConduitKey = null;
        BuildLines();
    }

    private void UnlinkSelectedConduit()
    {
        if (selectedConduitKey == null || !conduitsByKey.TryGetValue(selectedConduitKey, out ConduitEntry? selected))
            return;

        conduitMgr.RemoveLinksInvolving(selected.LocationName, selected.Tile);
        powerMgr.MarkAllDirty();
        selectedConduitKey = null;
        Game1.addHUDMessage(new HUDMessage("Conduit link removed.", HUDMessage.error_type));
        BuildLines();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (x < TextLeft || x > xPositionOnScreen + width - 32 || y < TextTop || y > yPositionOnScreen + height - 32)
            return;

        int clickedLine = scrollOffset + ((y - TextTop) / LineHeight);
        if (conduitLineToKey.TryGetValue(clickedLine, out string? conduitKey))
            HandleConduitClick(conduitKey);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction > 0 && scrollOffset > 0)
            scrollOffset--;
        else if (direction < 0 && scrollOffset + maxVisibleLines < lines.Count)
            scrollOffset++;
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape || key == Keys.E)
        {
            exitThisMenu();
            return;
        }

        if (key == Keys.Delete || key == Keys.Back)
        {
            UnlinkSelectedConduit();
            return;
        }

        if (key == Keys.R)
        {
            powerMgr.MarkAllDirty();
            BuildLines();
            return;
        }

        if (key == Keys.Up && scrollOffset > 0)
        {
            scrollOffset--;
            return;
        }

        if (key == Keys.Down && scrollOffset + maxVisibleLines < lines.Count)
        {
            scrollOffset++;
            return;
        }

        base.receiveKeyPress(key);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        int textY = TextTop;
        int endLine = Math.Min(scrollOffset + maxVisibleLines, lines.Count);
        for (int i = scrollOffset; i < endLine; i++)
        {
            string line = lines[i];
            Color color = Color.Black;

            if (line.StartsWith("==="))
                color = new Color(60, 60, 160);
            else if (line.StartsWith("---"))
                color = new Color(90, 90, 90);
            else if (line.StartsWith("  ["))
                color = new Color(70, 70, 120);
            else if (line.Contains("Powered / Active") || line.Contains("Powered / Ready") || line.Contains("Powered / Aging"))
                color = new Color(20, 140, 20);
            else if (line.Contains("Waiting For Power"))
                color = new Color(170, 120, 20);
            else if (line.Contains("Idle / Unpowered") || line.Contains("Aging / Unpowered"))
                color = new Color(180, 40, 40);
            else if (line.StartsWith("[*]"))
                color = new Color(20, 140, 20);
            else if (line.StartsWith("[ ]"))
                color = new Color(30, 90, 140);

            b.DrawString(Game1.smallFont, line, new Vector2(TextLeft, textY), color);
            textY += LineHeight;
        }

        if (scrollOffset > 0)
            b.DrawString(Game1.smallFont, "^ Scroll Up ^", new Vector2(TextLeft + width / 2 - 60, yPositionOnScreen + 12), Color.Gray);
        if (endLine < lines.Count)
            b.DrawString(Game1.smallFont, "v Scroll Down v", new Vector2(TextLeft + width / 2 - 60, yPositionOnScreen + height - 28), Color.Gray);

        base.draw(b);
        drawMouse(b);
    }
}
