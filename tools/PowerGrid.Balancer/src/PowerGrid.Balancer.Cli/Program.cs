using System.Text.Json;
using PowerGrid.Balancer.Core;

JsonSerializerOptions jsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    WriteIndented = true
};

try
{
    if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
    {
        PrintHelp();
        return 0;
    }

    string command = args[0].ToLowerInvariant();
    string configPath = ReadOption(args, "--config") ?? FindDefaultConfig();

    BalanceConfig config = LoadJson<BalanceConfig>(configPath, jsonOptions);
    PowerGridSimulator simulator = new();

    switch (command)
    {
        case "scenario":
            if (args.Length < 2)
                throw new InvalidOperationException("Missing scenario path.");

            Scenario scenario = LoadJson<Scenario>(args[1], jsonOptions);
            SimulationResult result = simulator.Simulate(config, scenario);
            Console.WriteLine(MarkdownReport.Render(result));
            return result.TotalUnmetEu > 0 ? 2 : 0;

        case "batch":
            if (args.Length < 2)
                throw new InvalidOperationException("Missing scenario folder or glob.");

            IReadOnlyList<string> files = ExpandScenarioFiles(args[1]);
            if (files.Count == 0)
                throw new InvalidOperationException($"No scenario files matched '{args[1]}'.");

            List<SimulationResult> results = files
                .Select(file => simulator.Simulate(config, LoadJson<Scenario>(file, jsonOptions)))
                .ToList();

            Console.WriteLine(MarkdownReport.RenderBatch(results));
            return results.Any(result => result.TotalUnmetEu > 0) ? 2 : 0;

        case "audit":
            {
                string scenarioInput = args.Length >= 2 && !args[1].StartsWith("--", StringComparison.Ordinal)
                    ? args[1]
                    : FindDefaultScenarioPath();
                string outputPath = ReadOption(args, "--out") ?? Path.Combine("artifacts", "balance-lab", "current");

                IReadOnlyList<string> auditFiles = ExpandScenarioFiles(scenarioInput);
                if (auditFiles.Count == 0)
                    throw new InvalidOperationException($"No scenario files matched '{scenarioInput}'.");

                List<SimulationResult> auditResults = auditFiles
                    .Select(file => simulator.Simulate(config, LoadJson<Scenario>(file, jsonOptions)))
                    .ToList();

                AuditReport.Write(outputPath, config, auditResults);
                Console.WriteLine($"Wrote balance audit to {Path.GetFullPath(outputPath)}");
                Console.WriteLine("- summary.md");
                Console.WriteLine("- scenario-results.csv");
                Console.WriteLine("- fuel-use.csv");
                Console.WriteLine("- generator-capacity.md");
                Console.WriteLine("- generator-capacity.csv");
                Console.WriteLine("- machine-defaults.csv");
                return 0;
            }

        case "progression":
            {
                string profilePath = args.Length >= 2 && !args[1].StartsWith("--", StringComparison.Ordinal)
                    ? args[1]
                    : FindDefaultProgressionPath();
                string outputPath = ReadOption(args, "--out") ?? Path.Combine("artifacts", "balance-lab", "progression");

                ProgressionProfile profile = LoadJson<ProgressionProfile>(profilePath, jsonOptions);
                ProgressionReport.Write(outputPath, config, profile);
                Console.WriteLine($"Wrote progression audit to {Path.GetFullPath(outputPath)}");
                Console.WriteLine("- progression-summary.md");
                Console.WriteLine("- progression-stages.csv");
                Console.WriteLine("- progression-resource-gaps.csv");
                Console.WriteLine("- progression-fuel.csv");
                Console.WriteLine("- progression-plan.md");
                Console.WriteLine("- progression-plan.csv");
                return 0;
            }

        case "plan":
            {
                string loadoutInput = args.Length >= 2 && !args[1].StartsWith("--", StringComparison.Ordinal)
                    ? args[1]
                    : FindDefaultLoadoutPath();
                string? outputPath = ReadOption(args, "--out");
                IReadOnlyList<string> loadoutFiles = ExpandJsonFiles(loadoutInput);
                if (loadoutFiles.Count == 0)
                    throw new InvalidOperationException($"No loadout files matched '{loadoutInput}'.");

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    foreach (string file in loadoutFiles)
                    {
                        LoadoutPlan loadout = LoadJson<LoadoutPlan>(file, jsonOptions);
                        Console.WriteLine(LoadoutPlanReport.RenderConsole(config, loadout));
                        Console.WriteLine();
                    }
                    return 0;
                }

                List<LoadoutPlanReport.PlanSummary> summaries = new();
                foreach (string file in loadoutFiles)
                {
                    LoadoutPlan loadout = LoadJson<LoadoutPlan>(file, jsonOptions);
                    string reportFolderName = MakeSafeFolderName(Path.GetFileNameWithoutExtension(file));
                    string reportPath = Path.Combine(outputPath, reportFolderName);
                    LoadoutPlanReport.Write(reportPath, config, loadout);
                    summaries.Add(LoadoutPlanReport.Summarize(config, loadout, reportFolderName));
                }

                LoadoutPlanReport.WriteIndex(outputPath, summaries);
                Console.WriteLine($"Wrote loadout plan to {Path.GetFullPath(outputPath)}");
                Console.WriteLine("- loadout-index.md");
                Console.WriteLine("- loadout-index.csv");
                foreach (LoadoutPlanReport.PlanSummary summary in summaries)
                    Console.WriteLine($"- {summary.ReportPath}/loadout-plan.md");
                return 0;
            }

        case "resources":
            {
                string loadoutInput = args.Length >= 2 && !args[1].StartsWith("--", StringComparison.Ordinal)
                    ? args[1]
                    : FindDefaultLoadoutFolder();
                string? outputPath = ReadOption(args, "--out");
                IReadOnlyList<string> loadoutFiles = ExpandJsonFiles(loadoutInput);
                if (loadoutFiles.Count == 0)
                    throw new InvalidOperationException($"No loadout files matched '{loadoutInput}'.");

                List<LoadoutPlan> loadouts = loadoutFiles
                    .Select(file => LoadJson<LoadoutPlan>(file, jsonOptions))
                    .ToList();

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    Console.WriteLine(ResourcePressureReport.RenderConsole(config, loadouts));
                    return 0;
                }

                ResourcePressureReport.Write(outputPath, config, loadouts);
                Console.WriteLine($"Wrote resource pressure report to {Path.GetFullPath(outputPath)}");
                Console.WriteLine("- resource-pressure.md");
                Console.WriteLine("- resource-pressure.csv");
                return 0;
            }

        case "compare-resources":
            {
                if (args.Length < 3)
                    throw new InvalidOperationException("Missing baseline config and candidate config folder or glob.");

                string baselinePath = args[1];
                string candidateInput = args[2];
                string loadoutInput = args.Length >= 4 && !args[3].StartsWith("--", StringComparison.Ordinal)
                    ? args[3]
                    : FindDefaultLoadoutFolder();
                string outputPath = ReadOption(args, "--out") ?? Path.Combine("artifacts", "balance-lab", "resource-comparison");

                IReadOnlyList<string> candidateFiles = ExpandJsonFiles(candidateInput);
                if (candidateFiles.Count == 0)
                    throw new InvalidOperationException($"No candidate config files matched '{candidateInput}'.");

                IReadOnlyList<string> loadoutFiles = ExpandJsonFiles(loadoutInput);
                if (loadoutFiles.Count == 0)
                    throw new InvalidOperationException($"No loadout files matched '{loadoutInput}'.");

                Dictionary<string, BalanceConfig> configs = new(StringComparer.OrdinalIgnoreCase)
                {
                    [Path.GetFileNameWithoutExtension(baselinePath)] = LoadJson<BalanceConfig>(baselinePath, jsonOptions)
                };

                foreach (string file in candidateFiles)
                    configs[Path.GetFileNameWithoutExtension(file)] = LoadJson<BalanceConfig>(file, jsonOptions);

                List<LoadoutPlan> loadouts = loadoutFiles
                    .Select(file => LoadJson<LoadoutPlan>(file, jsonOptions))
                    .ToList();

                ResourcePressureComparisonReport.Write(outputPath, configs, loadouts);
                Console.WriteLine($"Wrote resource pressure comparison to {Path.GetFullPath(outputPath)}");
                Console.WriteLine("- resource-pressure-comparison.md");
                Console.WriteLine("- resource-pressure-comparison.csv");
                return 0;
            }

        case "sustainability":
            {
                string loadoutInput = args.Length >= 2 && !args[1].StartsWith("--", StringComparison.Ordinal)
                    ? args[1]
                    : FindDefaultLoadoutFolder();
                string budgetPath = ReadOption(args, "--budget") ?? FindDefaultBudgetPath();
                string? outputPath = ReadOption(args, "--out");

                IReadOnlyList<string> loadoutFiles = ExpandJsonFiles(loadoutInput);
                if (loadoutFiles.Count == 0)
                    throw new InvalidOperationException($"No loadout files matched '{loadoutInput}'.");

                List<LoadoutPlan> loadouts = loadoutFiles
                    .Select(file => LoadJson<LoadoutPlan>(file, jsonOptions))
                    .ToList();
                ResourceBudgetProfile budget = LoadJson<ResourceBudgetProfile>(budgetPath, jsonOptions);

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    Console.WriteLine(ResourceSustainabilityReport.RenderConsole(config, loadouts, budget));
                    return 0;
                }

                ResourceSustainabilityReport.Write(outputPath, config, loadouts, budget);
                Console.WriteLine($"Wrote resource sustainability report to {Path.GetFullPath(outputPath)}");
                Console.WriteLine("- resource-sustainability.md");
                Console.WriteLine("- resource-sustainability.csv");
                return 0;
            }

        case "compare-sustainability":
            {
                if (args.Length < 3)
                    throw new InvalidOperationException("Missing baseline config and candidate config folder or glob.");

                string baselinePath = args[1];
                string candidateInput = args[2];
                string loadoutInput = args.Length >= 4 && !args[3].StartsWith("--", StringComparison.Ordinal)
                    ? args[3]
                    : FindDefaultLoadoutFolder();
                string budgetPath = ReadOption(args, "--budget") ?? FindDefaultBudgetPath();
                string outputPath = ReadOption(args, "--out") ?? Path.Combine("artifacts", "balance-lab", "sustainability-comparison");

                IReadOnlyList<string> candidateFiles = ExpandJsonFiles(candidateInput);
                if (candidateFiles.Count == 0)
                    throw new InvalidOperationException($"No candidate config files matched '{candidateInput}'.");

                IReadOnlyList<string> loadoutFiles = ExpandJsonFiles(loadoutInput);
                if (loadoutFiles.Count == 0)
                    throw new InvalidOperationException($"No loadout files matched '{loadoutInput}'.");

                Dictionary<string, BalanceConfig> configs = new(StringComparer.OrdinalIgnoreCase)
                {
                    [Path.GetFileNameWithoutExtension(baselinePath)] = LoadJson<BalanceConfig>(baselinePath, jsonOptions)
                };

                foreach (string file in candidateFiles)
                    configs[Path.GetFileNameWithoutExtension(file)] = LoadJson<BalanceConfig>(file, jsonOptions);

                List<LoadoutPlan> loadouts = loadoutFiles
                    .Select(file => LoadJson<LoadoutPlan>(file, jsonOptions))
                    .ToList();
                ResourceBudgetProfile budget = LoadJson<ResourceBudgetProfile>(budgetPath, jsonOptions);

                ResourceSustainabilityComparisonReport.Write(outputPath, configs, loadouts, budget);
                Console.WriteLine($"Wrote resource sustainability comparison to {Path.GetFullPath(outputPath)}");
                Console.WriteLine("- resource-sustainability-comparison.md");
                Console.WriteLine("- resource-sustainability-comparison.csv");
                return 0;
            }

        case "compare-progression":
            {
                if (args.Length < 3)
                    throw new InvalidOperationException("Missing baseline and proposed config paths.");

                string baselinePath = args[1];
                string proposedPath = args[2];
                string profilePath = args.Length >= 4 && !args[3].StartsWith("--", StringComparison.Ordinal)
                    ? args[3]
                    : FindDefaultProgressionPath();
                string outputPath = ReadOption(args, "--out") ?? Path.Combine("artifacts", "balance-lab", "comparison");

                BalanceConfig baseline = LoadJson<BalanceConfig>(baselinePath, jsonOptions);
                BalanceConfig proposed = LoadJson<BalanceConfig>(proposedPath, jsonOptions);
                ProgressionProfile profile = LoadJson<ProgressionProfile>(profilePath, jsonOptions);

                ProgressionComparisonReport.Write(
                    outputPath,
                    Path.GetFileNameWithoutExtension(baselinePath),
                    baseline,
                    Path.GetFileNameWithoutExtension(proposedPath),
                    proposed,
                    profile);

                Console.WriteLine($"Wrote progression comparison to {Path.GetFullPath(outputPath)}");
                Console.WriteLine("- progression-comparison.md");
                Console.WriteLine("- progression-comparison.csv");
                Console.WriteLine("- progression-comparison-gaps.csv");
                return 0;
            }

        default:
            throw new InvalidOperationException($"Unknown command '{args[0]}'.");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"PowerGrid Balancer error: {ex.Message}");
    return 1;
}

