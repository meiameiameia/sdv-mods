using System.Globalization;
using System.Text;
using PowerGrid.Balancer.Core;

internal static class ResourcePressureReport
{
    public static void Write(string outputPath, BalanceConfig config, IReadOnlyList<LoadoutPlan> loadouts)
    {
        Directory.CreateDirectory(outputPath);
        IReadOnlyList<ResourcePressure> pressure = Analyze(config, loadouts);

        File.WriteAllText(Path.Combine(outputPath, "resource-pressure.md"), RenderMarkdown(pressure));
        File.WriteAllText(Path.Combine(outputPath, "resource-pressure.csv"), RenderCsv(pressure));
    }

    public static string RenderConsole(BalanceConfig config, IReadOnlyList<LoadoutPlan> loadouts)
    {
        return RenderMarkdown(Analyze(config, loadouts));
    }

    public static IReadOnlyList<ResourcePressure> Analyze(BalanceConfig config, IReadOnlyList<LoadoutPlan> loadouts)
    {
        List<LoadoutPlanReport.ResourceSnapshot> snapshots = loadouts
            .Select(loadout => LoadoutPlanReport.AnalyzeResources(config, loadout))
            .ToList();

        Dictionary<string, MutableResourcePressure> resources = new(StringComparer.OrdinalIgnoreCase);

        foreach (LoadoutPlanReport.ResourceSnapshot snapshot in snapshots)
        {
            foreach ((string resource, int needed) in snapshot.Needed)
            {
                MutableResourcePressure pressure = GetOrCreate(resources, resource);
                pressure.TotalNeeded += needed;
                snapshot.Stockpile.TryGetValue(resource, out int stockpile);
                pressure.TotalStockpile += stockpile;
                pressure.LoadoutsUsing.Add(snapshot.LoadoutName);
            }

            foreach ((string source, IReadOnlyDictionary<string, int> sourceCosts) in snapshot.CostBySource)
            {
                foreach ((string resource, int amount) in sourceCosts)
                {
                    MutableResourcePressure pressure = GetOrCreate(resources, resource);
                    pressure.AddSource(source, amount);
                }
            }

            foreach ((string resource, int gap) in snapshot.Gaps)
            {
                MutableResourcePressure pressure = GetOrCreate(resources, resource);
                pressure.TotalGap += gap;
                pressure.BlockedLoadouts.Add(snapshot.LoadoutName);
                if (gap > pressure.WorstGap)
                {
                    pressure.WorstGap = gap;
                    pressure.WorstLoadout = snapshot.LoadoutName;
                }
            }
        }

        return resources.Values
            .Select(pressure => pressure.ToResourcePressure(loadouts.Count))
            .OrderByDescending(pressure => pressure.TotalGap)
            .ThenByDescending(pressure => pressure.BlockedLoadoutCount)
            .ThenByDescending(pressure => pressure.TotalNeeded)
            .ThenBy(pressure => pressure.Resource, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static MutableResourcePressure GetOrCreate(Dictionary<string, MutableResourcePressure> resources, string resource)
    {
        if (resources.TryGetValue(resource, out MutableResourcePressure? pressure))
            return pressure;

        pressure = new MutableResourcePressure(resource);
        resources[resource] = pressure;
        return pressure;
    }

    private static string RenderMarkdown(IReadOnlyList<ResourcePressure> pressure)
    {
        List<string> lines = new()
        {
            "# PowerGrid Resource Pressure",
            "",
            "Ranks ingredients by how often they block planned loadouts. Use this to decide whether a recipe or fuel loop is too grindy before changing runtime balance.",
            "",
            "| Resource | Signal | Needed | Stockpile | Gap | Main source | Loadouts blocked | Worst loadout |",
            "| --- | --- | ---: | ---: | ---: | --- | ---: | --- |"
        };

        foreach (ResourcePressure resource in pressure)
        {
            lines.Add($"| {EscapeMarkdown(resource.Resource)} | {resource.Signal} | {resource.TotalNeeded} | {resource.TotalStockpile} | {resource.TotalGap} | {EscapeMarkdown(resource.PrimarySource)} | {resource.BlockedLoadoutCount}/{resource.TotalLoadouts} | {EscapeMarkdown(resource.WorstLoadout)} |");
        }

        lines.Add("");
        lines.Add("## Balance Signals");
        lines.Add("");

        foreach (ResourcePressure resource in pressure.Where(resource => resource.TotalGap > 0).Take(8))
            lines.Add($"- {resource.Resource}: {resource.Signal.ToLowerInvariant()} pressure, short by {resource.TotalGap}; mostly from {resource.PrimarySource}; worst case is {resource.WorstLoadout}.");

        if (pressure.All(resource => resource.TotalGap <= 0))
            lines.Add("- No resource gaps under the current loadout assumptions.");

        lines.Add("");
        lines.Add("## How To Read This");
        lines.Add("");
        lines.Add("- `Needed` is the total recipe and reserve-fuel cost across the loadout suite.");
        lines.Add("- `Stockpile` is the total benchmark stockpile across those loadouts.");
        lines.Add("- `Gap` is the total shortage after each loadout spends its own stockpile.");
        lines.Add("- `Main source` shows whether the pressure comes mostly from machines, generators, cables, batteries, conduits, or fuel reserve.");
        lines.Add("- High pressure means the resource deserves balance review, not an automatic recipe change.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderCsv(IReadOnlyList<ResourcePressure> pressure)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Resource", "Signal", "TotalNeeded", "TotalStockpile", "TotalGap", "PrimarySource", "PrimarySourceAmount", "LoadoutsUsing", "BlockedLoadouts", "TotalLoadouts", "WorstLoadout", "WorstGap"));

        foreach (ResourcePressure resource in pressure)
        {
            csv.AppendLine(CsvLine(
                resource.Resource,
                resource.Signal,
                resource.TotalNeeded,
                resource.TotalStockpile,
                resource.TotalGap,
                resource.PrimarySource,
                resource.PrimarySourceAmount,
                resource.LoadoutsUsing,
                resource.BlockedLoadoutCount,
                resource.TotalLoadouts,
                resource.WorstLoadout,
                resource.WorstGap));
        }

        return csv.ToString();
    }

    private static string Classify(MutableResourcePressure pressure, int totalLoadouts)
    {
        if (pressure.TotalGap <= 0)
            return "OK";

        double blockedRatio = (double)pressure.BlockedLoadouts.Count / Math.Max(1, totalLoadouts);
        double shortageRatio = (double)pressure.TotalGap / Math.Max(1, pressure.TotalNeeded);

        if (blockedRatio >= 0.5d || shortageRatio >= 0.5d)
            return "Severe";
        if (blockedRatio >= 0.25d || shortageRatio >= 0.2d)
            return "Watch";
        return "Minor";
    }

    private static string CsvLine(params object?[] values)
    {
        return string.Join(",", values.Select(CsvValue));
    }

    private static string CsvValue(object? value)
    {
        string text = value switch
        {
            null => "",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
            _ => value.ToString() ?? ""
        };

        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
            return $"\"{text.Replace("\"", "\"\"")}\"";

