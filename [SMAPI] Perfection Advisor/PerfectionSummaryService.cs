using StardewModdingAPI;
using StardewValley;
using System.Collections;
using System.Reflection;

namespace Darth.PerfectionAdvisor;

internal sealed class PerfectionSummaryService
{
    private readonly IModHelper helper;

    public PerfectionSummaryService(IModHelper helper)
    {
        this.helper = helper;
    }

    public string BuildSummary(ModConfig config)
    {
        if (!Context.IsWorldReady)
            return "No save is loaded.";

        Farmer player = Game1.player;
        List<string> lines = new()
        {
            "Perfection Advisor (v1)",
            config.EnableDetailedSpoilers && !config.ShowOnlyCategorySummary
                ? "Mode: detailed spoilers enabled"
                : "Mode: summary-only (spoilers hidden)"
        };

        int shippedUnique = CountPositiveValues(player.basicShipped);
        int fishCaught = CountPositiveValues(player.fishCaught);
        int cookedRecipes = CountPositiveValues(player.cookingRecipes);
        int craftedRecipes = CountPositiveValues(player.craftingRecipes);
        int villagersAtEightHearts = CountFriendshipsAtLeastHearts(player.friendshipData, 8);
        int villagersAtTenHearts = CountFriendshipsAtLeastHearts(player.friendshipData, 10);

        int? totalFish = this.TryLoadCount("Data/Fish");
        int? totalCookingRecipes = this.TryLoadCount("Data/CookingRecipes");
        int? totalCraftingRecipes = this.TryLoadCount("Data/CraftingRecipes");

        lines.Add(string.Empty);
        lines.Add("Progress");
        lines.Add($"- Shipped items: {shippedUnique} unique");
        lines.Add(FormatProgress("Fish caught", fishCaught, totalFish));
        lines.Add(FormatProgress("Cooking recipes made", cookedRecipes, totalCookingRecipes));
        lines.Add(FormatProgress("Crafting recipes made", craftedRecipes, totalCraftingRecipes));
        lines.Add($"- Friendships 8+ hearts: {villagersAtEightHearts}");
        lines.Add($"- Friendships 10+ hearts: {villagersAtTenHearts}");

        List<(string Label, int Remaining)> blockers = new();
        AddBlockerIfKnown(blockers, "Fish collection", fishCaught, totalFish);
        AddBlockerIfKnown(blockers, "Cooking recipes", cookedRecipes, totalCookingRecipes);
        AddBlockerIfKnown(blockers, "Crafting recipes", craftedRecipes, totalCraftingRecipes);

        lines.Add(string.Empty);
        lines.Add("Top blockers");
        if (blockers.Count == 0)
        {
            lines.Add("- No blocker estimate available yet.");
        }
        else
        {
            foreach ((string label, int remaining) in blockers
                         .Where(p => p.Remaining > 0)
                         .OrderByDescending(p => p.Remaining)
                         .Take(3))
            {
                lines.Add($"- {label}: {remaining} remaining");
            }

            if (!blockers.Any(p => p.Remaining > 0))
                lines.Add("- All tracked categories are complete.");
        }

        if (config.EnableDetailedSpoilers && !config.ShowOnlyCategorySummary)
        {
            AddDetailIfAny(lines, "Missing fish examples", this.GetMissingFishNames(maxItems: 3));
            AddDetailIfAny(lines, "Missing cooking recipe examples", this.GetMissingCookingRecipeNames(maxItems: 3));
        }
        else
        {
            lines.Add(string.Empty);
            lines.Add("Detailed spoiler hints are disabled.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static int CountPositiveValues(object? source)
    {
        int count = 0;
        foreach (KeyValuePair<string, object?> pair in EnumerateEntries(source))
        {
            if (IsPositiveValue(pair.Value))
                count++;
        }

        return count;
    }

    private static int CountFriendshipsAtLeastHearts(object? source, int hearts)
    {
        int thresholdPoints = hearts * 250;
        int count = 0;
        foreach (KeyValuePair<string, object?> pair in EnumerateEntries(source))
        {
            object? friendshipObject = UnwrapValue(pair.Value);
            int? points = GetIntProperty(friendshipObject, "Points");
            if (points.HasValue && points.Value >= thresholdPoints)
                count++;
        }

        return count;
    }

    private int? TryLoadCount(string assetName)
    {
        try
        {
            return this.helper.GameContent.Load<Dictionary<string, string>>(assetName).Count;
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<string> GetMissingFishNames(int maxItems)
    {
        try
        {
            Dictionary<string, string> fishData = this.helper.GameContent.Load<Dictionary<string, string>>("Data/Fish");
            HashSet<string> caught = GetPositiveKeys(Game1.player.fishCaught);

            return fishData
                .Where(pair => !caught.Contains(pair.Key))
                .Select(pair => ExtractNameFromDataEntry(pair.Value, fallback: pair.Key))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Take(maxItems)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private IEnumerable<string> GetMissingCookingRecipeNames(int maxItems)
    {
        try
        {
            Dictionary<string, string> allRecipes = this.helper.GameContent.Load<Dictionary<string, string>>("Data/CookingRecipes");
            HashSet<string> cooked = GetPositiveKeys(Game1.player.cookingRecipes);

            return allRecipes.Keys
                .Where(recipeName => !cooked.Contains(recipeName))
                .Take(maxItems)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string ExtractNameFromDataEntry(string data, string fallback)
    {
        if (string.IsNullOrWhiteSpace(data))
            return fallback;

        string[] parts = data.Split('/');
        return parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])
            ? parts[0]
            : fallback;
    }

    private static string FormatProgress(string label, int current, int? total)
    {
        return total.HasValue
            ? $"- {label}: {current}/{total.Value}"
            : $"- {label}: {current}";
    }

    private static void AddBlockerIfKnown(List<(string Label, int Remaining)> blockers, string label, int current, int? total)
    {
        if (!total.HasValue)
            return;

        blockers.Add((label, Math.Max(0, total.Value - current)));
    }

    private static void AddDetailIfAny(List<string> lines, string title, IEnumerable<string> items)
    {
        string[] values = items.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
        if (values.Length == 0)
            return;

        lines.Add(string.Empty);
        lines.Add(title);
        foreach (string value in values)
            lines.Add($"- {value}");
    }

    private static HashSet<string> GetPositiveKeys(object? source)
    {
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, object?> pair in EnumerateEntries(source))
        {
            if (IsPositiveValue(pair.Value))
                keys.Add(pair.Key);
        }

        return keys;
    }

    private static IEnumerable<KeyValuePair<string, object?>> EnumerateEntries(object? source)
    {
        if (source == null)
            yield break;

        IEnumerable? rawEntries = source as IEnumerable;
        if (rawEntries == null)
        {
            PropertyInfo? pairsProp = source.GetType().GetProperty("Pairs", BindingFlags.Instance | BindingFlags.Public);
            rawEntries = pairsProp?.GetValue(source) as IEnumerable;
        }

        if (rawEntries == null)
            yield break;

        foreach (object? entry in rawEntries)
        {
            if (entry == null)
                continue;

            if (!TryGetKeyValue(entry, out string key, out object? value))
                continue;

            yield return new KeyValuePair<string, object?>(key, value);
        }
    }

    private static bool TryGetKeyValue(object entry, out string key, out object? value)
    {
        if (entry is DictionaryEntry dictionaryEntry)
        {
            key = dictionaryEntry.Key?.ToString() ?? string.Empty;
            value = dictionaryEntry.Value;
            return key.Length > 0;
        }

        Type type = entry.GetType();
        PropertyInfo? keyProp = type.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo? valueProp = type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        if (keyProp == null || valueProp == null)
        {
            key = string.Empty;
            value = null;
            return false;
        }

        object? keyObj = keyProp.GetValue(entry);
        key = keyObj?.ToString() ?? string.Empty;
        value = valueProp.GetValue(entry);
        return key.Length > 0;
    }

    private static bool IsPositiveValue(object? value)
    {
        object? unwrapped = UnwrapValue(value);
        if (unwrapped == null)
            return false;

        return unwrapped switch
        {
            int intValue => intValue > 0,
            long longValue => longValue > 0,
            float floatValue => floatValue > 0,
            double doubleValue => doubleValue > 0,
            decimal decimalValue => decimalValue > 0,
            int[] ints => ints.Any(v => v > 0),
            IEnumerable<int> intValues => intValues.Any(v => v > 0),
            _ => false
        };
    }

    private static object? UnwrapValue(object? value)
    {
        if (value == null)
            return null;

        Type type = value.GetType();
        PropertyInfo? wrappedValueProp = type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        if (wrappedValueProp != null && type.Namespace != "System")
        {
            object? wrapped = wrappedValueProp.GetValue(value);
            if (wrapped != null && !ReferenceEquals(wrapped, value))
                return wrapped;
        }

        return value;
    }

    private static int? GetIntProperty(object? source, string propertyName)
    {
        if (source == null)
            return null;

        PropertyInfo? prop = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        object? value = prop?.GetValue(source);
        if (value == null)
            return null;

        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            _ => null
        };
    }
}
