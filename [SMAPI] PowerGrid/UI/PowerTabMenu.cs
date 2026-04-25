using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using Meiameiameia.PowerGrid.Core;
using Meiameiameia.PowerGrid.Integrations;

namespace Meiameiameia.PowerGrid.UI;

internal sealed class PowerTabMenu : IClickableMenu
{
    private static readonly Color SectionColor = new(60, 60, 160);
    private static readonly Color HeaderColor = new(90, 90, 90);
    private static readonly Color InfoColor = new(70, 70, 120);
    private static readonly Color PositiveColor = new(20, 140, 20);
    private static readonly Color WarningColor = new(170, 120, 20);
    private static readonly Color NegativeColor = new(180, 40, 40);
    private static readonly Color MutedColor = new(75, 75, 75);
    private static readonly Color TextColor = Color.Black;

    private readonly PowerManager powerMgr;
    private readonly PowerQueryService powerQuery;
    private readonly BatteryStateManager batteryState;
    private readonly FuelManager fuelMgr;
    private readonly ConduitManager conduitMgr;

    private int scrollOffset;
    private readonly int maxVisibleLines = 20;
    private readonly List<DisplayLine> lines = new();
    private readonly Dictionary<int, string> conduitLineToKey = new();
    private readonly Dictionary<string, ConduitEntry> conduitsByKey = new();
    private string? selectedConduitKey;

    private sealed class DisplayLine
    {
        public string Text { get; init; } = "";
        public Color Color { get; init; } = TextColor;
    }

    private sealed class ConduitEntry
    {
        public string LocationName { get; init; } = "";
        public Vector2 Tile { get; init; }
    }

    private sealed class ConsumerCounts
    {
        public int Total { get; set; }
        public int Powered { get; set; }
        public int ActivePowered { get; set; }
        public int WaitingForPower { get; set; }
        public int Ready { get; set; }
        public int Idle { get; set; }
        public int Aging { get; set; }
    }

    private readonly record struct StatusBadge(string Label, Color Color);

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

    private static ConsumerCounts CountConsumers(IEnumerable<PowerConsumerSnapshot> consumers)
    {
        ConsumerCounts counts = new();

        foreach (PowerConsumerSnapshot consumer in consumers)
        {
            counts.Total++;

            if (consumer.IsPowered)
                counts.Powered++;

            if (consumer.ProgressMode == "days" && consumer.IsProcessing)
                counts.Aging++;

            if (consumer.IsProcessing && consumer.IsPowered)
                counts.ActivePowered++;
            else if (consumer.IsProcessing)
                counts.WaitingForPower++;
            else if (consumer.IsPowered)
                counts.Ready++;
            else
                counts.Idle++;
        }

        return counts;
    }

    private static StatusBadge GetNetworkBadge(PowerNetworkSnapshot network, IReadOnlyList<PowerConsumerSnapshot> consumers, IReadOnlyList<PowerGeneratorSnapshot> generators)
    {
        if (consumers.Any(consumer => consumer.IsProcessing && !consumer.IsPowered))
            return new StatusBadge("Needs power", WarningColor);

        if (generators.Any(generator => generator.IsOnline) || network.TotalStoredEU > 0)
            return consumers.Count > 0
                ? new StatusBadge("Stable", PositiveColor)
                : new StatusBadge("Infrastructure", InfoColor);

        return new StatusBadge("Offline", NegativeColor);
    }

    private static StatusBadge GetConsumerBadge(PowerConsumerSnapshot consumer)
    {
        if (consumer.ProgressMode == "days" && consumer.IsProcessing)
            return consumer.IsPowered
                ? new StatusBadge("AGING + POWER", PositiveColor)
                : new StatusBadge("AGING / NO POWER", WarningColor);

        if (consumer.IsProcessing)
            return consumer.IsPowered
                ? new StatusBadge("ACTIVE", PositiveColor)
                : new StatusBadge("WAITING", WarningColor);

        if (consumer.IsPowered)
            return new StatusBadge("READY", PositiveColor);

        return new StatusBadge("IDLE", NegativeColor);
    }

    private static string FormatMachineSummary(ConsumerCounts counts)
    {
        List<string> parts = new()
        {
            $"{counts.Total} total",
            $"{counts.ActivePowered} active",
            $"{counts.WaitingForPower} waiting",
            $"{counts.Ready} ready",
            $"{counts.Idle} idle"
        };

        if (counts.Aging > 0)
            parts.Add($"{counts.Aging} aging");

        return string.Join(" | ", parts);
    }

