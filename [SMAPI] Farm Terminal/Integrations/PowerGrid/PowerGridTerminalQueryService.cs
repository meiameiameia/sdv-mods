using Darth.FarmTerminal.Domain;
using DarthMods.API.Power;
using StardewValley;

namespace Darth.FarmTerminal.Integrations.PowerGrid;

internal sealed class PowerGridTerminalQueryService
{
    private readonly Func<IPowerGridApi?> apiProvider;

    public PowerGridTerminalQueryService(Func<IPowerGridApi?> apiProvider)
    {
        this.apiProvider = apiProvider;
    }

    public TerminalSnapshot CreateSnapshot(string? currentLocationName)
    {
        IPowerGridApi? api = this.apiProvider();
        if (api == null)
        {
            return CreateUnavailableSnapshot(
                currentLocationName,
                "PowerGrid API unavailable. Confirm meiameiameia.PowerGrid is loaded before opening Farm Terminal.");
        }

        IReadOnlyList<PowerNetworkSnapshot> networks = api.GetNetworkSnapshots()
            .OrderByDescending(network => network.IsCrossLocation)
            .ThenBy(network => network.NetworkId)
            .ToArray();
        IReadOnlyList<PowerConsumerSnapshot> consumers = api.GetConsumerSnapshots()
            .OrderBy(consumer => GetConsumerSortOrder(consumer))
            .ThenBy(consumer => consumer.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(consumer => consumer.LocationName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        IReadOnlyList<PowerGeneratorSnapshot> generators = api.GetGeneratorSnapshots()
            .OrderBy(generator => generator.LocationName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(generator => generator.TileY)
            .ThenBy(generator => generator.TileX)
            .ToArray();
        IReadOnlyList<PowerBatterySnapshot> batteries = api.GetBatterySnapshots()
            .OrderBy(battery => battery.LocationName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(battery => battery.TileY)
            .ThenBy(battery => battery.TileX)
            .ToArray();

        List<TerminalAlertSummary> alerts = BuildAlerts(networks, consumers);
        List<TerminalSummaryCard> overviewCards = BuildOverviewCards(networks, consumers, generators, batteries, alerts);
        List<TerminalNetworkSummary> networkSummaries = BuildNetworkSummaries(networks);
        List<TerminalConsumerSummary> consumerSummaries = BuildConsumerSummaries(consumers);
        List<TerminalSourceSummary> sourceSummaries = BuildSourceSummaries(generators, batteries);

        string scopeText = string.IsNullOrWhiteSpace(currentLocationName)
            ? "Loaded farm snapshot"
            : $"Loaded farm snapshot while standing in {currentLocationName}";

        string refreshStatusText = $"Refreshed {Game1.dayOfMonth}/{Game1.currentSeason} at {FormatTime(Game1.timeOfDay)}";
        string overviewSummaryText = BuildOverviewSummary(networks, consumers, generators, batteries, alerts);

        return new TerminalSnapshot(
            scopeText,
            refreshStatusText,
            overviewSummaryText,
            overviewCards,
            networkSummaries,
            consumerSummaries,
            sourceSummaries,
            alerts
        );
    }

    private static TerminalSnapshot CreateUnavailableSnapshot(string? currentLocationName, string detail)
    {
        string scopeText = string.IsNullOrWhiteSpace(currentLocationName)
            ? "Loaded farm snapshot unavailable"
            : $"Loaded farm snapshot unavailable while standing in {currentLocationName}";

        string refreshStatusText = "No PowerGrid data loaded";
        TerminalAlertSummary alert = new("Warning", "PowerGrid unavailable", detail);

        return new TerminalSnapshot(
            scopeText,
            refreshStatusText,
            "Farm Terminal could not read PowerGrid snapshots.",
            new[]
            {
                new TerminalSummaryCard("PowerGrid", "Unavailable", "Farm Terminal needs the PowerGrid API to populate its read-only dashboard.")
            },
            Array.Empty<TerminalNetworkSummary>(),
            Array.Empty<TerminalConsumerSummary>(),
            Array.Empty<TerminalSourceSummary>(),
            new[] { alert }
        );
    }

    private static string BuildOverviewSummary(
        IReadOnlyList<PowerNetworkSnapshot> networks,
        IReadOnlyList<PowerConsumerSnapshot> consumers,
        IReadOnlyList<PowerGeneratorSnapshot> generators,
        IReadOnlyList<PowerBatterySnapshot> batteries,
        IReadOnlyList<TerminalAlertSummary> alerts)
    {
        if (networks.Count == 0)
            return "No loaded power networks were reported by PowerGrid.";

        int activeConsumers = consumers.Count(consumer => consumer.IsProcessing && consumer.IsPowered);
        int unpoweredConsumers = consumers.Count(consumer => !consumer.IsPowered);
        int onlineGenerators = generators.Count(generator => generator.IsOnline);
        int totalStored = batteries.Sum(battery => battery.Charge);
        int totalCapacity = batteries.Sum(battery => battery.Capacity);

        return $"{networks.Count} network(s), {activeConsumers} active consumer(s), {unpoweredConsumers} unpowered consumer(s), {onlineGenerators} online source(s), {totalStored}/{Math.Max(totalCapacity, 0)} EU stored, {alerts.Count} alert(s).";
    }

    private static List<TerminalSummaryCard> BuildOverviewCards(
        IReadOnlyList<PowerNetworkSnapshot> networks,
        IReadOnlyList<PowerConsumerSnapshot> consumers,
        IReadOnlyList<PowerGeneratorSnapshot> generators,
        IReadOnlyList<PowerBatterySnapshot> batteries,
        IReadOnlyList<TerminalAlertSummary> alerts)
    {
        int totalGeneration = networks.Sum(network => network.TotalGenerationPerTick);
        int totalDemand = networks.Sum(network => network.TotalDemandPerTick);
        int totalStored = batteries.Sum(battery => battery.Charge);
        int totalCapacity = batteries.Sum(battery => battery.Capacity);
        int activeConsumers = consumers.Count(consumer => consumer.IsProcessing && consumer.IsPowered);
        int idleConsumers = consumers.Count(consumer => !consumer.IsProcessing && consumer.IsPowered);
        int unpoweredConsumers = consumers.Count(consumer => !consumer.IsPowered);
        int onlineGenerators = generators.Count(generator => generator.IsOnline);

        return new List<TerminalSummaryCard>
        {
            new("Networks", networks.Count.ToString(), $"{networks.Count(network => network.IsCrossLocation)} conduit-linked across locations."),
            new("Power", $"{totalGeneration} / {totalDemand} EU/tick", $"{onlineGenerators} online source(s) feeding {consumers.Count} consumer(s)."),
            new("Consumers", $"{activeConsumers} active", $"{idleConsumers} idle, {unpoweredConsumers} unpowered."),
            new("Storage", totalCapacity > 0 ? $"{totalStored} / {totalCapacity} EU" : "No batteries", $"{batteries.Count} battery node(s) reported."),
            new("Alerts", alerts.Count.ToString(), alerts.Count == 0 ? "No warnings derived from the latest snapshot." : "Review the Alerts module for derived warnings.")
        };
    }

    private static List<TerminalNetworkSummary> BuildNetworkSummaries(IReadOnlyList<PowerNetworkSnapshot> networks)
    {
        return networks.Select(network => new TerminalNetworkSummary(
            $"Network {network.NetworkId}",
            string.Join(", ", network.LocationNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)),
            $"{network.GeneratorCount} generator(s), {network.BatteryCount} battery node(s), {network.ConsumerCount} consumer(s), {network.CableCount} cable tile(s), {network.ConduitCount} conduit endpoint(s).",
            $"Generation {network.TotalGenerationPerTick} EU/tick, demand {network.TotalDemandPerTick} EU/tick, last tick {network.LastTickGenerated} generated / {network.LastTickConsumed} consumed, throughput cap {network.CableThroughputCap}.",
            network.TotalBatteryCapacity > 0
                ? $"{network.TotalStoredEU}/{network.TotalBatteryCapacity} EU stored, {network.LastTickFromBatteries} drained and {network.LastTickStoredInBatteries} stored last tick."
                : "No batteries reported in this network."
        )).ToList();
    }

    private static List<TerminalConsumerSummary> BuildConsumerSummaries(IReadOnlyList<PowerConsumerSnapshot> consumers)
    {
        return consumers.Select(consumer => new TerminalConsumerSummary(
            consumer.DisplayName,
            GetConsumerStatus(consumer),
            $"{consumer.LocationName} @ ({consumer.TileX}, {consumer.TileY})",
            $"Demand {consumer.DemandPerTick} EU/tick",
            $"Allocated {consumer.EUAllocated} EU, speedup {consumer.SpeedupFraction:P0}",
            consumer.IsProcessing
                ? $"Processing with {consumer.MinutesRemaining} in-game minute(s) remaining."
                : consumer.IsPowered
                    ? "Powered and idle."
                    : "Not receiving power."
        )).ToList();
    }

    private static List<TerminalSourceSummary> BuildSourceSummaries(
        IReadOnlyList<PowerGeneratorSnapshot> generators,
        IReadOnlyList<PowerBatterySnapshot> batteries)
    {
        List<TerminalSourceSummary> summaries = generators
            .Select(generator => new TerminalSourceSummary(
                generator.DisplayName,
                "Generator",
                generator.IsOnline ? "Online" : "Offline",
                $"{generator.LocationName} @ ({generator.TileX}, {generator.TileY})",
                $"Rated {generator.GenerationPerTick} EU/tick, generated {generator.GeneratedThisTick} last tick.",
                generator.RequiresFuel
                    ? $"{generator.FuelTicksRemaining} fuel tick(s) remaining."
                    : "No fuel required."
            ))
            .ToList();

        summaries.AddRange(batteries.Select(battery => new TerminalSourceSummary(
            battery.DisplayName,
            "Battery",
            $"{battery.ChargePercent:P0} charged",
            $"{battery.LocationName} @ ({battery.TileX}, {battery.TileY})",
            $"{battery.Charge}/{battery.Capacity} EU stored.",
            $"Last tick: {battery.StoredThisTick} stored, {battery.DrainedThisTick} drained."
        )));

        return summaries
            .OrderBy(summary => summary.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.LocationText, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<TerminalAlertSummary> BuildAlerts(
        IReadOnlyList<PowerNetworkSnapshot> networks,
        IReadOnlyList<PowerConsumerSnapshot> consumers)
    {
        List<TerminalAlertSummary> alerts = new();

        if (networks.Count == 0)
        {
            alerts.Add(new TerminalAlertSummary(
                "Warning",
                "No loaded networks",
                "PowerGrid did not report any loaded networks for the current world state."
            ));
            return alerts;
        }

        int unpoweredConsumers = consumers.Count(consumer => !consumer.IsPowered);
        if (unpoweredConsumers > 0)
        {
            alerts.Add(new TerminalAlertSummary(
                "Warning",
                "Unpowered consumers detected",
                $"{unpoweredConsumers} consumer(s) were unpowered in the latest snapshot."
            ));
        }

        foreach (PowerNetworkSnapshot network in networks)
        {
            if (network.TotalDemandPerTick > 0 && network.LastTickConsumed < network.TotalDemandPerTick)
            {
                alerts.Add(new TerminalAlertSummary(
                    "Warning",
                    $"Network {network.NetworkId} under-supplied",
                    $"Demand is {network.TotalDemandPerTick} EU/tick but only {network.LastTickConsumed} EU were consumed on the last tick."
                ));
            }

            if (network.TotalBatteryCapacity > 0 && network.TotalStoredEU == 0)
            {
                alerts.Add(new TerminalAlertSummary(
                    "Info",
                    $"Network {network.NetworkId} battery reserve empty",
                    "This network has batteries but no stored reserve at the current snapshot."
                ));
            }
            else if (network.TotalBatteryCapacity > 0 && network.TotalStoredEU <= Math.Max(1, network.TotalBatteryCapacity / 10))
            {
                alerts.Add(new TerminalAlertSummary(
                    "Info",
                    $"Network {network.NetworkId} battery reserve low",
                    $"{network.TotalStoredEU}/{network.TotalBatteryCapacity} EU remain in this network's batteries."
                ));
            }
        }

        return alerts;
    }

    private static int GetConsumerSortOrder(PowerConsumerSnapshot consumer)
    {
        if (!consumer.IsPowered)
            return 0;
        if (consumer.IsProcessing)
            return 1;
        return 2;
    }

    private static string GetConsumerStatus(PowerConsumerSnapshot consumer)
    {
        if (!consumer.IsPowered)
            return "Unpowered";
        if (consumer.IsProcessing)
            return "Active";
        return "Idle";
    }

    private static string FormatTime(int timeOfDay)
    {
        int normalized = Math.Max(0, timeOfDay);
        int hours = normalized / 100;
        int minutes = normalized % 100;
        return $"{hours:00}:{minutes:00}";
    }
}