static T LoadJson<T>(string path, JsonSerializerOptions options)
{
    if (!File.Exists(path))
        throw new FileNotFoundException($"File not found: {path}");

    string json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<T>(json, options)
        ?? throw new InvalidOperationException($"Could not deserialize {path}.");
}

static string? ReadOption(string[] args, string option)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], option, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

static string FindDefaultConfig()
{
    string cwd = Directory.GetCurrentDirectory();
    string[] candidates =
    {
        Path.Combine(cwd, "balance", "powergrid-0.1.0.json"),
        Path.Combine(cwd, "tools", "PowerGrid.Balancer", "balance", "powergrid-0.1.0.json"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "balance", "powergrid-0.1.0.json"))
    };

    return candidates.FirstOrDefault(File.Exists)
        ?? throw new FileNotFoundException("Could not find default balance config. Pass --config <path>.");
}

static string FindDefaultScenarioPath()
{
    string cwd = Directory.GetCurrentDirectory();
    string[] candidates =
    {
        Path.Combine(cwd, "scenarios"),
        Path.Combine(cwd, "tools", "PowerGrid.Balancer", "scenarios"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "scenarios"))
    };

    return candidates.FirstOrDefault(Directory.Exists)
        ?? throw new DirectoryNotFoundException("Could not find default scenarios folder. Pass audit <scenario-folder-or-glob>.");
}

