using Darth.FarmTerminal.Domain;
using Darth.FarmTerminal.Integrations.PowerGrid;

namespace Darth.FarmTerminal.UI.ViewModels;

internal sealed class TerminalViewModel : BindableBase
{
    private readonly Func<string?> currentLocationNameProvider;
    private readonly PowerGridTerminalQueryService queryService;
    private readonly IReadOnlyList<TerminalTabViewModel> tabs;
    private string activeModuleId = TerminalModule.Overview;
    private string headerStatusText = "Waiting for first refresh.";
    private string scopeText = "No snapshot loaded yet.";
    private string refreshStatusText = "Refresh to load a PowerGrid snapshot.";
    private string overviewSummaryText = "Open the terminal and refresh to load a PowerGrid snapshot.";
    private IReadOnlyList<TerminalSummaryCard> overviewCards = Array.Empty<TerminalSummaryCard>();
    private IReadOnlyList<TerminalNetworkSummary> networks = Array.Empty<TerminalNetworkSummary>();
    private IReadOnlyList<TerminalConsumerSummary> consumers = Array.Empty<TerminalConsumerSummary>();
    private IReadOnlyList<TerminalSourceSummary> sources = Array.Empty<TerminalSourceSummary>();
    private IReadOnlyList<TerminalAlertSummary> alerts = Array.Empty<TerminalAlertSummary>();

    public TerminalViewModel(PowerGridTerminalQueryService queryService, Func<string?> currentLocationNameProvider)
    {
        this.queryService = queryService;
        this.currentLocationNameProvider = currentLocationNameProvider;
        this.tabs = new[]
        {
            new TerminalTabViewModel(TerminalModule.Overview, "Overview", "High-level farm snapshot", active: true),
            new TerminalTabViewModel(TerminalModule.Power, "Power", "Network totals and topology"),
            new TerminalTabViewModel(TerminalModule.Consumers, "Consumers", "Powered, idle, and unpowered machines"),
            new TerminalTabViewModel(TerminalModule.Sources, "Sources", "Generators and batteries"),
            new TerminalTabViewModel(TerminalModule.Alerts, "Alerts", "Derived warnings from snapshot data")
        };
    }

    public string TitleText => "Farm Terminal";

    public string SubtitleText => "Read-only PowerGrid dashboard";

    public string HeaderStatusText
    {
        get => this.headerStatusText;
        private set => this.SetProperty(ref this.headerStatusText, value);
    }

    public IReadOnlyList<TerminalTabViewModel> Tabs => this.tabs;

    public string ActiveModuleId
    {
        get => this.activeModuleId;
        private set
        {
            if (!this.SetProperty(ref this.activeModuleId, value))
                return;

            this.OnPropertyChanged(nameof(this.ActiveSectionTitle));
            this.OnPropertyChanged(nameof(this.ActiveSectionSummaryText));
        }
    }

    public string ActiveSectionTitle => this.ActiveModuleId;

    public string ActiveSectionSummaryText => this.ActiveModuleId switch
    {
        TerminalModule.Overview => this.OverviewCards.Count == 0
            ? "High-level status cards appear here after a successful refresh."
            : $"{this.OverviewCards.Count} overview card(s) summarizing the latest PowerGrid snapshot.",
        TerminalModule.Power => this.Networks.Count == 0
            ? "No network summary is available yet."
            : $"{this.Networks.Count} network summary card(s) loaded from the latest PowerGrid snapshot.",
        TerminalModule.Consumers => BuildConsumerModuleSummary(),
        TerminalModule.Sources => BuildSourceModuleSummary(),
        TerminalModule.Alerts => BuildAlertsModuleSummary(),
        _ => "Read-only module view."
    };

    public string ScopeText
    {
        get => this.scopeText;
        private set => this.SetProperty(ref this.scopeText, value);
    }

    public string RefreshStatusText
    {
        get => this.refreshStatusText;
        private set => this.SetProperty(ref this.refreshStatusText, value);
    }

    public string OverviewSummaryText
    {
        get => this.overviewSummaryText;
        private set => this.SetProperty(ref this.overviewSummaryText, value);
    }

    public IReadOnlyList<TerminalSummaryCard> OverviewCards
    {
        get => this.overviewCards;
        private set
        {
            if (this.SetProperty(ref this.overviewCards, value))
                this.OnPropertyChanged(nameof(this.OverviewEmptyText));
        }
    }

    public IReadOnlyList<TerminalNetworkSummary> Networks
    {
        get => this.networks;
        private set
        {
            if (this.SetProperty(ref this.networks, value))
            {
                this.OnPropertyChanged(nameof(this.PowerEmptyText));
                this.OnPropertyChanged(nameof(this.ShowPowerEmptyText));
            }
        }
    }

    public IReadOnlyList<TerminalConsumerSummary> Consumers
    {
        get => this.consumers;
        private set
        {
            if (this.SetProperty(ref this.consumers, value))
            {
                this.OnPropertyChanged(nameof(this.ConsumersEmptyText));
                this.OnPropertyChanged(nameof(this.ShowConsumersEmptyText));
            }
        }
    }

