namespace Darth.FarmTerminal.Domain;

internal sealed record TerminalSnapshot(
    string ScopeText,
    string RefreshStatusText,
    string OverviewSummaryText,
    IReadOnlyList<TerminalSummaryCard> OverviewCards,
    IReadOnlyList<TerminalNetworkSummary> Networks,
    IReadOnlyList<TerminalConsumerSummary> Consumers,
    IReadOnlyList<TerminalSourceSummary> Sources,
    IReadOnlyList<TerminalAlertSummary> Alerts
);

internal sealed record TerminalSummaryCard(string Title, string Value, string Detail);

internal sealed record TerminalNetworkSummary(
    string Title,
    string LocationsText,
    string TopologyText,
    string FlowText,
    string StorageText
);

internal sealed record TerminalConsumerSummary(
    string DisplayName,
    string StatusText,
    string LocationText,
    string DemandText,
    string AllocationText,
    string DetailText
);

internal sealed record TerminalSourceSummary(
    string DisplayName,
    string SourceType,
    string StatusText,
    string LocationText,
    string PrimaryMetricText,
    string SecondaryMetricText
);

internal sealed record TerminalAlertSummary(string Severity, string Title, string Detail)
{
    public string SeverityBadgeText => $"[{Severity}]";
}
