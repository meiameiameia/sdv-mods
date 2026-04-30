using System.Globalization;
using System.Text;
using PowerGrid.Balancer.Core;

internal static class ResourceSustainabilityReport
{
    public static void Write(
        string outputPath,
        BalanceConfig config,
        IReadOnlyList<LoadoutPlan> loadouts,
        ResourceBudgetProfile profile)
    {
        Directory.CreateDirectory(outputPath);
        IReadOnlyList<SustainabilityRow> rows = Analyze(config, loadouts, profile);

        File.WriteAllText(Path.Combine(outputPath, "resource-sustainability.md"), RenderMarkdown(profile, rows));
        File.WriteAllText(Path.Combine(outputPath, "resource-sustainability.csv"), RenderCsv(rows));
    }

    public static string RenderConsole(
        BalanceConfig config,
        IReadOnlyList<LoadoutPlan> loadouts,
        ResourceBudgetProfile profile)
    {
        return RenderMarkdown(profile, Analyze(config, loadouts, profile));
    }

    public static IReadOnlyList<SustainabilityRow> Analyze(
        BalanceConfig config,
        IReadOnlyList<LoadoutPlan> loadouts,
        ResourceBudgetProfile profile)
    {
        Dictionary<string, ResourceBudgetStage> stages = profile.Stages
            .ToDictionary(stage => stage.Name, StringComparer.OrdinalIgnoreCase);

        List<SustainabilityRow> rows = new();
        foreach (LoadoutPlan loadout in loadouts)
        {
            stages.TryGetValue(loadout.Name, out ResourceBudgetStage? stage);
            GeneratorDefinition generator = Resolve(config.Generators, loadout.Generator, "generator");
            LoadoutPlanReport.ResourceSnapshot snapshot = LoadoutPlanReport.AnalyzeResources(config, loadout);
            Dictionary<string, int> fuelReserve = snapshot.CostBySource.TryGetValue("Fuel Reserve", out Dictionary<string, int>? source)
                ? source
                : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            FlexibleFuelWeeklyBudget? flexibleFuelBudget = BuildFlexibleFuelWeeklyBudget(config, loadout, stage, generator);

            HashSet<string> resources = new(StringComparer.OrdinalIgnoreCase);
            foreach (string resource in snapshot.Needed.Keys)
                resources.Add(resource);
            if (stage is not null)
            {
                foreach (string resource in stage.Resources.Keys)
                    resources.Add(resource);
            }

            foreach (string resource in resources.OrderBy(resource => resource, StringComparer.OrdinalIgnoreCase))
            {
                snapshot.Needed.TryGetValue(resource, out int totalNeed);
                fuelReserve.TryGetValue(resource, out int fuelReserveNeed);
                int setupNeed = Math.Max(0, totalNeed - fuelReserveNeed);
                int weeklyNeed = flexibleFuelBudget is not null && flexibleFuelBudget.OptionResources.Contains(resource)
                    ? 0
                    : loadout.ReserveDays <= 0
                    ? 0
                    : (int)Math.Ceiling((double)fuelReserveNeed * 7d / loadout.ReserveDays);
                snapshot.Stockpile.TryGetValue(resource, out int stockpile);

                ResourceBudget? budget = null;
                stage?.Resources.TryGetValue(resource, out budget);

                int reserveFloor = budget?.ReserveFloor ?? 0;
                int setupAvailable = Math.Max(0, stockpile - reserveFloor);
                int setupGap = Math.Max(0, setupNeed - setupAvailable);
                double weeklyAllowed = budget is null
                    ? 0d
                    : budget.WeeklyIncome * Math.Clamp(budget.MaxPowerGridShare, 0d, 1d);
                double weeklyRatio = weeklyAllowed <= 0d
                    ? weeklyNeed > 0 ? double.PositiveInfinity : 0d
                    : weeklyNeed / weeklyAllowed;
                int weeklyGap = weeklyAllowed <= 0d
                    ? weeklyNeed
                    : Math.Max(0, (int)Math.Ceiling(weeklyNeed - weeklyAllowed));
                string signal = Classify(budget, setupNeed, setupGap, weeklyNeed, weeklyRatio);

                if (setupNeed <= 0 && weeklyNeed <= 0 && budget is null)
                    continue;

                rows.Add(new SustainabilityRow(
                    loadout.Name,
                    stage?.Name ?? "-",
                    resource,
                    signal,
                    setupNeed,
                    stockpile,
                    reserveFloor,
                    setupAvailable,
                    setupGap,
                    weeklyNeed,
                    budget?.WeeklyIncome ?? 0,
                    budget?.MaxPowerGridShare ?? 0d,
                    weeklyAllowed,
                    weeklyRatio,
                    weeklyGap,
                    budget?.Notes ?? ""));
            }

            if (flexibleFuelBudget is not null)
            {
                string signal = ClassifySynthetic(0, flexibleFuelBudget.WeeklyNeed, flexibleFuelBudget.WeeklyAllowed);
                rows.Add(new SustainabilityRow(
                    loadout.Name,
                    stage?.Name ?? "-",
                    flexibleFuelBudget.Name,
                    signal,
                    0,
                    0,
                    0,
                    0,
                    0,
                    flexibleFuelBudget.WeeklyNeed,
                    flexibleFuelBudget.WeeklyIncomeEquivalent,
                    flexibleFuelBudget.MaxPowerGridShareEquivalent,
                    flexibleFuelBudget.WeeklyAllowed,
                    flexibleFuelBudget.WeeklyRatio,
                    flexibleFuelBudget.WeeklyGap,
                    flexibleFuelBudget.Notes));
            }
        }

        return rows
            .OrderBy(row => SignalRank(row.Signal))
            .ThenByDescending(row => row.WeeklyGap)
            .ThenByDescending(row => row.SetupGap)
            .ThenBy(row => row.Loadout, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Resource, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Classify(ResourceBudget? budget, int setupNeed, int setupGap, int weeklyNeed, double weeklyRatio)
    {
        if (budget is null)
            return setupNeed > 0 || weeklyNeed > 0 ? "Unbudgeted" : "OK";

        if (weeklyRatio > 1d || setupGap > 0)
            return "Severe";
        if (weeklyRatio >= 0.75d)
            return "Watch";
        if (weeklyNeed > 0 || setupNeed > 0)
            return "OK";

        return "Unused";
    }

    private static string ClassifySynthetic(int setupGap, int weeklyNeed, double weeklyAllowed)
    {
        if (weeklyAllowed <= 0d)
            return weeklyNeed > 0 || setupGap > 0 ? "Unbudgeted" : "OK";

        double weeklyRatio = weeklyNeed / weeklyAllowed;
        if (weeklyRatio > 1d || setupGap > 0)
            return "Severe";
        if (weeklyRatio >= 0.75d)
            return "Watch";
        if (weeklyNeed > 0)
            return "OK";

        return "Unused";
    }

    private static FlexibleFuelWeeklyBudget? BuildFlexibleFuelWeeklyBudget(
        BalanceConfig config,
        LoadoutPlan loadout,
        ResourceBudgetStage? stage,
        GeneratorDefinition generator)
    {
        IReadOnlyList<string> fuelOptions = GetFuelOptions(generator);
        if (stage is null || fuelOptions.Count <= 1)
            return null;

        List<FuelDefinition> fuels = fuelOptions
            .Select(fuelId => Resolve(config.Fuels, fuelId, "fuel"))
            .ToList();
        if (fuels.Any(fuel => config.Recipes.TryGetValue(fuel.Name, out RecipeDefinition? recipe) && recipe.Ingredients.Count > 0))
            return null;

        int comfortGenerators = CalculateComfortGeneratorCount(config, loadout, generator);
        if (comfortGenerators <= 0)
            return null;

        FuelDefinition primaryFuel = fuels[0];
        int weeklyTicks = comfortGenerators * config.TicksPerDay * 7;
        int weeklyNeed = (int)Math.Ceiling((double)weeklyTicks / Math.Max(1, primaryFuel.TicksPerUnit));

        double combinedAllowedTicks = 0d;
        double combinedWeeklyIncomeEquivalent = 0d;
        double combinedShareEquivalent = 0d;
        List<string> availableBudgets = new();

        foreach (FuelDefinition fuel in fuels)
        {
            if (!stage.Resources.TryGetValue(fuel.Name, out ResourceBudget? budget))
                continue;

            double weeklyAllowedUnits = budget.WeeklyIncome * Math.Clamp(budget.MaxPowerGridShare, 0d, 1d);
            if (weeklyAllowedUnits <= 0d)
                continue;

            combinedAllowedTicks += weeklyAllowedUnits * Math.Max(1, fuel.TicksPerUnit);
            combinedWeeklyIncomeEquivalent += (double)budget.WeeklyIncome * Math.Max(1, fuel.TicksPerUnit) / Math.Max(1, primaryFuel.TicksPerUnit);
            combinedShareEquivalent += (double)weeklyAllowedUnits * Math.Max(1, fuel.TicksPerUnit) / Math.Max(1, primaryFuel.TicksPerUnit);
            availableBudgets.Add(fuel.Name);
        }

        if (combinedAllowedTicks <= 0d)
            return null;

        double weeklyAllowed = combinedAllowedTicks / Math.Max(1, primaryFuel.TicksPerUnit);
        double weeklyRatio = weeklyNeed / weeklyAllowed;
        int weeklyGap = Math.Max(0, (int)Math.Ceiling(weeklyNeed - weeklyAllowed));
        string name = $"{generator.Name} fuel mix ({primaryFuel.Name}-equivalent)";
        string notes = $"Flexible fuel budget combines {string.Join(", ", availableBudgets)} using their own burn values.";

        return new FlexibleFuelWeeklyBudget(
            name,
            new HashSet<string>(fuels.Select(fuel => fuel.Name), StringComparer.OrdinalIgnoreCase),
            weeklyNeed,
            (int)Math.Round(combinedWeeklyIncomeEquivalent, MidpointRounding.AwayFromZero),
            weeklyAllowed <= 0d || combinedWeeklyIncomeEquivalent <= 0d
                ? 0d
                : combinedShareEquivalent / combinedWeeklyIncomeEquivalent,
            weeklyAllowed,
            weeklyRatio,
            weeklyGap,
            notes);
    }

    private static int CalculateComfortGeneratorCount(BalanceConfig config, LoadoutPlan loadout, GeneratorDefinition generator)
    {
        int demand = 0;
        foreach ((string machineId, int count) in loadout.PoweredMachines)
        {
            MachineDefinition machine = Resolve(config.Machines, machineId, "machine");
            demand += machine.DemandEuPerTick * Math.Max(0, count);
        }

        if (generator.OutputEuPerTick <= 0)
            return 0;

        int minimumGenerators = (int)Math.Ceiling((double)demand / generator.OutputEuPerTick);
        double headroom = Math.Clamp(loadout.ComfortHeadroom, 0d, 2d);
        return Math.Max(minimumGenerators, (int)Math.Ceiling(demand * (1d + headroom) / generator.OutputEuPerTick));
    }

    private static T Resolve<T>(Dictionary<string, T> values, string id, string label)
    {
        return values.TryGetValue(id, out T? value)
            ? value
            : throw new InvalidOperationException($"Unknown {label} '{id}'.");
    }

    private static IReadOnlyList<string> GetFuelOptions(GeneratorDefinition generator)
    {
        if (generator.FuelOptions.Count > 0)
            return generator.FuelOptions.Where(fuel => !string.IsNullOrWhiteSpace(fuel)).ToList();

        return string.IsNullOrWhiteSpace(generator.Fuel)
            ? Array.Empty<string>()
            : new[] { generator.Fuel };
    }

    private static int SignalRank(string signal)
    {
        return signal switch
        {
            "Severe" => 0,
            "Unbudgeted" => 1,
            "Watch" => 2,
            "OK" => 3,
            _ => 4
        };
    }

    private static string RenderMarkdown(ResourceBudgetProfile profile, IReadOnlyList<SustainabilityRow> rows)
    {
        List<string> lines = new()
        {
            "# PowerGrid Resource Sustainability",
            "",
            profile.Description,
            "",
            "This report protects normal Stardew resource use before judging PowerGrid balance. Setup costs are checked against stockpile after a reserve floor. Ongoing fuel is checked against a safe share of weekly income.",
            "",
            "| Loadout | Resource | Signal | Setup need | Usable stockpile | Setup gap | Weekly PG need | Weekly PG budget | Weekly gap | Notes |",
            "| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |"
        };

        foreach (SustainabilityRow row in rows.Where(row => row.Signal is "Severe" or "Unbudgeted" or "Watch"))
        {
            lines.Add($"| {EscapeMarkdown(row.Loadout)} | {EscapeMarkdown(row.Resource)} | {row.Signal} | {row.SetupNeed} | {row.SetupAvailable} | {row.SetupGap} | {row.WeeklyNeed} | {Decimal(row.WeeklyAllowed)} | {row.WeeklyGap} | {EscapeMarkdown(row.Notes)} |");
        }

        if (rows.All(row => row.Signal is not ("Severe" or "Unbudgeted" or "Watch")))
            lines.Add("| - | - | OK | 0 | 0 | 0 | 0 | 0 | 0 | All planned resources fit inside the budget assumptions. |");

        lines.Add("");
        lines.Add("## Fuel Sustainability Snapshot");
        lines.Add("");
        lines.Add("| Loadout | Resource | Weekly need | Weekly budget | Share used | Signal |");
        lines.Add("| --- | --- | ---: | ---: | ---: | --- |");

        foreach (SustainabilityRow row in rows.Where(row => row.WeeklyNeed > 0).OrderBy(row => row.Loadout, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Resource, StringComparer.OrdinalIgnoreCase))
        {
            string share = double.IsPositiveInfinity(row.WeeklyRatio)
                ? "unbudgeted"
                : Percent(row.WeeklyRatio);
            lines.Add($"| {EscapeMarkdown(row.Loadout)} | {EscapeMarkdown(row.Resource)} | {row.WeeklyNeed} | {Decimal(row.WeeklyAllowed)} | {share} | {row.Signal} |");
        }

        lines.Add("");
        lines.Add("## How To Read This");
        lines.Add("");
        lines.Add("- `Setup need` is one-time machine, generator, battery, cable, and conduit crafting cost.");
        lines.Add("- `Usable stockpile` is stockpile minus the reserve floor for normal Stardew needs.");
        lines.Add("- `Weekly PG need` is ongoing fuel ingredient demand scaled to seven days.");
        lines.Add("- `Weekly PG budget` is the safe share of expected weekly resource income.");
        lines.Add("- Severe pressure means PowerGrid is likely crowding out normal gameplay under these assumptions.");

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderCsv(IReadOnlyList<SustainabilityRow> rows)
    {
        StringBuilder csv = new();
        csv.AppendLine(CsvLine("Loadout", "Stage", "Resource", "Signal", "SetupNeed", "Stockpile", "ReserveFloor", "SetupAvailable", "SetupGap", "WeeklyNeed", "WeeklyIncome", "MaxPowerGridShare", "WeeklyAllowed", "WeeklyRatio", "WeeklyGap", "Notes"));

        foreach (SustainabilityRow row in rows)
        {
            csv.AppendLine(CsvLine(
                row.Loadout,
                row.Stage,
                row.Resource,
                row.Signal,
                row.SetupNeed,
                row.Stockpile,
                row.ReserveFloor,
                row.SetupAvailable,
                row.SetupGap,
                row.WeeklyNeed,
                row.WeeklyIncome,
                Decimal(row.MaxPowerGridShare),
                Decimal(row.WeeklyAllowed),
                double.IsPositiveInfinity(row.WeeklyRatio) ? "Infinity" : Decimal(row.WeeklyRatio),
                row.WeeklyGap,
                row.Notes));
        }

        return csv.ToString();
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

    private static string Decimal(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string Percent(double value)
    {
        return value.ToString("P0", CultureInfo.InvariantCulture);
    }

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    public sealed record SustainabilityRow(
        string Loadout,
        string Stage,
        string Resource,
        string Signal,
        int SetupNeed,
        int Stockpile,
        int ReserveFloor,
        int SetupAvailable,
        int SetupGap,
        int WeeklyNeed,
        int WeeklyIncome,
        double MaxPowerGridShare,
        double WeeklyAllowed,
        double WeeklyRatio,
        int WeeklyGap,
        string Notes);

    private sealed record FlexibleFuelWeeklyBudget(
        string Name,
        IReadOnlySet<string> OptionResources,
        int WeeklyNeed,
        int WeeklyIncomeEquivalent,
        double MaxPowerGridShareEquivalent,
        double WeeklyAllowed,
        double WeeklyRatio,
        int WeeklyGap,
        string Notes);
}
