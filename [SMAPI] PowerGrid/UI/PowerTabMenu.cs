using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using Darth.PowerGrid.Core;

namespace Darth.PowerGrid.UI;

internal sealed class PowerTabMenu : IClickableMenu
{
    private readonly PowerManager powerMgr;
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

    public PowerTabMenu(PowerManager powerMgr, BatteryStateManager batteryState, FuelManager fuelMgr, ConduitManager conduitMgr)
        : base(
            (int)(Game1.uiViewport.Width * 0.08f),
            (int)(Game1.uiViewport.Height * 0.05f),
            (int)(Game1.uiViewport.Width * 0.84f),
            (int)(Game1.uiViewport.Height * 0.9f),
            showUpperRightCloseButton: true)
    {
        this.powerMgr = powerMgr;
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

    private void BuildLines()
    {
        lines.Clear();
        conduitLineToKey.Clear();
        conduitsByKey.Clear();

        var locations = CollectLocations();
        int totalNetworks = 0;
        int totalGenerators = 0;
        int totalCables = 0;
        int totalBatteries = 0;
        int totalConsumers = 0;
        int totalConduits = 0;
        int totalGeneration = 0;
        int totalDemand = 0;
        int totalCapacity = 0;
        int activeLocations = 0;

        var perLocation = new List<(GameLocation Location, string Name, List<PowerNetwork> Networks)>();

        foreach (GameLocation location in locations)
        {
            List<PowerNetwork> networks = powerMgr.GetNetworks(location);
            if (networks.Count > 0)
                activeLocations++;

            perLocation.Add((location, location.NameOrUniqueName, networks));

            totalNetworks += networks.Count;
            foreach (PowerNetwork net in networks)
            {
                totalGenerators += net.Generators.Count;
                totalCables += net.Cables.Count;
                totalBatteries += net.Batteries.Count;
                totalConsumers += net.Consumers.Count;
                totalConduits += net.Conduits.Count;
                totalGeneration += net.TotalGenerationPerTick();
                totalDemand += net.TotalDemandPerTick();
                totalCapacity += net.TotalBatteryCapacity();

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

        foreach ((GameLocation location, string name, List<PowerNetwork> networks) in perLocation.OrderBy(p => p.Name))
        {
            if (networks.Count == 0)
                continue;

            lines.Add($"--- {name} ---");
            foreach (PowerNetwork net in networks.OrderBy(n => n.NetworkId))
            {
                string throughput = net.MinCableThroughput == int.MaxValue
                    ? "unlimited"
                    : $"{net.MinCableThroughput} EU";
                lines.Add($"  Net #{net.NetworkId}: G={net.Generators.Count}, C={net.Cables.Count}, B={net.Batteries.Count}, U={net.Consumers.Count}, P={net.Conduits.Count}, Throughput={throughput}");

                TickReport? report = powerMgr.GetLastReports(name).Find(r => r.NetworkId == net.NetworkId);

                foreach (PowerNode gen in net.Generators)
                {
                    if (gen.LocationName != name)
                        continue;

                    string fuelInfo = gen.RequiresFuel
                        ? $"fuel ticks {fuelMgr.GetFuelTicksRemaining(gen.UniqueKey)}"
                        : "passive";
                    lines.Add($"    Gen ({gen.Tile.X},{gen.Tile.Y}) {gen.ItemId}: {gen.GenerationPerTick} EU/tick [{fuelInfo}]");
                }

                foreach (PowerNode consumer in net.Consumers)
                {
                    if (consumer.LocationName != name)
                        continue;

                    AllocationResult? allocation = report?.Allocations.FirstOrDefault(a =>
                        a.Consumer.LocationName == consumer.LocationName && a.Consumer.Tile == consumer.Tile);

                    StardewValley.Object? machine = location.getObjectAtTile((int)consumer.Tile.X, (int)consumer.Tile.Y);
                    int machineMinutes = machine?.MinutesUntilReady ?? 0;

                    if (allocation == null)
                    {
                        lines.Add($"    Cons ({consumer.Tile.X},{consumer.Tile.Y}) {consumer.ItemId}: connected, awaiting tick, now={machineMinutes}m");
                        continue;
                    }

                    string powerState = allocation.EUAllocated > 0 ? "POWERED" : "UNPOWERED";
                    lines.Add($"    Cons ({consumer.Tile.X},{consumer.Tile.Y}) {consumer.ItemId}: {powerState}, EU {allocation.EUAllocated}/{allocation.EUDemanded}, speed {allocation.SpeedupFraction:P0}, accel {allocation.MinutesAccelerated}m, now={machineMinutes}m");
                }
            }

            lines.Add("");
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