static string FindDefaultProgressionPath()
{
    string cwd = Directory.GetCurrentDirectory();
    string[] candidates =
    {
        Path.Combine(cwd, "profiles", "powergrid-progression-ladder.json"),
        Path.Combine(cwd, "tools", "PowerGrid.Balancer", "profiles", "powergrid-progression-ladder.json"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "profiles", "powergrid-progression-ladder.json"))
    };

    return candidates.FirstOrDefault(File.Exists)
        ?? throw new FileNotFoundException("Could not find default progression profile. Pass progression <profile.json>.");
}

static string FindDefaultLoadoutPath()
{
    string cwd = Directory.GetCurrentDirectory();
    string[] candidates =
    {
        Path.Combine(cwd, "loadouts", "sample-big-shed.json"),
        Path.Combine(cwd, "tools", "PowerGrid.Balancer", "loadouts", "sample-big-shed.json"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "loadouts", "sample-big-shed.json"))
    };

    return candidates.FirstOrDefault(File.Exists)
        ?? throw new FileNotFoundException("Could not find default loadout. Pass plan <loadout.json>.");
}

static string FindDefaultLoadoutFolder()
{
    string cwd = Directory.GetCurrentDirectory();
    string[] candidates =
    {
        Path.Combine(cwd, "loadouts"),
        Path.Combine(cwd, "tools", "PowerGrid.Balancer", "loadouts"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "loadouts"))
    };

    return candidates.FirstOrDefault(Directory.Exists)
        ?? throw new DirectoryNotFoundException("Could not find default loadouts folder. Pass resources <loadout-folder-or-glob>.");
}

