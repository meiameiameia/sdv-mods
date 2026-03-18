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
            .ThenBy(consumer => consumer.NetworkId)
            .ThenBy(consumer => consumer.LocationName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(consumer => consumer.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(consumer => consumer.TileY)
            .ThenBy(consumer => consumer.TileX)
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
            ? "Showing all loaded PowerGrid networks"
            : $"Showing all loaded PowerGrid networks while standing in {currentLocationName}";

        string refreshStatusText = $"Refreshed just now at {FormatTime(Game1.timeOfDay)} on {Game1.currentSeason} {Game1.dayOfMonth}";
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
            ? "Farm snapshot unavailable"
            : $"Farm snapshot unavailable while standing in {currentLocationName}";

        string refreshStatusText = "No PowerGrid snapshot available";
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
            return "No loaded power networks were reported. Place or load PowerGrid equipment, wait for the next 10-minute tick, and refresh.";

        int activeConsumers = consumers.Count(IsConsumerActive);
        int standbyConsumers = consumers.Count(IsConsumerStandby);
        int waitingConsumers = consumers.Count(IsConsumerWaitingForPower);
        int idleUnpoweredConsumers = consumers.Count(IsConsumerIdleAndUnpowered);
        int onlineGenerators = generators.Count(generator => generator.IsOnline);
        int totalStored = batteries.Sum(battery => battery.Charge);
        int totalCapacity = batteries.Sum(battery => battery.Capacity);

        return $"{networks.Count} network(s) online, {activeConsumers} active consumer(s), {standbyConsumers} standby consumer(s), {waitingConsumers} waiting for power, {idleUnpoweredConsumers} idle and unpowered, {onlineGenerators} online source(s), {totalStored}/{Math.Max(totalCapacity, 0)} EU stored, {alerts.Count} alert(s).";
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
        int activeConsumers = consumers.Count(IsConsumerActive);
        int standbyConsumers = consumers.Count(IsConsumerStandby);
        int waitingConsumers = consumers.Count(IsConsumerWaitingForPower);
        int idleUnpoweredConsumers = consumers.Count(IsConsumerIdleAndUnpowered);
        int onlineGenerators = generators.Count(generator => generator.IsOnline);

        return new List<TerminalSummaryCard>
        {
            new("Networks", networks.Count.ToString(), $"{networks.Count(network => network.IsCrossLocation)} conduit-linked across locations."),
            new("Power", $"{totalGeneration} / {totalDemand} EU/tick", $"{onlineGenerators} online source(s) feeding {consumers.Count} consumer(s)."),
            new("Consumers", $"{activeConsumers} active", $"{standbyConsumers} standby, {waitingConsumers} waiting for power, {idleUnpoweredConsumers} idle and unpowered."),
            new("Storage", totalCapacity > 0 ? $"{totalStored} / {totalCapacity} EU" : "No batteries", $"{batteries.Count} battery node(s) reported."),
            new("Alerts", alerts.Count.ToString(), alerts.Count == 0 ? "All clear on the latest snapshot." : "Review the Alerts module for warnings and low-reserve notices.")
        };
    }

    private static List<TerminalNetworkSummary> BuildNetworkSummaries(IReadOnlyList<PowerNetworkSnapshot> networks)
    {
        return networks.Select(network => new TerminalNetworkSummary(
            $"Network {network.NetworkId}",
            string.Join(", ", network.LocationNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)),
            $"{network.GeneratorCount} generator(s), {network.BatteryCount} battery node(s), {network.ConsumerCount} consumer(s), {network.CableCount} cable tile(s), {network.ConduitCount} conduit endpoint(s).",
            $"Rated {network.TotalGenerationPerTick} EU/tick against {network.TotalDemandPerTick} EU/tick demand. Last tick: {network.LastTickGenerated} generated / {network.LastTickConsumed} consumed. Throughput cap {network.CableThroughputCap}.",
            network.TotalBatteryCapacity > 0
                ? $"{network.TotalStoredEU}/{network.TotalBatteryCapacity} EU stored. Last tick: {network.LastTickFromBatteries} drained from batteries, {network.LastTickStoredInBatteries} stored."
                : "No batteries reported in this network."
        )).ToList();
    }

    private static List<TerminalConsumerSummary> BuildConsumerSummaries(IReadOnlyList<PowerConsumerSnapshot> consumers)
    {
        return consumers.Select(consumer => new TerminalConsumerSummary(
            consumer.DisplayName,
            GetConsumerStatus(consumer),
            $"Network {consumer.NetworkId} · {consumer.LocationName} @ ({consumer.TileX}, {consumer.TileY})",
            $"Demand {consumer.DemandPerTick} EU/tick · Priority {consumer.Priority}",
            BuildConsumerAllocationText(consumer),
            BuildConsumerDetailText(consumer)
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
                $"Rated {generator.GenerationPerTick} EU/tick. Last tick: {generator.GeneratedThisTick} EU generated.",
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
                BuildUnpoweredConsumerDetail(consumers)
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

        return alerts
            .OrderBy(alert => GetAlertSortOrder(alert.Severity))
            .ThenBy(alert => alert.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int GetConsumerSortOrder(PowerConsumerSnapshot consumer)
    {
        if (IsConsumerWaitingForPower(consumer))
            return 0;
        if (IsConsumerActive(consumer))
            return 1;
        if (IsConsumerStandby(consumer))
            return 2;
        return 3;
    }

    private static string GetConsumerStatus(PowerConsumerSnapshot consumer)
    {
        if (IsConsumerWaitingForPower(consumer))
            return "Waiting For Power";
        if (IsConsumerActive(consumer))
            return "Powered / Active";
        if (IsConsumerStandby(consumer))
            return "Powered / Ready";
        return "Idle / Unpowered";
    }

    private static string BuildConsumerAllocationText(PowerConsumerSnapshot consumer)
    {
        if (!consumer.IsPowered)
            return $"Latest tick: {consumer.EUAllocated}/{consumer.DemandPerTick} EU allocated.";

        if (consumer.ProgressMode == "days")
            return $"Latest tick: {consumer.EUAllocated}/{consumer.DemandPerTick} EU, live speedup {consumer.SpeedupFraction:P0}";

        return $"Latest tick: {consumer.EUAllocated}/{consumer.DemandPerTick} EU, speedup {consumer.SpeedupFraction:P0}";
    }

    private static string BuildConsumerDetailText(PowerConsumerSnapshot consumer)
    {
        if (!string.IsNullOrWhiteSpace(consumer.ProgressText))
            return consumer.ProgressText;

        if (IsConsumerActive(consumer))
            return $"Powered on the latest snapshot and accelerating. About {consumer.MinutesRemaining} in-game minute(s) remain.";

        if (IsConsumerWaitingForPower(consumer))
            return $"Work is present, but the latest snapshot did not allocate power. About {consumer.MinutesRemaining} in-game minute(s) remain once power resumes.";

        return IsConsumerStandby(consumer)
            ? "Powered on the latest snapshot and ready for work."
            : "Idle on the latest snapshot and not receiving power.";
    }

    private static bool IsConsumerActive(PowerConsumerSnapshot consumer)
    {
        return consumer.IsPowered && consumer.IsProcessing;
    }

    private static bool IsConsumerStandby(PowerConsumerSnapshot consumer)
    {
        return consumer.IsPowered && !consumer.IsProcessing;
    }

    private static bool IsConsumerWaitingForPower(PowerConsumerSnapshot consumer)
    {
        return !consumer.IsPowered && consumer.IsProcessing;
    }

    private static bool IsConsumerIdleAndUnpowered(PowerConsumerSnapshot consumer)
    {
        return !consumer.IsPowered && !consumer.IsProcessing;
    }

    private static string BuildUnpoweredConsumerDetail(IReadOnlyList<PowerConsumerSnapshot> consumers)
    {
        PowerConsumerSnapshot[] sample = consumers
            .Where(consumer => !consumer.IsPowered)
            .Take(3)
            .ToArray();

        if (sample.Length == 0)
            return "One or more consumers were unpowered in the latest snapshot.";

        string sampleText = string.Join(
            ", ",
            sample.Select(consumer => $"{consumer.DisplayName} ({consumer.LocationName} {consumer.TileX},{consumer.TileY})"));

        return $"{consumers.Count(consumer => !consumer.IsPowered)} consumer(s) were unpowered in the latest snapshot: {sampleText}.";
    }

    private static int GetAlertSortOrder(string severity)
    {
        return severity switch
        {
            "Warning" => 0,
            "Info" => 1,
            _ => 2
        };
    }

    private static string FormatTime(int timeOfDay)
    {
        int normalized = Math.Max(0, timeOfDay);
        int hours = normalized / 100;
        int minutes = normalized % 100;
        return $"{hours:00}:{minutes:00}";
    }
}
