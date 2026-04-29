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

static IReadOnlyList<string> ExpandScenarioFiles(string pattern)
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

static void PrintHelp()
{
    Console.WriteLine(
        """
        PowerGrid Balancer

        Usage:
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- scenario <scenario.json>
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- batch <scenario-folder-or-glob>

        Options:
          --config <path>   Balance config JSON. Defaults to balance/powergrid-0.1.0.json.

        Examples:
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- scenario tools/PowerGrid.Balancer/scenarios/early-mixed-room.json
          dotnet run --project tools/PowerGrid.Balancer/src/PowerGrid.Balancer.Cli -- batch tools/PowerGrid.Balancer/scenarios
        """);
}
