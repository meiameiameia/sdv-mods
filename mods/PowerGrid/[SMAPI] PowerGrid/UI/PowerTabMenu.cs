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

    private enum PowerTabView
    {
        Overview,
        Networks,
        Machines,
        Conduits
    }

    private PowerTabView currentView = PowerTabView.Overview;
    private int scrollOffset;
    private readonly List<DisplayLine> lines = new();
    private readonly List<ClickableComponent> tabButtons = new();
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
        RebuildTabButtons();
        BuildLines();
    }

    private int LineHeight => (int)(Game1.smallFont.LineSpacing * 1.1f);
    private int TextLeft => xPositionOnScreen + 32;
    private int ContentWidth => width - 64;
    private int TabTop => yPositionOnScreen + 60;
    private int TabHeight => 40;
    private int TextTop => yPositionOnScreen + 118;
    private int MaxVisibleLines => Math.Max(8, (yPositionOnScreen + height - 42 - TextTop) / LineHeight);

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

        return new StatusBadge("IDLE", MutedColor);
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

    private static string TrimProgressText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string text = value
            .Replace("Day-based aging: ", "", StringComparison.Ordinal)
            .Replace("Powered now at ", "powered ", StringComparison.Ordinal)
            .Replace("; overnight bonus applies on day change.", " (overnight bonus)", StringComparison.Ordinal)
            .Replace("Not powered for the next overnight bonus.", "no overnight bonus", StringComparison.Ordinal)
            .Replace("Day-based aging machine. ", "", StringComparison.Ordinal);

        return TrimDetail(text, 82);
    }

    private static string FormatConsumerDetail(PowerConsumerSnapshot consumer)
    {
        if (consumer.ProgressMode == "days" && !string.IsNullOrWhiteSpace(consumer.ProgressText))
            return $"EU {consumer.EUAllocated}/{consumer.DemandPerTick} | speed {consumer.SpeedupFraction:P0} | {TrimProgressText(consumer.ProgressText)}";

        if (consumer.IsProcessing)
            return $"EU {consumer.EUAllocated}/{consumer.DemandPerTick} | speed {consumer.SpeedupFraction:P0} | +{consumer.MinutesAccelerated}m/tick | ~{consumer.MinutesRemaining}m left";

        if (consumer.IsPowered)
            return $"EU {consumer.EUAllocated}/{consumer.DemandPerTick} | speed {consumer.SpeedupFraction:P0} | ready for work";

        return $"EU {consumer.EUAllocated}/{consumer.DemandPerTick} | idle";
    }

    private static string FormatGeneratorDetail(PowerGeneratorSnapshot generator)
    {
        if (generator.IsBlockedIndoors)
            return $"offline | outdoor placement required | {generator.GenerationPerTick} EU/tick";

        if (!generator.RequiresFuel)
            return generator.IsOnline
                ? $"online | {generator.GenerationPerTick} EU/tick | passive"
                : $"offline | {generator.GenerationPerTick} EU/tick | passive";

        if (generator.IsOnline)
            return $"online | {generator.GenerationPerTick} EU/tick | {generator.FuelTicksRemaining} fuel ticks";

        return generator.FuelTicksRemaining > 0
            ? $"fueled | waiting for power tick | {generator.FuelTicksRemaining} fuel ticks"
            : $"offline | no fuel | {generator.GenerationPerTick} EU/tick";
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

    private static Color GetGeneratorColor(PowerGeneratorSnapshot generator)
    {
        if (generator.IsOnline)
            return PositiveColor;

        if (generator.RequiresFuel && generator.FuelTicksRemaining <= 0)
            return WarningColor;

        return MutedColor;
    }

    private static string FormatConsumerName(PowerConsumerSnapshot consumer)
    {
        return $"{consumer.DisplayName} ({consumer.LocationName} {consumer.TileX},{consumer.TileY})";
    }

    private static bool MatchesNetwork(PowerNetworkSnapshot network, int networkId, string locationName)
    {
        return network.NetworkId == networkId
            && network.LocationNames.Any(name => string.Equals(name, locationName, StringComparison.Ordinal));
    }

    private static bool BelongsToNetwork(PowerConsumerSnapshot consumer, PowerNetworkSnapshot network)
    {
        return MatchesNetwork(network, consumer.NetworkId, consumer.LocationName);
    }

    private static bool BelongsToNetwork(PowerGeneratorSnapshot generator, PowerNetworkSnapshot network)
    {
        return MatchesNetwork(network, generator.NetworkId, generator.LocationName);
    }

    private static bool BelongsToNetwork(PowerBatterySnapshot battery, PowerNetworkSnapshot network)
    {
        return MatchesNetwork(network, battery.NetworkId, battery.LocationName);
    }

    private static PowerNetworkSnapshot? FindMatchingNetwork(PowerConsumerSnapshot consumer, IReadOnlyList<PowerNetworkSnapshot> networks)
    {
        return networks.FirstOrDefault(net => BelongsToNetwork(consumer, net));
    }

    private static string TrimToPixelWidth(string value, SpriteFont font, float maxWidth)
    {
        if (string.IsNullOrEmpty(value) || font.MeasureString(value).X <= maxWidth)
            return value;

        const string ellipsis = "...";
        for (int length = value.Length - 1; length > 0; length--)
        {
            string candidate = value[..length] + ellipsis;
            if (font.MeasureString(candidate).X <= maxWidth)
                return candidate;
        }

        return ellipsis;
    }

    private static string GetNetworkFocusLocation(
        PowerNetworkSnapshot net,
        IReadOnlyList<PowerConsumerSnapshot> consumers,
        IReadOnlyList<PowerGeneratorSnapshot> generators,
        IReadOnlyList<PowerBatterySnapshot>? batteries = null)
    {
        PowerConsumerSnapshot? activeConsumer = consumers.FirstOrDefault(consumer => consumer.IsProcessing);
        if (activeConsumer != null)
            return activeConsumer.LocationName;

        PowerGeneratorSnapshot? firstGenerator = generators.FirstOrDefault();
        if (firstGenerator != null)
            return firstGenerator.LocationName;

        PowerBatterySnapshot? firstBattery = batteries?.FirstOrDefault();
        if (firstBattery != null)
            return firstBattery.LocationName;

        return net.LocationNames.FirstOrDefault() ?? "Unknown";
    }

    private static string FormatNetworkLabel(
        PowerNetworkSnapshot net,
        IReadOnlyList<PowerConsumerSnapshot> consumers,
        IReadOnlyList<PowerGeneratorSnapshot> generators,
        IReadOnlyList<PowerBatterySnapshot>? batteries = null)
    {
        string scope = FormatLocationScope(net.LocationNames);
        string focus = GetNetworkFocusLocation(net, consumers, generators, batteries);

        if (net.LocationNames.Length <= 1 || string.Equals(scope, focus, StringComparison.Ordinal))
            return $"Net #{net.NetworkId} | {scope}";

        return $"Net #{net.NetworkId} | focus {focus} | {scope}";
    }

    private static string FormatNetworkPower(PowerNetworkSnapshot net)
    {
        return $"Supply {net.LastTickGenerated}/{net.TotalGenerationPerTick} EU/tick | Used {net.LastTickConsumed}/{net.TotalDemandPerTick} | Battery {net.LastTickFromBatteries} out, {net.LastTickStoredInBatteries} in | Throughput {FormatThroughput(net.CableThroughputCap)}";
    }

    private static string FormatTabLabel(PowerTabView view)
    {
        return view switch
        {
            PowerTabView.Overview => "Overview",
            PowerTabView.Networks => "Networks",
            PowerTabView.Machines => "Machines",
            PowerTabView.Conduits => "Conduits",
            _ => view.ToString()
        };
    }

    private static string FormatHealthLine(PowerNetworkSnapshot net, IReadOnlyList<PowerConsumerSnapshot> consumers)
    {
        int waiting = consumers.Count(consumer => consumer.IsProcessing && !consumer.IsPowered);
        int active = consumers.Count(consumer => consumer.IsProcessing && consumer.IsPowered);
        int ready = consumers.Count(consumer => !consumer.IsProcessing && consumer.IsPowered);

        if (waiting > 0)
            return $"{waiting} machine(s) waiting for power";

        if (active > 0)
            return $"{active} active machine(s) powered";

        if (ready > 0)
            return $"{ready} ready machine(s) on the network";

        if (net.TotalStoredEU > 0 || net.LastTickGenerated > 0)
            return "ready, no active machine demand";

        return "offline";
    }

    private static string BuildPowerFixHint(PowerConsumerSnapshot consumer, PowerNetworkSnapshot? network)
    {
        if (network == null)
            return "Check that the machine is connected to a valid PowerGrid network.";

        if (network.GeneratorCount == 0 && network.BatteryCount == 0)
            return $"Connect Net #{consumer.NetworkId} to a generator/battery network with a conduit pair.";

        if (network.TotalStoredEU <= 0 && network.LastTickGenerated <= 0)
            return $"Net #{consumer.NetworkId} has no usable power right now; add generation, fuel, or battery charge.";

        if (network.LastTickGenerated + network.LastTickFromBatteries < consumer.DemandPerTick)
            return $"Net #{consumer.NetworkId} needs {consumer.DemandPerTick} EU/tick for this machine; add supply or reduce demand.";

        return $"Net #{consumer.NetworkId} has power available; check cable adjacency and conduit pairing.";
    }

    private void RebuildTabButtons()
    {
        tabButtons.Clear();

        const int spacing = 10;
        int buttonCount = Enum.GetValues<PowerTabView>().Length;
        int tabWidth = Math.Clamp((ContentWidth - (spacing * (buttonCount - 1))) / buttonCount, 112, 170);
        int totalWidth = (tabWidth * buttonCount) + (spacing * (buttonCount - 1));
        int tabX = xPositionOnScreen + ((width - totalWidth) / 2);

        foreach (PowerTabView view in Enum.GetValues<PowerTabView>())
        {
            string label = FormatTabLabel(view);
            tabButtons.Add(new ClickableComponent(new Rectangle(tabX, TabTop, tabWidth, TabHeight), view.ToString(), label));
            tabX += tabWidth + spacing;
        }
    }

    private void SetView(PowerTabView view)
    {
        if (currentView == view)
            return;

        currentView = view;
        scrollOffset = 0;
        selectedConduitKey = null;
        BuildLines();
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

        BuildCurrentView(
            networkSnapshots,
            consumerSnapshots,
            generatorSnapshots,
            batterySnapshots,
            totalNetworks,
            totalGenerators,
            totalConsumers,
            totalConduits,
            totalGeneration,
            totalDemand,
            totalCapacity,
            totalStoredEu,
            activeLocations,
            crossLocationNetworks,
            onlineGenerators,
            globalConsumerCounts,
            storagePercent);
    }

    private void BuildCurrentView(
        IReadOnlyList<PowerNetworkSnapshot> networkSnapshots,
        IReadOnlyList<PowerConsumerSnapshot> consumerSnapshots,
        IReadOnlyList<PowerGeneratorSnapshot> generatorSnapshots,
        IReadOnlyList<PowerBatterySnapshot> batterySnapshots,
        int totalNetworks,
        int totalGenerators,
        int totalConsumers,
        int totalConduits,
        int totalGeneration,
        int totalDemand,
        int totalCapacity,
        int totalStoredEu,
        int activeLocations,
        int crossLocationNetworks,
        int onlineGenerators,
        ConsumerCounts globalConsumerCounts,
        float storagePercent)
    {
        switch (currentView)
        {
            case PowerTabView.Overview:
                BuildOverviewLines(
                    networkSnapshots,
                    consumerSnapshots,
                    generatorSnapshots,
                    totalNetworks,
                    totalGenerators,
                    totalGeneration,
                    totalDemand,
                    totalCapacity,
                    totalStoredEu,
                    activeLocations,
                    crossLocationNetworks,
                    onlineGenerators,
                    globalConsumerCounts,
                    storagePercent);
                break;

            case PowerTabView.Networks:
                BuildNetworkLines(networkSnapshots, consumerSnapshots, generatorSnapshots, batterySnapshots);
                break;

            case PowerTabView.Machines:
                BuildMachineLines(networkSnapshots, consumerSnapshots);
                break;

            case PowerTabView.Conduits:
                BuildConduitLines();
                break;
        }
    }

    private void BuildOverviewLines(
        IReadOnlyList<PowerNetworkSnapshot> networkSnapshots,
        IReadOnlyList<PowerConsumerSnapshot> consumerSnapshots,
        IReadOnlyList<PowerGeneratorSnapshot> generatorSnapshots,
        int totalNetworks,
        int totalGenerators,
        int totalGeneration,
        int totalDemand,
        int totalCapacity,
        int totalStoredEu,
        int activeLocations,
        int crossLocationNetworks,
        int onlineGenerators,
        ConsumerCounts globalConsumerCounts,
        float storagePercent)
    {
        bool hasWaitingConsumers = globalConsumerCounts.WaitingForPower > 0;
        Color globalStatusColor = hasWaitingConsumers ? WarningColor : totalNetworks == 0 ? NegativeColor : PositiveColor;
        string globalStatus = hasWaitingConsumers
            ? $"{globalConsumerCounts.WaitingForPower} machine(s) need power"
            : totalNetworks == 0
                ? "No power networks detected"
                : "All active machines powered";

        AddLine(hasWaitingConsumers ? "Action needed" : "Power status", SectionColor);
        AddLine(globalStatus, globalStatusColor);
        AddLine($"{onlineGenerators}/{totalGenerators} generators online | {totalGeneration} EU/tick supply | {totalDemand} EU/tick demand", HeaderColor);
        AddLine($"{totalStoredEu}/{totalCapacity} EU stored ({storagePercent:P0}) | {totalNetworks} networks, {crossLocationNetworks} linked | {activeLocations} locations", HeaderColor);
        AddLine($"{globalConsumerCounts.ActivePowered} active | {globalConsumerCounts.WaitingForPower} waiting | {globalConsumerCounts.Ready} ready | {globalConsumerCounts.Idle} idle", HeaderColor);
        AddLine();

        AddSectionTitle("What Needs Attention");
        List<PowerConsumerSnapshot> waitingConsumers = consumerSnapshots
            .Where(consumer => consumer.IsProcessing && !consumer.IsPowered)
            .OrderBy(consumer => consumer.LocationName, StringComparer.Ordinal)
            .ThenBy(consumer => consumer.TileY)
            .ThenBy(consumer => consumer.TileX)
            .Take(5)
            .ToList();

        if (waitingConsumers.Count == 0)
        {
            AddLine("All processing machines currently have power.", PositiveColor);
        }
        else
        {
            foreach (PowerConsumerSnapshot consumer in waitingConsumers)
            {
                PowerNetworkSnapshot? network = FindMatchingNetwork(consumer, networkSnapshots);
                string networkLabel = network == null
                    ? $"Net #{consumer.NetworkId}"
                    : FormatNetworkLabel(
                        network,
                        consumerSnapshots.Where(c => BelongsToNetwork(c, network)).ToList(),
                        generatorSnapshots.Where(g => BelongsToNetwork(g, network)).ToList());

                AddLine($"{consumer.DisplayName} at {consumer.LocationName} ({consumer.TileX},{consumer.TileY}) is processing without power.", WarningColor);
                AddLine($"  {networkLabel} | needs {consumer.DemandPerTick} EU/tick. {BuildPowerFixHint(consumer, network)}", MutedColor);
                AddLine($"  Current: {TrimDetail(FormatConsumerDetail(consumer), 88)}", MutedColor);
            }
        }

        AddLine();
        AddSectionTitle("Network Summary");

        if (networkSnapshots.Count == 0)
        {
            AddLine("No active networks detected. Place adjacent PowerGrid objects to form a network.", NegativeColor);
            AddLine();
        }
        else
        {
            foreach (PowerNetworkSnapshot net in networkSnapshots
                .OrderByDescending(net => consumerSnapshots.Any(consumer => BelongsToNetwork(consumer, net) && consumer.IsProcessing && !consumer.IsPowered))
                .ThenBy(net => net.NetworkId))
            {
                List<PowerConsumerSnapshot> networkConsumers = consumerSnapshots
                    .Where(consumer => BelongsToNetwork(consumer, net))
                    .OrderByDescending(consumer => consumer.IsProcessing && !consumer.IsPowered)
                    .ThenByDescending(consumer => consumer.IsProcessing && consumer.IsPowered)
                    .ThenBy(consumer => consumer.LocationName, StringComparer.Ordinal)
                    .ThenBy(consumer => consumer.TileY)
                    .ThenBy(consumer => consumer.TileX)
                    .ToList();

                List<PowerGeneratorSnapshot> networkGenerators = generatorSnapshots
                    .Where(generator => BelongsToNetwork(generator, net))
                    .OrderBy(generator => generator.LocationName, StringComparer.Ordinal)
                    .ThenBy(generator => generator.TileY)
                    .ThenBy(generator => generator.TileX)
                    .ToList();

                StatusBadge badge = GetNetworkBadge(net, networkConsumers, networkGenerators);
                AddLine($"{FormatNetworkLabel(net, networkConsumers, networkGenerators)} | {badge.Label}", badge.Color);
                AddLine($"  {FormatHealthLine(net, networkConsumers)} | {net.TotalStoredEU}/{net.TotalBatteryCapacity} EU stored | {net.LastTickGenerated}/{net.TotalGenerationPerTick} EU/tick supply", MutedColor);

                PowerConsumerSnapshot? topMachine = networkConsumers.FirstOrDefault(consumer => consumer.IsProcessing);
                if (topMachine != null)
                    AddLine($"  Main machine: {topMachine.DisplayName} ({topMachine.LocationName} {topMachine.TileX},{topMachine.TileY}) - {TrimDetail(FormatConsumerDetail(topMachine), 74)}", topMachine.IsPowered ? PositiveColor : WarningColor);
            }
        }
    }

    private void BuildNetworkLines(
        IReadOnlyList<PowerNetworkSnapshot> networkSnapshots,
        IReadOnlyList<PowerConsumerSnapshot> consumerSnapshots,
        IReadOnlyList<PowerGeneratorSnapshot> generatorSnapshots,
        IReadOnlyList<PowerBatterySnapshot> batterySnapshots)
    {
        AddSectionTitle("Networks");

        if (networkSnapshots.Count == 0)
        {
            AddLine("No active networks detected.", NegativeColor);
            return;
        }

        foreach (PowerNetworkSnapshot net in networkSnapshots.OrderBy(net => net.NetworkId))
        {
            List<PowerGeneratorSnapshot> networkGenerators = generatorSnapshots
                .Where(generator => BelongsToNetwork(generator, net))
                .OrderBy(generator => generator.LocationName, StringComparer.Ordinal)
                .ThenBy(generator => generator.TileY)
                .ThenBy(generator => generator.TileX)
                .ToList();

            List<PowerConsumerSnapshot> networkConsumers = consumerSnapshots
                .Where(consumer => BelongsToNetwork(consumer, net))
                .OrderBy(consumer => consumer.LocationName, StringComparer.Ordinal)
                .ThenBy(consumer => consumer.TileY)
                .ThenBy(consumer => consumer.TileX)
                .ToList();

            List<PowerBatterySnapshot> networkBatteries = batterySnapshots
                .Where(battery => BelongsToNetwork(battery, net))
                .OrderBy(battery => battery.LocationName, StringComparer.Ordinal)
                .ThenBy(battery => battery.TileY)
                .ThenBy(battery => battery.TileX)
                .ToList();

            ConsumerCounts networkCounts = CountConsumers(networkConsumers);
            StatusBadge badge = GetNetworkBadge(net, networkConsumers, networkGenerators);
            float networkStoragePercent = net.TotalBatteryCapacity > 0 ? (float)net.TotalStoredEU / net.TotalBatteryCapacity : 0f;

            AddLine($"{FormatNetworkLabel(net, networkConsumers, networkGenerators, networkBatteries)} | {badge.Label}", badge.Color);
            AddLine($"  {net.LastTickGenerated}/{net.TotalGenerationPerTick} EU/tick supply | {net.LastTickConsumed}/{net.TotalDemandPerTick} used | throughput {FormatThroughput(net.CableThroughputCap)}", MutedColor);
            AddLine($"  Storage {net.TotalStoredEU}/{net.TotalBatteryCapacity} EU ({networkStoragePercent:P0}) | {FormatMachineSummary(networkCounts)}", MutedColor);

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

                AddLine($"  {locationName}: {FormatLocationSummary(locationGenerators, locationConsumers, locationBatteries)}", InfoColor);

                foreach (PowerGeneratorSnapshot generator in locationGenerators)
                    AddLine($"    Gen ({generator.TileX},{generator.TileY}) {generator.DisplayName}: {FormatGeneratorDetail(generator)}", GetGeneratorColor(generator));

                foreach (PowerBatterySnapshot battery in locationBatteries)
                    AddLine($"    Bat ({battery.TileX},{battery.TileY}) {battery.DisplayName}: {FormatBatteryDetail(battery)}", GetBatteryColor(battery));
            }

            AddLine();
        }
    }

    private void BuildMachineLines(IReadOnlyList<PowerNetworkSnapshot> networkSnapshots, IReadOnlyList<PowerConsumerSnapshot> consumerSnapshots)
    {
        AddSectionTitle("Machines");

        if (consumerSnapshots.Count == 0)
        {
            AddLine("No powered machines detected.", MutedColor);
            return;
        }

        foreach (PowerConsumerSnapshot consumer in consumerSnapshots
            .OrderByDescending(consumer => consumer.IsProcessing && !consumer.IsPowered)
            .ThenByDescending(consumer => consumer.IsProcessing && consumer.IsPowered)
            .ThenBy(consumer => consumer.LocationName, StringComparer.Ordinal)
            .ThenBy(consumer => consumer.TileY)
            .ThenBy(consumer => consumer.TileX))
        {
            StatusBadge badge = GetConsumerBadge(consumer);
            PowerNetworkSnapshot? network = FindMatchingNetwork(consumer, networkSnapshots);
            string scope = network == null ? "Unknown net" : FormatLocationScope(network.LocationNames);
            AddLine($"{consumer.DisplayName} | {badge.Label} | {consumer.LocationName} ({consumer.TileX},{consumer.TileY})", badge.Color);
            AddLine($"  Net #{consumer.NetworkId} | {scope} | {TrimDetail(FormatConsumerDetail(consumer), 74)}", MutedColor);
        }
    }

    private void BuildConduitLines()
    {
        AddSectionTitle("Conduits");
        if (conduitsByKey.Count == 0)
        {
            AddLine("No conduits found.", NegativeColor);
            return;
        }

        int linkedConduits = conduitsByKey.Values.Count(conduit => conduitMgr.GetPartner(conduit.LocationName, conduit.Tile) != null);
        AddLine($"{linkedConduits}/{conduitsByKey.Count} linked. Select two conduits to link them; Delete unlinks selected.", InfoColor);

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

        foreach (ClickableComponent tab in tabButtons)
        {
            if (!tab.containsPoint(x, y))
                continue;

            if (Enum.TryParse(tab.name, out PowerTabView view))
                SetView(view);
            return;
        }

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
        else if (direction < 0 && scrollOffset + MaxVisibleLines < lines.Count)
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

        if (key == Keys.Tab)
        {
            PowerTabView next = currentView == PowerTabView.Conduits
                ? PowerTabView.Overview
                : (PowerTabView)((int)currentView + 1);
            SetView(next);
            return;
        }

        if (key is Keys.D1 or Keys.NumPad1)
        {
            SetView(PowerTabView.Overview);
            return;
        }

        if (key is Keys.D2 or Keys.NumPad2)
        {
            SetView(PowerTabView.Networks);
            return;
        }

        if (key is Keys.D3 or Keys.NumPad3)
        {
            SetView(PowerTabView.Machines);
            return;
        }

        if (key is Keys.D4 or Keys.NumPad4)
        {
            SetView(PowerTabView.Conduits);
            return;
        }

        if (key == Keys.Up && scrollOffset > 0)
        {
            scrollOffset--;
            return;
        }

        if (key == Keys.Down && scrollOffset + MaxVisibleLines < lines.Count)
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

        b.DrawString(Game1.dialogueFont, "PowerGrid", new Vector2(TextLeft, yPositionOnScreen + 22), SectionColor);

        foreach (ClickableComponent tab in tabButtons)
        {
            bool selected = Enum.TryParse(tab.name, out PowerTabView view) && view == currentView;
            Color tabColor = selected ? new Color(255, 248, 220) : new Color(226, 170, 91);
            Color textColor = selected ? SectionColor : new Color(80, 55, 30);
            drawTextureBox(b, tab.bounds.X, tab.bounds.Y, tab.bounds.Width, tab.bounds.Height, tabColor);
            string label = TrimToPixelWidth(tab.label, Game1.smallFont, tab.bounds.Width - 24);
            Vector2 labelSize = Game1.smallFont.MeasureString(label);
            Vector2 labelPos = new(
                tab.bounds.X + ((tab.bounds.Width - labelSize.X) / 2f),
                tab.bounds.Y + ((tab.bounds.Height - labelSize.Y) / 2f) - 1f);
            b.DrawString(Game1.smallFont, label, labelPos, textColor);
        }

        int textY = TextTop;
        int endLine = Math.Min(scrollOffset + MaxVisibleLines, lines.Count);
        for (int i = scrollOffset; i < endLine; i++)
        {
            DisplayLine line = lines[i];
            string visibleText = TrimToPixelWidth(line.Text, Game1.smallFont, ContentWidth);
            b.DrawString(Game1.smallFont, visibleText, new Vector2(TextLeft, textY), line.Color);
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
