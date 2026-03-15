using Darth.FarmTerminal.Domain;
using Darth.FarmTerminal.Integrations.PowerGrid;

namespace Darth.FarmTerminal.UI.ViewModels;

internal sealed class TerminalViewModel : BindableBase
{
    private readonly Func<string?> currentLocationNameProvider;
    private readonly PowerGridTerminalQueryService queryService;
    private readonly IReadOnlyList<TerminalTabViewModel> tabs;
    private string activeModuleId = TerminalModule.Overview;
    private string scopeText = "Loaded farm snapshot";
    private string refreshStatusText = "Not yet refreshed.";
    private string overviewSummaryText = "Open the terminal to load a PowerGrid snapshot.";
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

    public IReadOnlyList<TerminalTabViewModel> Tabs => this.tabs;

    public string ActiveModuleId
    {
        get => this.activeModuleId;
        private set
        {
            if (!this.SetProperty(ref this.activeModuleId, value))
                return;

            this.OnPropertyChanged(nameof(this.ActiveSectionTitle));
        }
    }

    public string ActiveSectionTitle => this.ActiveModuleId;

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

    public string OverviewEmptyText => this.OverviewCards.Count == 0 ? "No overview cards available." : "";

    public string PowerEmptyText => this.Networks.Count == 0 ? "No loaded power networks reported by PowerGrid." : "";

    public string ConsumersEmptyText => this.Consumers.Count == 0 ? "No consumers reported by PowerGrid." : "";

    public string SourcesEmptyText => this.Sources.Count == 0 ? "No generators or batteries reported by PowerGrid." : "";

    public string AlertsEmptyText => this.Alerts.Count == 0 ? "No derived alerts for the latest snapshot." : "";

    public bool ShowPowerEmptyText => this.Networks.Count == 0;

    public bool ShowConsumersEmptyText => this.Consumers.Count == 0;

    public bool ShowSourcesEmptyText => this.Sources.Count == 0;

    public bool ShowAlertsEmptyText => this.Alerts.Count == 0;

    public void Refresh()
    {
        TerminalSnapshot snapshot = this.queryService.CreateSnapshot(this.currentLocationNameProvider());
        this.ScopeText = snapshot.ScopeText;
        this.RefreshStatusText = snapshot.RefreshStatusText;
        this.OverviewSummaryText = snapshot.OverviewSummaryText;
        this.OverviewCards = snapshot.OverviewCards;
        this.Networks = snapshot.Networks;
        this.Consumers = snapshot.Consumers;
        this.Sources = snapshot.Sources;
        this.Alerts = snapshot.Alerts;
    }

    public void OnTabActivated(string id)
    {
        this.ActiveModuleId = id;

        foreach (TerminalTabViewModel tab in this.Tabs)
            tab.Active = string.Equals(tab.Id, id, StringComparison.Ordinal);
    }
}