    private static string FormatLocationSummary(
        IReadOnlyList<PowerGeneratorSnapshot> generators,
        IReadOnlyList<PowerConsumerSnapshot> consumers,
        IReadOnlyList<PowerBatterySnapshot> batteries)
    {
        ConsumerCounts counts = CountConsumers(consumers);
        int onlineGenerators = generators.Count(generator => generator.IsOnline);
        int storedEu = batteries.Sum(battery => battery.Charge);
        int totalCapacity = batteries.Sum(battery => battery.Capacity);

        return string.Join(
            " | ",
            $"{generators.Count} gen ({onlineGenerators} online)",
            $"{batteries.Count} batt ({storedEu}/{totalCapacity} EU)",
            $"{counts.Total} machines ({counts.ActivePowered} active, {counts.WaitingForPower} waiting, {counts.Ready} ready, {counts.Idle} idle)");
    }

    private static string FormatThroughput(int throughputCap)
    {
        return throughputCap <= 0 ? "unlimited" : $"{throughputCap} EU";
    }

    private static string TrimDetail(string value, int maxLength = 110)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..(maxLength - 3)] + "...";
    }

    private static string FormatConsumerDetail(PowerConsumerSnapshot consumer)
    {
        if (consumer.ProgressMode == "days" && !string.IsNullOrWhiteSpace(consumer.ProgressText))
            return TrimDetail($"EU {consumer.EUAllocated}/{consumer.DemandPerTick} | Speed {consumer.SpeedupFraction:P0} | {consumer.ProgressText}");

        if (consumer.IsProcessing)
            return $"EU {consumer.EUAllocated}/{consumer.DemandPerTick} | Speed {consumer.SpeedupFraction:P0} | +{consumer.MinutesAccelerated}m/tick | ~{consumer.MinutesRemaining}m left";

        if (consumer.IsPowered)
            return $"EU {consumer.EUAllocated}/{consumer.DemandPerTick} | Speed {consumer.SpeedupFraction:P0} | Ready for work";

        return $"EU {consumer.EUAllocated}/{consumer.DemandPerTick} | No allocation on latest tick";
    }

    private static string FormatGeneratorDetail(PowerGeneratorSnapshot generator)
    {
        string state = generator.IsOnline ? "ONLINE" : generator.RequiresFuel ? "NO FUEL / OFFLINE" : "OFFLINE";
        string fuelInfo = generator.RequiresFuel
            ? $"fuel {generator.FuelTicksRemaining}"
            : "passive";

        return $"{state} | {generator.GenerationPerTick} EU/tick | {fuelInfo}";
    }

    private static string FormatBatteryDetail(PowerBatterySnapshot battery)
    {
        return $"{battery.Charge}/{battery.Capacity} EU ({battery.ChargePercent:P0}) | +{battery.StoredThisTick} / -{battery.DrainedThisTick}";
    }

    private static Color GetBatteryColor(PowerBatterySnapshot battery)
    {
        if (battery.ChargePercent >= 0.6f)
            return PositiveColor;

        if (battery.ChargePercent >= 0.25f)
            return WarningColor;

        return NegativeColor;
    }

    private void AddLine(string text = "", Color? color = null, string? conduitKey = null)
    {
        int lineIndex = lines.Count;
        lines.Add(new DisplayLine
        {
            Text = text,
            Color = color ?? TextColor
        });

        if (conduitKey != null)
            conduitLineToKey[lineIndex] = conduitKey;
    }

    private void AddSectionTitle(string title)
    {
        AddLine(title, SectionColor);
    }

    private void BuildLines()
    {
        lines.Clear();
        conduitLineToKey.Clear();
        conduitsByKey.Clear();

        List<GameLocation> locations = CollectLocations();
        IReadOnlyList<PowerNetworkSnapshot> networkSnapshots = powerQuery.GetNetworkSnapshots();
        IReadOnlyList<PowerConsumerSnapshot> consumerSnapshots = powerQuery.GetConsumerSnapshots();
        IReadOnlyList<PowerGeneratorSnapshot> generatorSnapshots = powerQuery.GetGeneratorSnapshots();
        IReadOnlyList<PowerBatterySnapshot> batterySnapshots = powerQuery.GetBatterySnapshots();

        int totalNetworks = networkSnapshots.Count;
        int totalGenerators = networkSnapshots.Sum(net => net.GeneratorCount);
        int totalConsumers = networkSnapshots.Sum(net => net.ConsumerCount);
        int totalConduits = networkSnapshots.Sum(net => net.ConduitCount);
        int totalGeneration = networkSnapshots.Sum(net => net.TotalGenerationPerTick);
        int totalDemand = networkSnapshots.Sum(net => net.TotalDemandPerTick);
        int totalCapacity = networkSnapshots.Sum(net => net.TotalBatteryCapacity);
        int totalStoredEu = networkSnapshots.Sum(net => net.TotalStoredEU);
        int activeLocations = networkSnapshots
            .SelectMany(net => net.LocationNames)
            .Distinct(StringComparer.Ordinal)
            .Count();
        int crossLocationNetworks = networkSnapshots.Count(net => net.IsCrossLocation);
        int onlineGenerators = generatorSnapshots.Count(generator => generator.IsOnline);
        ConsumerCounts globalConsumerCounts = CountConsumers(consumerSnapshots);
        float storagePercent = totalCapacity > 0 ? (float)totalStoredEu / totalCapacity : 0f;

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

        AddSectionTitle("=== Power Tab ===");
        AddLine("Open with your Power Tab keybind (default P or K) or `powergrid_tab`. Click conduit A, then B to link. Delete unlinks. R refreshes.", InfoColor);
        AddLine();

        AddSectionTitle("=== Overview ===");
        AddLine($"Networks {totalNetworks} | Cross-location {crossLocationNetworks} | Active locations {activeLocations}", HeaderColor);
        AddLine($"Generators online {onlineGenerators}/{totalGenerators} | Consumers powered {globalConsumerCounts.Powered}/{totalConsumers} | Waiting {globalConsumerCounts.WaitingForPower}", HeaderColor);
        AddLine($"Battery charge {totalStoredEu}/{totalCapacity} EU ({storagePercent:P0}) | Generation {totalGeneration} EU/tick | Demand {totalDemand} EU/tick", HeaderColor);
        AddLine($"Tracked battery charge: {batteryState.TotalStoredEU()} EU | Registered conduits: {totalConduits}", HeaderColor);
        AddLine();

        AddSectionTitle("=== Needs Attention ===");
        List<PowerConsumerSnapshot> waitingConsumers = consumerSnapshots
            .Where(consumer => consumer.IsProcessing && !consumer.IsPowered)
            .OrderBy(consumer => consumer.LocationName, StringComparer.Ordinal)
            .ThenBy(consumer => consumer.TileY)
            .ThenBy(consumer => consumer.TileX)
            .ToList();

        if (waitingConsumers.Count == 0)
        {
            AddLine("No machines are currently waiting for power.", PositiveColor);
        }
        else
        {
            foreach (PowerConsumerSnapshot consumer in waitingConsumers)
                AddLine($"WAIT  {consumer.LocationName} ({consumer.TileX},{consumer.TileY}) {consumer.DisplayName} | Net #{consumer.NetworkId} | {TrimDetail(FormatConsumerDetail(consumer), 85)}", WarningColor);
        }

        AddLine();
        AddSectionTitle("=== Networks ===");

        if (networkSnapshots.Count == 0)
        {
            AddLine("No active networks detected.", NegativeColor);
            AddLine();
        }
        else
        {
            foreach (PowerNetworkSnapshot net in networkSnapshots.OrderBy(net => net.NetworkId))
            {
                List<PowerGeneratorSnapshot> networkGenerators = generatorSnapshots
                    .Where(generator => generator.NetworkId == net.NetworkId)
                    .OrderBy(generator => generator.LocationName, StringComparer.Ordinal)
                    .ThenBy(generator => generator.TileY)
                    .ThenBy(generator => generator.TileX)
                    .ToList();

                List<PowerConsumerSnapshot> networkConsumers = consumerSnapshots
                    .Where(consumer => consumer.NetworkId == net.NetworkId)
                    .OrderBy(consumer => consumer.LocationName, StringComparer.Ordinal)
                    .ThenBy(consumer => consumer.TileY)
                    .ThenBy(consumer => consumer.TileX)
                    .ToList();

                List<PowerBatterySnapshot> networkBatteries = batterySnapshots
                    .Where(battery => battery.NetworkId == net.NetworkId)
                    .OrderBy(battery => battery.LocationName, StringComparer.Ordinal)
                    .ThenBy(battery => battery.TileY)
                    .ThenBy(battery => battery.TileX)
                    .ToList();

                ConsumerCounts networkCounts = CountConsumers(networkConsumers);
                StatusBadge badge = GetNetworkBadge(net, networkConsumers, networkGenerators);
                float networkStoragePercent = net.TotalBatteryCapacity > 0 ? (float)net.TotalStoredEU / net.TotalBatteryCapacity : 0f;

                AddLine($"--- Net #{net.NetworkId} | {badge.Label} | {FormatLocationScope(net.LocationNames)} ---", badge.Color);
                AddLine($"Power: gen {net.LastTickGenerated} | used {net.LastTickConsumed} | batt out {net.LastTickFromBatteries} | batt in {net.LastTickStoredInBatteries} | throughput {FormatThroughput(net.CableThroughputCap)}", MutedColor);
                AddLine($"Storage: {net.TotalStoredEU}/{net.TotalBatteryCapacity} EU ({networkStoragePercent:P0}) | Nodes {net.GeneratorCount} gen, {net.BatteryCount} batt, {net.ConsumerCount} cons, {net.ConduitCount} conduit", MutedColor);
                AddLine($"Machines: {FormatMachineSummary(networkCounts)}", MutedColor);

                foreach (string locationName in net.LocationNames.OrderBy(name => name, StringComparer.Ordinal))
                {
                    List<PowerGeneratorSnapshot> locationGenerators = networkGenerators
                        .Where(generator => string.Equals(generator.LocationName, locationName, StringComparison.Ordinal))
                        .ToList();
                    List<PowerConsumerSnapshot> locationConsumers = networkConsumers
                        .Where(consumer => string.Equals(consumer.LocationName, locationName, StringComparison.Ordinal))
                        .ToList();
                    List<PowerBatterySnapshot> locationBatteries = networkBatteries
                        .Where(battery => string.Equals(battery.LocationName, locationName, StringComparison.Ordinal))
                        .ToList();

                    if (locationGenerators.Count == 0 && locationConsumers.Count == 0 && locationBatteries.Count == 0)
                        continue;

                    AddLine($"  [{locationName}] {FormatLocationSummary(locationGenerators, locationConsumers, locationBatteries)}", InfoColor);

                    foreach (PowerGeneratorSnapshot generator in locationGenerators)
                    {
                        Color generatorColor = generator.IsOnline ? PositiveColor : generator.RequiresFuel ? WarningColor : MutedColor;
                        AddLine($"    Gen  ({generator.TileX},{generator.TileY}) {generator.DisplayName} | {FormatGeneratorDetail(generator)}", generatorColor);
                    }

                    foreach (PowerConsumerSnapshot consumer in locationConsumers)
                    {
                        StatusBadge consumerBadge = GetConsumerBadge(consumer);
                        AddLine($"    Cons ({consumer.TileX},{consumer.TileY}) {consumer.DisplayName} | {consumerBadge.Label}", consumerBadge.Color);
                        AddLine($"      {TrimDetail(FormatConsumerDetail(consumer))}", MutedColor);
                    }

                    foreach (PowerBatterySnapshot battery in locationBatteries)
                        AddLine($"    Bat  ({battery.TileX},{battery.TileY}) {battery.DisplayName} | {FormatBatteryDetail(battery)}", GetBatteryColor(battery));
                }

                AddLine();
            }
        }

        AddSectionTitle("=== Conduits ===");
        if (conduitsByKey.Count == 0)
        {
            AddLine("No conduits found.", NegativeColor);
            return;
        }

        int linkedConduits = conduitsByKey.Values.Count(conduit => conduitMgr.GetPartner(conduit.LocationName, conduit.Tile) != null);
        AddLine($"Linked entries {linkedConduits}/{conduitsByKey.Count} | Click conduit A, then B to link | Delete unlinks selected", InfoColor);

        foreach (var pair in conduitsByKey.OrderBy(kvp => kvp.Value.LocationName).ThenBy(kvp => kvp.Value.Tile.Y).ThenBy(kvp => kvp.Value.Tile.X))
        {
            string conduitKey = pair.Key;
            ConduitEntry conduit = pair.Value;
            var partner = conduitMgr.GetPartner(conduit.LocationName, conduit.Tile);
            string selected = selectedConduitKey == conduitKey ? "[*]" : "[ ]";
            string partnerText = partner == null
                ? "unlinked"
                : $"linked -> {partner.Value.Location} ({partner.Value.Tile.X},{partner.Value.Tile.Y})";

            AddLine(
                $"{selected} {conduit.LocationName} ({conduit.Tile.X},{conduit.Tile.Y}) {partnerText}",
                selectedConduitKey == conduitKey ? PositiveColor : partner == null ? WarningColor : InfoColor,
                conduitKey);
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
            DisplayLine line = lines[i];
            b.DrawString(Game1.smallFont, line.Text, new Vector2(TextLeft, textY), line.Color);
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
