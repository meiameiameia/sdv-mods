using StardewModdingAPI;
using StardewValley;
using System.Collections;
using System.Reflection;

namespace Darth.PerfectionAdvisor;

internal sealed class PerfectionSummaryService
{
    private sealed class SeasonalCropCandidate
    {
        public string HarvestItemId { get; init; } = string.Empty;
        public string SeedItemId { get; init; } = string.Empty;
        public string HarvestName { get; init; } = string.Empty;
        public string SeedName { get; init; } = string.Empty;
        public int DaysToMature { get; init; }
        public IReadOnlyList<Season> Seasons { get; init; } = Array.Empty<Season>();
    }

    private sealed class FriendshipTargetStatus
    {
        public NPC Npc { get; init; } = null!;
        public string NpcName { get; init; } = string.Empty;
        public int CurrentHearts { get; init; }
        public int TargetHearts { get; init; }
        public int HeartsRemaining { get; init; }
    }

    private readonly IModHelper helper;
    private readonly Dictionary<string, string> giftHintByNpcName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly int GiftTasteLoveCode = GetNpcGiftTasteCode("gift_taste_love", fallback: 0);
    private static readonly int GiftTasteLikeCode = GetNpcGiftTasteCode("gift_taste_like", fallback: 2);

    public PerfectionSummaryService(IModHelper helper)
    {
        this.helper = helper;
    }