        return text;
    }

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private sealed class MutableResourcePressure(string resource)
    {
        public string Resource { get; } = resource;
        public int TotalNeeded { get; set; }
        public int TotalStockpile { get; set; }
        public int TotalGap { get; set; }
        public int WorstGap { get; set; }
        public string WorstLoadout { get; set; } = "";
        public HashSet<string> LoadoutsUsing { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> BlockedLoadouts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> SourceTotals { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void AddSource(string source, int amount)
        {
            SourceTotals.TryGetValue(source, out int current);
            SourceTotals[source] = current + amount;
        }

        public ResourcePressure ToResourcePressure(int totalLoadouts)
        {
            KeyValuePair<string, int> primarySource = SourceTotals
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            return new ResourcePressure(
                Resource,
                Classify(this, totalLoadouts),
                TotalNeeded,
                TotalStockpile,
                TotalGap,
                string.IsNullOrWhiteSpace(primarySource.Key) ? "-" : primarySource.Key,
                primarySource.Value,
                LoadoutsUsing.Count,
                BlockedLoadouts.Count,
                totalLoadouts,
                string.IsNullOrWhiteSpace(WorstLoadout) ? "-" : WorstLoadout,
                WorstGap);
        }
    }

    public sealed record ResourcePressure(
        string Resource,
        string Signal,
        int TotalNeeded,
        int TotalStockpile,
        int TotalGap,
        string PrimarySource,
        int PrimarySourceAmount,
        int LoadoutsUsing,
        int BlockedLoadoutCount,
        int TotalLoadouts,
        string WorstLoadout,
        int WorstGap);
}
