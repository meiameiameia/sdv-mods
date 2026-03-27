namespace Darth.PerfectionAdvisor;

internal sealed class PerfectionAdvisorSnapshot
{
    public string ModeLabel { get; }
    public IReadOnlyList<string> OverviewLines { get; }
    public IReadOnlyList<string> ProgressLines { get; }
    public IReadOnlyList<string> FishLines { get; }
    public IReadOnlyList<string> BlockerLines { get; }
    public IReadOnlyList<string> FriendshipLines { get; }
    public IReadOnlyList<string> SeasonalLines { get; }
    public IReadOnlyList<string> TodayLines { get; }
    public IReadOnlyList<string> DetailLines { get; }

    public PerfectionAdvisorSnapshot(
        string modeLabel,
        IReadOnlyList<string> overviewLines,
        IReadOnlyList<string> progressLines,
        IReadOnlyList<string> fishLines,
        IReadOnlyList<string> blockerLines,
        IReadOnlyList<string> friendshipLines,
        IReadOnlyList<string> seasonalLines,
        IReadOnlyList<string> todayLines,
        IReadOnlyList<string> detailLines)
    {
        this.ModeLabel = modeLabel;
        this.OverviewLines = overviewLines;
        this.ProgressLines = progressLines;
        this.FishLines = fishLines;
        this.BlockerLines = blockerLines;
        this.FriendshipLines = friendshipLines;
        this.SeasonalLines = seasonalLines;
        this.TodayLines = todayLines;
        this.DetailLines = detailLines;
    }
}