    public PerfectionAdvisorSnapshot BuildSnapshot(ModConfig config)
    {
        if (!Context.IsWorldReady)
        {
            return new PerfectionAdvisorSnapshot(
                "No save loaded",
                new[] { "Load a save to view advisor data." },
                new[] { "No progress data available." },
                new[] { "No blocker data available." },
                new[] { "No friendship data available." },
                new[] { "No seasonal data available." },
                new[] { "Detailed spoilers are disabled." });
        }

        Farmer player = Game1.player;
        bool detailsEnabled = config.EnableDetailedSpoilers && !config.ShowOnlyCategorySummary;

        (int shippedUnique, int shippedTotal) = this.GetCanonicalShippedProgress(player);
        int fishCaught = CountCollectionEntries(player.fishCaught);
        int cookedRecipes = CountPlayerProgressEntries(player, "recipesCooked", player.cookingRecipes);
        int craftedRecipes = CountPlayerProgressEntries(player, "recipesCrafted", player.craftingRecipes);
        List<string> friendshipLines = this.BuildFriendshipLines(player, detailsEnabled, out int completeFriendshipTargets, out int totalFriendshipTargets, out int totalHeartsRemaining);

        int? totalFish = this.TryLoadCount("Data/Fish");
        int? totalCookingRecipes = this.TryLoadCount("Data/CookingRecipes");
        int? totalCraftingRecipes = this.TryLoadCount("Data/CraftingRecipes");

        List<string> overviewLines = new()
        {
            detailsEnabled ? "Detailed spoiler mode is ON." : "Summary-only mode is ON.",
            config.ShowOnlyCategorySummary ? "Category summary lock is ON." : "Category summary lock is OFF.",
            "Shipped progress mirrors Collections/perfection shipped-item semantics.",
            "Read-only advisor. No automation or gameplay control actions."
        };

        List<string> progressLines = new()
        {
            FormatProgress("Shipped items", shippedUnique, shippedTotal, suffix: " unique"),
            FormatProgress("Fish caught", fishCaught, totalFish),
            FormatProgress("Cooking recipes made", cookedRecipes, totalCookingRecipes),
            FormatProgress("Crafting recipes made", craftedRecipes, totalCraftingRecipes),
            FormatProgress("Friendship targets maxed", completeFriendshipTargets, totalFriendshipTargets),
            $"Friendship hearts remaining: {totalHeartsRemaining}"
        };

        List<(string Label, int Remaining)> blockers = new();
        AddBlockerIfKnown(blockers, "Fish collection", fishCaught, totalFish);
        AddBlockerIfKnown(blockers, "Cooking recipes", cookedRecipes, totalCookingRecipes);
        AddBlockerIfKnown(blockers, "Crafting recipes", craftedRecipes, totalCraftingRecipes);

        List<string> blockerLines = new();
        List<(string Label, int Remaining)> topBlockers = blockers
            .Where(p => p.Remaining > 0)
            .OrderByDescending(p => p.Remaining)
            .Take(3)
            .ToList();

        if (topBlockers.Count == 0)
        {
            blockerLines.Add(blockers.Count == 0
                ? "No blocker estimate available yet."
                : "All tracked blocker categories are complete.");
        }
        else
        {
            foreach ((string label, int remaining) in topBlockers)
                blockerLines.Add($"{label}: {remaining} remaining");
        }

        List<string> detailLines = new();
        if (detailsEnabled)
        {
            AddDetailIfAny(detailLines, "Missing fish examples", this.GetMissingFishNames(maxItems: 5));
            AddDetailIfAny(detailLines, "Missing cooking recipe examples", this.GetMissingCookingRecipeNames(maxItems: 5));
            if (detailLines.Count == 0)
                detailLines.Add("No detailed spoiler items available right now.");
        }
        else
        {
            detailLines.Add("Detailed spoiler hints are disabled.");
            detailLines.Add("Enable 'EnableDetailedSpoilers' and disable 'ShowOnlyCategorySummary' to view detail examples.");
        }

        List<string> seasonalLines = this.BuildSeasonalCropLines(player, detailsEnabled);

        return new PerfectionAdvisorSnapshot(
            detailsEnabled ? "Detailed spoilers enabled" : "Summary-only mode",
            overviewLines,
            progressLines,
            blockerLines,
            friendshipLines,
            seasonalLines,
            detailLines);
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

    private static int CountCollectionEntries(object? source)
    {
        if (source == null)
            return 0;

        PropertyInfo? lengthProp = source.GetType().GetProperty("Length", BindingFlags.Instance | BindingFlags.Public);
        if (lengthProp?.GetValue(source) is int lengthValue)
            return lengthValue;

        if (source is ICollection collection)
            return collection.Count;

        PropertyInfo? countProp = source.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
        if (countProp?.GetValue(source) is int countValue)
            return countValue;

        int count = 0;
        foreach (KeyValuePair<string, object?> _ in EnumerateEntries(source))
            count++;

        return count;
    }

    private static int CountPlayerProgressEntries(Farmer player, string canonicalPropertyName, object fallbackSource)
    {
        object? canonical = GetPropertyValue(player, canonicalPropertyName);
        if (canonical != null)
            return CountCollectionEntries(canonical);

        return CountPositiveValues(fallbackSource);
    }

    private (int ShippedUnique, int TotalShippable) GetCanonicalShippedProgress(Farmer player)
    {
        try
        {
            Dictionary<string, StardewValley.GameData.Objects.ObjectData> objectDataById =
                this.helper.GameContent.Load<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>("Data/Objects");

            int shippedUnique = 0;
            int totalShippable = 0;
            foreach (KeyValuePair<string, StardewValley.GameData.Objects.ObjectData> pair in objectDataById)
            {
                string itemId = pair.Key;
                StardewValley.GameData.Objects.ObjectData itemData = pair.Value;

                if (itemData.Category == -7 || itemData.Category == -2)
                    continue;

                if (!StardewValley.Object.isPotentialBasicShipped(itemId, itemData.Category, itemData.Type))
                    continue;

                totalShippable++;
                if (player.basicShipped.ContainsKey(itemId))
                    shippedUnique++;
            }

            return (shippedUnique, totalShippable);
        }
        catch
        {
            return (CountCollectionEntries(player.basicShipped), 0);
        }
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
            ? $"{label}: {current}/{total.Value}"
            : $"{label}: {current}";
    }

    private static string FormatProgress(string label, int current, int total, string suffix)
    {
        return $"{label}: {current}/{total}{suffix}";
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

        lines.Add(title);
        foreach (string value in values)
            lines.Add($"- {value}");
        lines.Add(string.Empty);
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

    private static object? GetPropertyValue(object? source, string propertyName)
    {
        if (source == null)
            return null;

        PropertyInfo? prop = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return prop?.GetValue(source);
    }

    private static string ResolveObjectDisplayName(string objectItemId)
    {
        string qualifiedItemId = "(O)" + objectItemId;

        try
        {
            Item created = ItemRegistry.Create(qualifiedItemId, allowNull: false);
            if (!string.IsNullOrWhiteSpace(created.DisplayName))
                return created.DisplayName;
        }
        catch
        {
            // Fall back to parsed item data.
        }

        try
        {
            var data = ItemRegistry.GetDataOrErrorItem(qualifiedItemId);
            if (!string.IsNullOrWhiteSpace(data.DisplayName))
                return data.DisplayName;
        }
        catch
        {
            // Final fallback below.
        }

        return objectItemId;
    }

    private List<string> BuildFriendshipLines(Farmer player, bool detailsEnabled, out int completeTargets, out int totalTargets, out int totalHeartsRemaining)
    {
        List<string> lines = new();
        List<FriendshipTargetStatus> targets = this.GetFriendshipTargets(player);

        totalTargets = targets.Count;
        List<FriendshipTargetStatus> incomplete = targets
            .Where(target => target.HeartsRemaining > 0)
            .OrderByDescending(target => target.HeartsRemaining)
            .ThenBy(target => target.NpcName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        completeTargets = totalTargets - incomplete.Count;
        totalHeartsRemaining = incomplete.Sum(target => target.HeartsRemaining);

        lines.Add($"Friendship targets tracked: {totalTargets}");
        lines.Add($"Targets complete: {completeTargets}");
        lines.Add($"Targets incomplete: {incomplete.Count}");
        lines.Add($"Total hearts remaining: {totalHeartsRemaining}");

        if (totalTargets == 0)
        {
            lines.Add(string.Empty);
            lines.Add("No friendship targets found in this save state.");
            return lines;
        }

        if (incomplete.Count == 0)
        {
            lines.Add(string.Empty);
            lines.Add("All tracked friendship targets are complete.");
            return lines;
        }

        if (!detailsEnabled)
        {
            lines.Add(string.Empty);
            lines.Add("Detailed NPC names and gift examples are hidden in summary-only mode.");
            lines.Add("Enable detailed spoilers and disable category-summary lock to see exact friendship targets.");
            return lines;
        }

        lines.Add(string.Empty);
        lines.Add("Top incomplete friendship targets");
        foreach (FriendshipTargetStatus target in incomplete)
        {
            string giftHint = this.GetGiftHintForNpc(target.Npc);
            lines.Add($"- {target.NpcName}: {target.CurrentHearts}/{target.TargetHearts} hearts ({target.HeartsRemaining} remaining){giftHint}");
        }

        return lines;
    }

    private List<FriendshipTargetStatus> GetFriendshipTargets(Farmer player)
    {
        List<FriendshipTargetStatus> targets = new();
        HashSet<string> seenNpcNames = new(StringComparer.OrdinalIgnoreCase);

        IEnumerable<NPC> candidates = this.GetFriendshipCandidateNpcs(player);
        foreach (NPC npc in candidates)
        {
            if (string.IsNullOrWhiteSpace(npc.Name))
                continue;
            if (!seenNpcNames.Add(npc.Name))
                continue;

            StardewValley.GameData.Characters.CharacterData? npcData = TryGetNpcData(npc);
            if (!IsPerfectionFriendshipTarget(npc, npcData))
                continue;

            int targetHearts = GetPerfectionTargetHearts(npcData);
            int currentHearts = Math.Clamp(player.getFriendshipHeartLevelForNPC(npc.Name), 0, targetHearts);

            targets.Add(new FriendshipTargetStatus
            {
                Npc = npc,
                NpcName = npc.displayName ?? npc.Name,
                CurrentHearts = currentHearts,
                TargetHearts = targetHearts,
                HeartsRemaining = Math.Max(0, targetHearts - currentHearts)
            });
        }

        return targets;
    }

    private IEnumerable<NPC> GetFriendshipCandidateNpcs(Farmer player)
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        List<NPC> npcs = new();

        try
        {
            foreach (Character character in Utility.getAllCharacters())
            {
                if (character is not NPC npc)
                    continue;
                if (!npc.IsVillager)
                    continue;
                if (string.IsNullOrWhiteSpace(npc.Name))
                    continue;
                if (names.Add(npc.Name))
                    npcs.Add(npc);
            }
        }
        catch
        {
            // Fallback to friendship keys below.
        }

        if (npcs.Count == 0)
        {
            foreach (string npcName in EnumerateEntries(player.friendshipData).Select(pair => pair.Key))
            {
                if (string.IsNullOrWhiteSpace(npcName))
                    continue;
                if (!names.Add(npcName))
                    continue;

                if (Game1.getCharacterFromName(npcName, mustBeVillager: true) is NPC npc)
                    npcs.Add(npc);
            }
        }

        return npcs;
    }

    private static StardewValley.GameData.Characters.CharacterData? TryGetNpcData(NPC npc)
    {
        try
        {
            return npc.GetData();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPerfectionFriendshipTarget(NPC npc, StardewValley.GameData.Characters.CharacterData? npcData)
    {
        if (!npc.IsVillager || string.IsNullOrWhiteSpace(npc.Name))
            return false;

        if (npcData?.PerfectionScore != true)
            return false;

        if (!npc.CanReceiveGifts())
            return false;

        if (!npc.CanSocialize)
            return false;

        return true;
    }

    private static int GetPerfectionTargetHearts(StardewValley.GameData.Characters.CharacterData? npcData)
    {
        bool isDatable = npcData?.CanBeRomanced == true;

        return isDatable ? 8 : 10;
    }

    private string GetGiftHintForNpc(NPC npc)
    {
        if (string.IsNullOrWhiteSpace(npc.Name))
            return string.Empty;

        if (this.giftHintByNpcName.TryGetValue(npc.Name, out string? cachedHint))
            return cachedHint;

        string hint = string.Empty;

        try
        {
            Dictionary<string, StardewValley.GameData.Objects.ObjectData> objectDataById =
                this.helper.GameContent.Load<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>("Data/Objects");

            List<string> loved = new();
            List<string> liked = new();
            HashSet<string> seenNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (string objectId in objectDataById.Keys.OrderBy(id => id, StringComparer.Ordinal))
            {
                Item? giftItem = null;
                try
                {
                    giftItem = ItemRegistry.Create("(O)" + objectId, allowNull: true);
                }
                catch
                {
                    // Skip invalid object entries for gift checks.
                }

                if (giftItem == null)
                    continue;

                int taste = npc.getGiftTasteForThisItem(giftItem);
                if (taste == GiftTasteLoveCode)
                {
                    string displayName = ResolveObjectDisplayName(objectId);
                    if (seenNames.Add(displayName))
                        loved.Add(displayName);
                }
                else if (taste == GiftTasteLikeCode)
                {
                    string displayName = ResolveObjectDisplayName(objectId);
                    if (seenNames.Add(displayName))
                        liked.Add(displayName);
                }

                if (loved.Count >= 2 && liked.Count >= 2)
                    break;
            }

            if (loved.Count > 0)
            {
                hint = $" | gift ideas: {string.Join(", ", loved.Take(2))}";
            }
            else if (liked.Count > 0)
            {
                hint = $" | liked gifts: {string.Join(", ", liked.Take(2))}";
            }
        }
        catch
        {
            // Keep friendship line without gift hints if gift data lookup fails.
        }

        this.giftHintByNpcName[npc.Name] = hint;
        return hint;
    }

    private static int GetNpcGiftTasteCode(string fieldName, int fallback)
    {
        try
        {
            FieldInfo? field = typeof(NPC).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field?.GetValue(null) is int value)
                return value;
        }
        catch
        {
            // Keep fallback.
        }

        return fallback;
    }

    private List<string> BuildSeasonalCropLines(Farmer player, bool detailsEnabled)
    {
        List<string> lines = new();

        try
        {
            Dictionary<string, StardewValley.GameData.Crops.CropData> cropDataBySeedId =
                this.helper.GameContent.Load<Dictionary<string, StardewValley.GameData.Crops.CropData>>("Data/Crops");
            Dictionary<string, StardewValley.GameData.Objects.ObjectData> objectDataById =
                this.helper.GameContent.Load<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>("Data/Objects");

            Dictionary<string, SeasonalCropCandidate> bestByHarvestId = new(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, StardewValley.GameData.Crops.CropData> pair in cropDataBySeedId)
            {
                string seedItemId = pair.Key;
                StardewValley.GameData.Crops.CropData cropData = pair.Value;

                if (string.IsNullOrWhiteSpace(cropData.HarvestItemId))
                    continue;
                if (!objectDataById.TryGetValue(cropData.HarvestItemId, out StardewValley.GameData.Objects.ObjectData? harvestObjectData))
                    continue;
                if (!StardewValley.Object.isPotentialBasicShipped(cropData.HarvestItemId, harvestObjectData.Category, harvestObjectData.Type))
                    continue;
                if (player.basicShipped.ContainsKey(cropData.HarvestItemId))
                    continue;

                int daysToMature = cropData.DaysInPhase?.Where(days => days > 0).Sum() ?? 0;
                if (daysToMature <= 0)
                    continue;

                IReadOnlyList<Season> seasons = cropData.Seasons?.ToArray() ?? Array.Empty<Season>();
                string harvestName = ResolveObjectDisplayName(cropData.HarvestItemId);
                string seedName = ResolveObjectDisplayName(seedItemId);

                SeasonalCropCandidate candidate = new()
                {
                    HarvestItemId = cropData.HarvestItemId,
                    SeedItemId = seedItemId,
                    HarvestName = harvestName,
                    SeedName = seedName,
                    DaysToMature = daysToMature,
                    Seasons = seasons
                };

                if (!bestByHarvestId.TryGetValue(candidate.HarvestItemId, out SeasonalCropCandidate? existing)
                    || candidate.DaysToMature < existing.DaysToMature)
                {
                    bestByHarvestId[candidate.HarvestItemId] = candidate;
                }
            }

            Season currentSeason = Game1.season;
            int dayOfMonth = Game1.dayOfMonth;
            int growthDaysLeftThisSeason = Math.Max(0, 28 - dayOfMonth);

            List<SeasonalCropCandidate> missingCandidates = bestByHarvestId.Values
                .OrderBy(candidate => candidate.HarvestName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<SeasonalCropCandidate> viableNow = new();
            List<SeasonalCropCandidate> tooLateThisSeason = new();
            List<SeasonalCropCandidate> notThisSeason = new();

            foreach (SeasonalCropCandidate candidate in missingCandidates)
            {
                bool growsThisSeason = candidate.Seasons.Count == 0 || candidate.Seasons.Contains(currentSeason);
                if (!growsThisSeason)
                {
                    notThisSeason.Add(candidate);
                    continue;
                }

                if (candidate.DaysToMature <= growthDaysLeftThisSeason)
                    viableNow.Add(candidate);
                else
                    tooLateThisSeason.Add(candidate);
            }

            lines.Add($"Season: {currentSeason} Day {dayOfMonth} (growth days left: {growthDaysLeftThisSeason})");
            lines.Add($"Missing shippable crop outputs: {missingCandidates.Count}");
            lines.Add($"Plant now + can mature this season: {viableNow.Count}");
            lines.Add($"Plantable now but too late this season: {tooLateThisSeason.Count}");
            lines.Add($"Not growable this season: {notThisSeason.Count}");

            if (!detailsEnabled)
            {
                lines.Add(string.Empty);
                lines.Add("Detailed crop names are hidden in summary-only mode.");
                lines.Add("Enable detailed spoilers and disable category-summary lock to see exact crop recommendations.");
                return lines;
            }

            AddSeasonalDetail(lines, "Plant now (viable this season)", viableNow.Take(8), includeTimeLeft: false, growthDaysLeftThisSeason);
            AddSeasonalDetail(lines, "Too late this season", tooLateThisSeason.Take(8), includeTimeLeft: true, growthDaysLeftThisSeason);
            AddSeasonalDetail(lines, "Not in current season", notThisSeason.Take(8), includeTimeLeft: false, growthDaysLeftThisSeason);
        }
        catch
        {
            lines.Add("Seasonal crop viability data unavailable.");
            lines.Add("Could not load crop/object game data for this save.");
        }

        return lines;
    }

    private static void AddSeasonalDetail(
        List<string> lines,
        string title,
        IEnumerable<SeasonalCropCandidate> candidates,
        bool includeTimeLeft,
        int growthDaysLeftThisSeason)
    {
        SeasonalCropCandidate[] values = candidates.ToArray();
        lines.Add(string.Empty);
        lines.Add(title);

        if (values.Length == 0)
        {
            lines.Add("- none");
            return;
        }

        foreach (SeasonalCropCandidate candidate in values)
        {
            string seasonText = candidate.Seasons.Count == 0
                ? "any season"
                : string.Join("/", candidate.Seasons.Select(season => season.ToString()));

            if (includeTimeLeft)
            {
                lines.Add($"- {candidate.HarvestName} ({candidate.SeedName}) needs {candidate.DaysToMature}d, {growthDaysLeftThisSeason}d left");
            }
            else
            {
                lines.Add($"- {candidate.HarvestName} ({candidate.SeedName}) {candidate.DaysToMature}d ({seasonText})");
            }
        }
    }
}