    public IReadOnlyList<TerminalSourceSummary> Sources
    {
        get => this.sources;
        private set
        {
            if (this.SetProperty(ref this.sources, value))
            {
                this.OnPropertyChanged(nameof(this.SourcesEmptyText));
                this.OnPropertyChanged(nameof(this.ShowSourcesEmptyText));
            }
        }
    }

    public IReadOnlyList<TerminalAlertSummary> Alerts
    {
        get => this.alerts;
        private set
        {
            if (this.SetProperty(ref this.alerts, value))
            {
                this.OnPropertyChanged(nameof(this.AlertsEmptyText));
                this.OnPropertyChanged(nameof(this.ShowAlertsEmptyText));
            }
        }
    }

    public string OverviewEmptyText => this.OverviewCards.Count == 0
        ? "No overview cards are available yet. Refresh after the next PowerGrid tick to populate the dashboard."
        : "";

    public string PowerEmptyText => this.Networks.Count == 0
        ? "No loaded power networks were reported. Make sure grid equipment is loaded, then refresh after the next PowerGrid tick."
        : "";

    public string ConsumersEmptyText => this.Consumers.Count == 0
        ? "No consumer snapshots were reported. Load powered machines or refresh after the next PowerGrid tick."
        : "";

    public string SourcesEmptyText => this.Sources.Count == 0
        ? "No generators or batteries were reported. Place sources on a loaded map and refresh."
        : "";

    public string AlertsEmptyText => this.Alerts.Count == 0
        ? "All clear. No alerts were derived from the latest snapshot."
        : "";

    public bool ShowOverviewEmptyText => this.OverviewCards.Count == 0;

    public bool ShowPowerEmptyText => this.Networks.Count == 0;

    public bool ShowConsumersEmptyText => this.Consumers.Count == 0;

    public bool ShowSourcesEmptyText => this.Sources.Count == 0;

    public bool ShowAlertsEmptyText => this.Alerts.Count == 0;

    public void Refresh()
    {
        TerminalSnapshot snapshot = this.queryService.CreateSnapshot(this.currentLocationNameProvider());
        this.HeaderStatusText = $"{snapshot.RefreshStatusText} | {snapshot.ScopeText}";
        this.ScopeText = snapshot.ScopeText;
        this.RefreshStatusText = snapshot.RefreshStatusText;
        this.OverviewSummaryText = snapshot.OverviewSummaryText;
        this.OverviewCards = snapshot.OverviewCards;
        this.Networks = snapshot.Networks;
        this.Consumers = snapshot.Consumers;
        this.Sources = snapshot.Sources;
        this.Alerts = snapshot.Alerts;
        this.OnPropertyChanged(nameof(this.ActiveSectionSummaryText));
    }

    public void OnTabActivated(string id)
    {
        this.ActiveModuleId = id;

        foreach (TerminalTabViewModel tab in this.Tabs)
            tab.Active = string.Equals(tab.Id, id, StringComparison.Ordinal);
    }

    private string BuildConsumerModuleSummary()
    {
        int waiting = this.Consumers.Count(consumer => string.Equals(consumer.StatusText, "Waiting For Power", StringComparison.Ordinal));
        int active = this.Consumers.Count(consumer => string.Equals(consumer.StatusText, "Powered / Active", StringComparison.Ordinal));
        int standby = this.Consumers.Count(consumer => string.Equals(consumer.StatusText, "Powered / Ready", StringComparison.Ordinal));
        int idleUnpowered = this.Consumers.Count(consumer => string.Equals(consumer.StatusText, "Idle / Unpowered", StringComparison.Ordinal));

        return this.Consumers.Count == 0
            ? "No consumer details are available yet."
            : $"{waiting} waiting for power, {active} active, {standby} standby, {idleUnpowered} idle and unpowered consumer(s).";
    }

    private string BuildSourceModuleSummary()
    {
        int generators = this.Sources.Count(source => string.Equals(source.SourceType, "Generator", StringComparison.Ordinal));
        int batteries = this.Sources.Count(source => string.Equals(source.SourceType, "Battery", StringComparison.Ordinal));
        int onlineGenerators = this.Sources.Count(source =>
            string.Equals(source.SourceType, "Generator", StringComparison.Ordinal)
            && string.Equals(source.StatusText, "Online", StringComparison.Ordinal));

        return this.Sources.Count == 0
            ? "No source details are available yet."
            : $"{generators} generator(s), {batteries} battery node(s), {onlineGenerators} generator(s) online.";
    }

    private string BuildAlertsModuleSummary()
    {
        int warnings = this.Alerts.Count(alert => string.Equals(alert.Severity, "Warning", StringComparison.Ordinal));
        int infos = this.Alerts.Count(alert => string.Equals(alert.Severity, "Info", StringComparison.Ordinal));

        return this.Alerts.Count == 0
            ? "No warnings or low-reserve notices are active."
            : $"{warnings} warning(s), {infos} info notice(s).";
    }
}