static string FindDefaultBudgetPath()
{
    string cwd = Directory.GetCurrentDirectory();
    string[] candidates =
    {
        Path.Combine(cwd, "resource-budgets", "powergrid-sustainability.json"),
        Path.Combine(cwd, "tools", "PowerGrid.Balancer", "resource-budgets", "powergrid-sustainability.json"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "resource-budgets", "powergrid-sustainability.json"))
    };

    return candidates.FirstOrDefault(File.Exists)
        ?? throw new FileNotFoundException("Could not find default resource budget profile. Pass --budget <path>.");
}

static IReadOnlyList<string> ExpandScenarioFiles(string pattern)
{
    return ExpandJsonFiles(pattern);
}

static IReadOnlyList<string> ExpandJsonFiles(string pattern)
{
    if (File.Exists(pattern))
        return new[] { pattern };

    string directory = Path.GetDirectoryName(pattern) ?? ".";
    string filePattern = Path.GetFileName(pattern);

    if (string.IsNullOrWhiteSpace(filePattern) || !filePattern.Contains('*'))
    {
        if (!Directory.Exists(pattern))
            return Array.Empty<string>();

        directory = pattern;
        filePattern = "*.json";
    }

    if (!Directory.Exists(directory))
        return Array.Empty<string>();

    return Directory.GetFiles(directory, filePattern)
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToList();
}

static string MakeSafeFolderName(string value)
{
    char[] invalidChars = Path.GetInvalidFileNameChars();
    string cleaned = new(value.Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());
    return string.IsNullOrWhiteSpace(cleaned) ? "loadout" : cleaned;
}

static void PrintHelp()
{
    Console.WriteLine(
        """
        PowerGrid Balancer

        Usage:
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- scenario <scenario.json>
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- batch <scenario-folder-or-glob>
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- audit [scenario-folder-or-glob] [--out <folder>]
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- progression [profile.json] [--out <folder>]
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- plan [loadout.json|loadout-folder-or-glob] [--out <folder>]
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- resources [loadout-folder-or-glob] [--out <folder>]
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- compare-resources <baseline-config.json> <candidate-config-folder-or-glob> [loadout-folder-or-glob] [--out <folder>]
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- sustainability [loadout-folder-or-glob] [--budget <budget.json>] [--out <folder>]
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- compare-sustainability <baseline-config.json> <candidate-config-folder-or-glob> [loadout-folder-or-glob] [--budget <budget.json>] [--out <folder>]
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- compare-progression <baseline-config.json> <proposed-config.json> [profile.json] [--out <folder>]

        Options:
          --config <path>   Balance config JSON. Defaults to balance/powergrid-0.1.0.json.
          --budget <path>   Resource budget JSON. Defaults to resource-budgets/powergrid-sustainability.json.
          --out <folder>    Audit output folder. Defaults to artifacts/balance-lab/current.

        Examples:
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- scenario tools/PowerGrid.Balancer/scenarios/early-mixed-room.json
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- batch tools/PowerGrid.Balancer/scenarios
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- audit tools/PowerGrid.Balancer/scenarios --out artifacts/balance-lab/current
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- progression tools/PowerGrid.Balancer/profiles/powergrid-progression-ladder.json --out artifacts/balance-lab/progression
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- plan tools/PowerGrid.Balancer/loadouts/sample-big-shed.json --config tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json --out artifacts/balance-lab/loadout-sample
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- resources tools/PowerGrid.Balancer/loadouts --config tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json --out artifacts/balance-lab/resource-pressure
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- compare-resources tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json tools/PowerGrid.Balancer/balance/powergrid-0.1.x-biofuel-*.json tools/PowerGrid.Balancer/loadouts --out artifacts/balance-lab/biofuel-candidates
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- sustainability tools/PowerGrid.Balancer/loadouts --config tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json --out artifacts/balance-lab/sustainability
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- compare-sustainability tools/PowerGrid.Balancer/balance/powergrid-0.1.x-moderate.json tools/PowerGrid.Balancer/balance/powergrid-0.1.x-biofuel-*.json tools/PowerGrid.Balancer/loadouts --out artifacts/balance-lab/biofuel-sustainability
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- compare-progression tools/PowerGrid.Balancer/balance/powergrid-0.1.0.json tools/PowerGrid.Balancer/balance/powergrid-0.1.x-test.json tools/PowerGrid.Balancer/profiles/powergrid-progression-ladder.json --out artifacts/balance-lab/comparison
        """);
}
