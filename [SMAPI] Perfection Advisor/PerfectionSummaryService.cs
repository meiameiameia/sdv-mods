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

    private sealed class SeasonalActionSnapshot
    {
        public int DayOfMonth { get; init; }
        public Season CurrentSeason { get; init; }
        public int GrowthDaysLeftThisSeason { get; init; }
        public List<SeasonalCropCandidate> MissingCandidates { get; init; } = new();
        public List<SeasonalCropCandidate> ViableNow { get; init; } = new();
        public List<SeasonalCropCandidate> TooLateThisSeason { get; init; } = new();
        public List<SeasonalCropCandidate> NotThisSeason { get; init; } = new();
    }

    private sealed class FishAvailabilityCandidate
    {
        public string FishName { get; init; } = string.Empty;
        public bool UsesSeasonWeatherTimeModel { get; init; }
        public string SeasonsText { get; init; } = "unknown";
        public string WeatherText { get; init; } = "unknown";
        public string LocationText { get; init; } = "unknown";
        public IReadOnlyList<(int Start, int End)> TimeWindows { get; init; } = Array.Empty<(int, int)>();
        public bool IsInCurrentSeason { get; init; }
        public bool MatchesCurrentWeather { get; init; }
        public bool IsLocationAware { get; init; }
        public bool MatchesCurrentLocation { get; init; }
        public bool IsAvailableNow { get; init; }
        public bool IsAvailableLaterToday { get; init; }
        public string CategoryLabel { get; init; } = string.Empty;
    }

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Dictionary<string, string> giftHintByNpcName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly int GiftTasteLoveCode = GetNpcGiftTasteCode("gift_taste_love", fallback: 0);
    private static readonly int GiftTasteLikeCode = GetNpcGiftTasteCode("gift_taste_like", fallback: 2);
    private static readonly HashSet<string> ValidSeasonTokens = new(StringComparer.OrdinalIgnoreCase) { "spring", "summer", "fall", "winter" };
    private const string WeddingRingRecipeName = "Wedding Ring";

    public PerfectionSummaryService(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
    }

    public PerfectionAdvisorSnapshot BuildSnapshot(ModConfig config)
    {
        if (!Context.IsWorldReady)
        {
            return new PerfectionAdvisorSnapshot(
                "No save loaded",
                new[] { "Load a save to view advisor data." },
                new[] { "No progress data available." },
                new[] { "No fish guidance data available." },
                new[] { "No blocker data available." },
                new[] { "No friendship data available." },
                new[] { "No seasonal data available." },
                new[] { "No daily-action data available." },
                new[] { "Detailed spoilers are disabled." });
        }

        Farmer player = Game1.player;
        bool detailsEnabled = config.EnableDetailedSpoilers && !config.ShowOnlyCategorySummary;

        List<string> degradedSections = new();

        (int shippedUnique, int? shippedTotal, bool shippedFallback) = this.GetCanonicalShippedProgress(player);
        if (shippedFallback)
            degradedSections.Add("Shipped");

        int fishCaught = CountCollectionEntries(player.fishCaught);
        int cookedRecipes = CountPlayerProgressEntries(player, "recipesCooked", player.cookingRecipes);
        (int craftedRecipes, int? totalCraftingRecipes, bool craftingFallback) = this.GetCraftingProgressExcludingWeddingRing(player);
        if (craftingFallback)
            degradedSections.Add("Crafting");

        List<string> friendshipLines = this.BuildFriendshipLines(player, detailsEnabled, out int completeFriendshipTargets, out int totalFriendshipTargets, out int totalHeartsRemaining);

        int? totalFish = this.TryLoadCount("Data/Fish");
        int? totalCookingRecipes = this.TryLoadCount("Data/CookingRecipes");
        if (!totalFish.HasValue)
            degradedSections.Add("Fish totals");
        if (!totalCookingRecipes.HasValue)
            degradedSections.Add("Cooking totals");
        if (!totalCraftingRecipes.HasValue)
            degradedSections.Add("Crafting totals");

        List<string> overviewLines = new()
        {
            detailsEnabled ? "Detailed spoiler mode is ON." : "Summary-only mode is ON.",
            config.ShowOnlyCategorySummary ? "Category summary lock is ON." : "Category summary lock is OFF.",
            "Coverage is a tracked subset for planning, not full perfection parity.",
            "Current-player view only; farm-wide multiplayer perfection is not modeled in this slice.",
            "Shipped progress mirrors Collections/perfection semantics when canonical data is available.",
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
        List<string> fishLines = this.BuildFishGuidanceLines(player, detailsEnabled, out bool fishDegraded);
        if (fishDegraded)
            degradedSections.Add("Fish");

        SeasonalActionSnapshot? seasonalSnapshot = this.TryBuildSeasonalActionSnapshot(player);
        if (seasonalSnapshot == null)
            degradedSections.Add("Seasonal");

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

        List<string> seasonalLines = this.BuildSeasonalCropLines(seasonalSnapshot, detailsEnabled);
        List<string> todayLines = this.BuildTodayActionLines(
            player,
            detailsEnabled,
            seasonalSnapshot,
            fishCaught,
            totalFish,
            cookedRecipes,
            totalCookingRecipes,
            craftedRecipes,
            totalCraftingRecipes);

        if (degradedSections.Count > 0)
            overviewLines.Add($"Data quality: degraded sections -> {string.Join(", ", degradedSections.Distinct(StringComparer.OrdinalIgnoreCase))}.");

        if (degradedSections.Count > 0)
        {
            this.monitor.Log(
                $"Perfection Advisor snapshot built with degraded sections: {string.Join(", ", degradedSections.Distinct(StringComparer.OrdinalIgnoreCase))}.",
                LogLevel.Debug);
        }

        return new PerfectionAdvisorSnapshot(
            detailsEnabled ? "Detailed spoilers enabled" : "Summary-only mode",
            overviewLines,
            progressLines,
            fishLines,
            blockerLines,
            friendshipLines,
            seasonalLines,
            todayLines,
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

    private (int ShippedUnique, int? TotalShippable, bool UsedFallback) GetCanonicalShippedProgress(Farmer player)
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

            if (totalShippable <= 0)
                return (shippedUnique, null, true);

            return (shippedUnique, totalShippable, false);
        }
        catch
        {
            return (CountCollectionEntries(player.basicShipped), null, true);
        }
    }

    private (int CraftedCount, int? TotalCraftable, bool UsedFallback) GetCraftingProgressExcludingWeddingRing(Farmer player)
    {
        try
        {
            Dictionary<string, string> allRecipes = this.helper.GameContent.Load<Dictionary<string, string>>("Data/CraftingRecipes");
            int totalCraftable = allRecipes.Keys.Count(recipeName => !IsWeddingRingRecipe(recipeName));

            object? canonicalSource = GetPropertyValue(player, "recipesCrafted") ?? player.craftingRecipes;
            HashSet<string> craftedKeys = GetPositiveKeys(canonicalSource);
            int craftedCount = craftedKeys.Count(recipeName => !IsWeddingRingRecipe(recipeName));

            return (craftedCount, totalCraftable, false);
        }
        catch
        {
            object? canonicalSource = GetPropertyValue(player, "recipesCrafted") ?? player.craftingRecipes;
            HashSet<string> craftedKeys = GetPositiveKeys(canonicalSource);
            int craftedCount = craftedKeys.Count(recipeName => !IsWeddingRingRecipe(recipeName));
            return (craftedCount, null, true);
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

    private List<string> BuildFishGuidanceLines(Farmer player, bool detailsEnabled, out bool degraded)
    {
        List<string> lines = new();
        degraded = false;

        List<FishAvailabilityCandidate>? missingFish = this.TryBuildMissingFishAvailability(player);
        if (missingFish == null)
        {
            degraded = true;
            lines.Add("Fish availability guidance unavailable.");
            lines.Add("Could not load fish data for this save.");
            return lines;
        }

        string seasonNow = Game1.season.ToString();
        string weatherNow = Game1.isRaining ? "rain" : "sun";
        string timeNow = FormatTimeOfDayDisplay(Game1.timeOfDay);

        List<FishAvailabilityCandidate> temporalFish = missingFish
            .Where(fish => fish.UsesSeasonWeatherTimeModel)
            .ToList();
        List<FishAvailabilityCandidate> locationAwareTemporalFish = temporalFish
            .Where(fish => fish.IsLocationAware)
            .ToList();
        List<FishAvailabilityCandidate> locationUnresolvedTemporalFish = temporalFish
            .Where(fish => !fish.IsLocationAware)
            .ToList();
        List<FishAvailabilityCandidate> nonstandardFish = missingFish
            .Where(fish => !fish.UsesSeasonWeatherTimeModel)
            .OrderBy(fish => fish.FishName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<FishAvailabilityCandidate> availableNow = temporalFish
            .Where(fish => fish.IsAvailableNow)
            .OrderBy(fish => fish.FishName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        List<FishAvailabilityCandidate> laterToday = temporalFish
            .Where(fish => !fish.IsAvailableNow && fish.IsAvailableLaterToday)
            .OrderBy(fish => GetNextWindowStartAfter(Game1.timeOfDay, fish.TimeWindows) ?? int.MaxValue)
            .ThenBy(fish => fish.FishName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        List<FishAvailabilityCandidate> notNowThisSeason = temporalFish
            .Where(fish => fish.IsInCurrentSeason && fish.IsLocationAware && !fish.IsAvailableNow && !fish.IsAvailableLaterToday)
            .OrderBy(fish => fish.FishName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        List<FishAvailabilityCandidate> notThisSeason = temporalFish
            .Where(fish => !fish.IsInCurrentSeason)
            .OrderBy(fish => fish.FishName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        lines.Add($"Now: {seasonNow} Day {Game1.dayOfMonth}, {timeNow}, weather {weatherNow}");
        lines.Add("Scope: tracked fish subset with rod-model location checks where canonical location signals are parseable.");
        lines.Add($"Missing fish tracked: {missingFish.Count}");
        lines.Add($"Rod-catch season/weather/time model entries: {temporalFish.Count}");
        lines.Add($"Rod-catch entries with modeled location signals: {locationAwareTemporalFish.Count}");
        lines.Add($"Rod-catch entries with unresolved location signals: {locationUnresolvedTemporalFish.Count}");
        lines.Add($"Location-aware catchable now: {availableNow.Count}");
        lines.Add($"Location-aware catchable later today: {laterToday.Count}");
        lines.Add($"Current season but blocked by time/weather/location right now: {notNowThisSeason.Count}");
        lines.Add($"Rod-catch model but not in current season: {notThisSeason.Count}");
        lines.Add($"Nonstandard or alternate-acquisition entries: {nonstandardFish.Count}");
        lines.Add("Note: method-specific constraints and special-case logic are still partial.");

        if (!detailsEnabled)
        {
            lines.Add(string.Empty);
            lines.Add("Detailed fish names and windows are hidden in summary-only mode.");
            lines.Add("Enable detailed spoilers and disable category-summary lock to see exact fish guidance.");
            return lines;
        }

        AddFishDetail(lines, "Location-aware catchable now", availableNow);
        AddFishDetail(lines, "Location-aware catchable later today", laterToday);
        AddFishDetail(lines, "Current season but not available right now (includes location-aware checks)", notNowThisSeason);
        AddFishDetail(lines, "Rod-model with unresolved location signals", locationUnresolvedTemporalFish);
        AddFishDetail(lines, "Not available this season", notThisSeason);
        AddNonstandardFishDetail(lines, "Nonstandard / alternate-acquisition entries", nonstandardFish);
        return lines;
    }

    private List<FishAvailabilityCandidate>? TryBuildMissingFishAvailability(Farmer player)
    {
        try
        {
            Dictionary<string, string> fishData = this.helper.GameContent.Load<Dictionary<string, string>>("Data/Fish");
            Dictionary<string, StardewValley.GameData.Objects.ObjectData> objectDataById =
                this.helper.GameContent.Load<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>("Data/Objects");
            HashSet<string> caught = GetPositiveKeys(player.fishCaught);
            List<FishAvailabilityCandidate> missing = new();

            string currentSeason = Game1.season.ToString().ToLowerInvariant();
            bool isRaining = Game1.isRaining;
            int currentTime = Game1.timeOfDay;
            HashSet<string> currentLocationSignals = GetCurrentLocationSignals(player.currentLocation);

            foreach (KeyValuePair<string, string> pair in fishData)
            {
                if (caught.Contains(pair.Key))
                    continue;

                string[] parts = pair.Value.Split('/');
                string fishName = ExtractNameFromDataEntry(pair.Value, fallback: pair.Key);
                objectDataById.TryGetValue(pair.Key, out StardewValley.GameData.Objects.ObjectData? objectData);
                bool isRodCatchType = IsRodCatchObjectType(objectData);
                bool hasAlternateAcquisitionMarkers = HasAlternateAcquisitionMarkers(parts);
                bool isRodCatchEligible = isRodCatchType && !hasAlternateAcquisitionMarkers;

                if (isRodCatchEligible
                    && TryParseSeasonWeatherTimeModel(
                        parts,
                        out List<(int Start, int End)> windows,
                        out HashSet<string> seasonTokens,
                        out HashSet<string> weatherTokens,
                        out string locationFieldRaw))
                {
                    bool hasSeasonRestriction = seasonTokens.Count > 0;
                    bool isInCurrentSeason = !hasSeasonRestriction || seasonTokens.Contains(currentSeason);
                    bool matchesCurrentWeather = WeatherMatches(weatherTokens, isRaining);
                    bool inTimeWindowNow = IsTimeInAnyWindow(currentTime, windows);
                    bool matchesTemporalNow = isInCurrentSeason && matchesCurrentWeather && inTimeWindowNow;
                    bool matchesTemporalLaterToday = !matchesTemporalNow && isInCurrentSeason && matchesCurrentWeather && HasFutureWindowToday(currentTime, windows);

                    bool isLocationAware = TryParseRodLocationSignals(locationFieldRaw, out HashSet<string> fishLocationSignals, out bool locationEverywhere);
                    bool matchesCurrentLocation = isLocationAware
                        && (locationEverywhere || LocationSignalsMatch(fishLocationSignals, currentLocationSignals));
                    bool isAvailableNow = matchesTemporalNow && matchesCurrentLocation;
                    bool isAvailableLaterToday = !isAvailableNow && matchesTemporalLaterToday && matchesCurrentLocation;

                    string seasonsText = hasSeasonRestriction
                        ? string.Join("/", seasonTokens.OrderBy(token => token, StringComparer.OrdinalIgnoreCase))
                        : "any";
                    string weatherText = FormatWeatherDisplay(weatherTokens);
                    string locationText = isLocationAware
                        ? FormatLocationSignalsDisplay(fishLocationSignals, locationEverywhere)
                        : "unresolved";

                    missing.Add(new FishAvailabilityCandidate
                    {
                        FishName = fishName,
                        UsesSeasonWeatherTimeModel = true,
                        SeasonsText = seasonsText,
                        WeatherText = weatherText,
                        LocationText = locationText,
                        TimeWindows = windows,
                        IsInCurrentSeason = isInCurrentSeason,
                        MatchesCurrentWeather = matchesCurrentWeather,
                        IsLocationAware = isLocationAware,
                        MatchesCurrentLocation = matchesCurrentLocation,
                        IsAvailableNow = isAvailableNow,
                        IsAvailableLaterToday = isAvailableLaterToday,
                        CategoryLabel = isLocationAware
                            ? "Rod-catch season/weather/time/location model"
                            : "Rod-catch model with unresolved location signals"
                    });
                }
                else
                {
                    string categoryLabel = hasAlternateAcquisitionMarkers
                        ? "Alternate acquisition entry (non-rod/trap-style data)"
                        : isRodCatchType
                        ? "Rod-fish type with incompatible fish-data shape"
                        : $"Non-rod object type ({objectData?.Type ?? "unknown"})";
                    missing.Add(new FishAvailabilityCandidate
                    {
                        FishName = fishName,
                        UsesSeasonWeatherTimeModel = false,
                        CategoryLabel = categoryLabel
                    });
                }
            }

            return missing;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseSeasonWeatherTimeModel(
        string[] parts,
        out List<(int Start, int End)> windows,
        out HashSet<string> seasonTokens,
        out HashSet<string> weatherTokens,
        out string locationFieldRaw)
    {
        windows = new List<(int Start, int End)>();
        seasonTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        weatherTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        locationFieldRaw = string.Empty;

        for (int timeIndex = 0; timeIndex < parts.Length; timeIndex++)
        {
            if (TryParseFishTimeWindows(parts[timeIndex], out List<(int Start, int End)> parsedWindows))
            {
                // Strict shape gate: season and weather must be the next two fields.
                if (timeIndex + 2 >= parts.Length)
                    continue;
                if (!TryParseSeasonField(parts[timeIndex + 1], out HashSet<string> parsedSeasons))
                    continue;
                if (!TryParseWeatherField(parts[timeIndex + 2], out HashSet<string> parsedWeather))
                    continue;

                windows = parsedWindows;
                seasonTokens = parsedSeasons;
                weatherTokens = parsedWeather;
                locationFieldRaw = timeIndex > 0 ? parts[timeIndex - 1] : string.Empty;
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> GetCurrentLocationSignals(GameLocation location)
    {
        HashSet<string> signals = new(StringComparer.OrdinalIgnoreCase);
        string raw = $"{location.NameOrUniqueName} {location.GetType().Name}";
        AddLocationSignalsFromFragment(raw, signals);

        string lowered = raw.ToLowerInvariant();
        if (lowered.Contains("town"))
            signals.Add("river");
        if (lowered.Contains("mountain"))
        {
            signals.Add("lake");
            signals.Add("river");
        }
        if (lowered.Contains("forest") || lowered.Contains("woods") || lowered.Contains("backwoods"))
            signals.Add("river");
        if (lowered.Contains("beach") || lowered.Contains("ocean"))
        {
            signals.Add("beach");
            signals.Add("ocean");
        }
        if (lowered.Contains("island"))
        {
            signals.Add("island");
            signals.Add("ocean");
        }
        if (lowered.Contains("sewer"))
            signals.Add("sewer");
        if (lowered.Contains("desert"))
            signals.Add("desert");
        if (lowered.Contains("mine"))
            signals.Add("mine");
        if (lowered.Contains("submarine"))
            signals.Add("submarine");

        return signals;
    }

    private static bool TryParseRodLocationSignals(string source, out HashSet<string> locationSignals, out bool locationEverywhere)
    {
        locationSignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        locationEverywhere = false;
        if (string.IsNullOrWhiteSpace(source))
            return false;

        HashSet<string> rawTokens = TokenizeLower(source);
        if (rawTokens.Count == 0)
            return false;

        if (rawTokens.Contains("any") || rawTokens.Contains("all") || rawTokens.Contains("both"))
        {
            locationEverywhere = true;
            return true;
        }

        AddLocationSignalsFromFragment(source, locationSignals);
        return locationSignals.Count > 0;
    }

    private static void AddLocationSignalsFromFragment(string source, HashSet<string> signals)
    {
        string lowered = source.ToLowerInvariant();
        if (lowered.Contains("beach"))
        {
            signals.Add("beach");
            signals.Add("ocean");
        }
        if (lowered.Contains("ocean"))
            signals.Add("ocean");
        if (lowered.Contains("river"))
            signals.Add("river");
        if (lowered.Contains("lake"))
            signals.Add("lake");
        if (lowered.Contains("town"))
            signals.Add("town");
        if (lowered.Contains("mountain"))
            signals.Add("mountain");
        if (lowered.Contains("forest") || lowered.Contains("woods"))
            signals.Add("forest");
        if (lowered.Contains("desert"))
            signals.Add("desert");
        if (lowered.Contains("sewer"))
            signals.Add("sewer");
        if (lowered.Contains("island"))
            signals.Add("island");
        if (lowered.Contains("mine"))
            signals.Add("mine");
        if (lowered.Contains("submarine"))
            signals.Add("submarine");
        if (lowered.Contains("mutant"))
            signals.Add("sewer");
    }

    private static bool LocationSignalsMatch(HashSet<string> fishLocationSignals, HashSet<string> currentLocationSignals)
    {
        return fishLocationSignals.Count > 0 && fishLocationSignals.Overlaps(currentLocationSignals);
    }

    private static string FormatLocationSignalsDisplay(HashSet<string> locationSignals, bool locationEverywhere)
    {
        if (locationEverywhere)
            return "any";
        if (locationSignals.Count == 0)
            return "unknown";
        return string.Join("/", locationSignals.OrderBy(token => token, StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasAlternateAcquisitionMarkers(string[] parts)
    {
        foreach (string part in parts)
        {
            HashSet<string> tokens = TokenizeLower(part);
            if (tokens.Contains("trap") || tokens.Contains("traps") || tokens.Contains("crabpot") || tokens.Contains("crab_pot"))
                return true;
        }

        return false;
    }

    private static bool IsRodCatchObjectType(StardewValley.GameData.Objects.ObjectData? objectData)
    {
        if (objectData == null)
            return false;

        return string.Equals(objectData.Type, "Fish", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseFishTimeWindows(string source, out List<(int Start, int End)> windows)
    {
        windows = new List<(int Start, int End)>();
        if (string.IsNullOrWhiteSpace(source))
            return false;

        string[] tokens = source.Split(new[] { ' ', ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
        List<int> times = new();
        foreach (string token in tokens)
        {
            if (!int.TryParse(token, out int timeValue))
                return false;
            if (!IsValidSdvTime(timeValue))
                return false;
            times.Add(timeValue);
        }

        if (times.Count < 2 || times.Count % 2 != 0)
            return false;

        for (int i = 0; i < times.Count; i += 2)
            windows.Add((times[i], times[i + 1]));

        return windows.Count > 0;
    }

    private static bool IsValidSdvTime(int timeValue)
    {
        if (timeValue < 0 || timeValue > 2600)
            return false;

        int minutes = timeValue % 100;
        return minutes >= 0 && minutes < 60 && timeValue % 10 == 0;
    }

    private static bool TryParseSeasonField(string source, out HashSet<string> seasonTokens)
    {
        seasonTokens = TokenizeLower(source);
        if (seasonTokens.Count == 0)
            return false;

        return seasonTokens.All(token => ValidSeasonTokens.Contains(token));
    }

    private static bool TryParseWeatherField(string source, out HashSet<string> weatherTokens)
    {
        weatherTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> tokens = TokenizeLower(source);
        if (tokens.Count == 0)
            return false;

        foreach (string token in tokens)
        {
            switch (token)
            {
                case "both":
                case "any":
                    weatherTokens.Add("sun");
                    weatherTokens.Add("rain");
                    break;
                case "sun":
                case "sunny":
                    weatherTokens.Add("sun");
                    break;
                case "rain":
                case "rainy":
                    weatherTokens.Add("rain");
                    break;
                default:
                    return false;
            }
        }

        return weatherTokens.Count > 0;
    }

    private static HashSet<string> TokenizeLower(string source)
    {
        return source
            .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim().ToLowerInvariant())
            .Where(token => token.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool WeatherMatches(HashSet<string> weatherTokens, bool isRaining)
    {
        if (weatherTokens.Count == 0)
            return true;
        return isRaining ? weatherTokens.Contains("rain") : weatherTokens.Contains("sun");
    }

    private static bool IsTimeInAnyWindow(int currentTime, IReadOnlyList<(int Start, int End)> windows)
    {
        foreach ((int start, int end) in windows)
        {
            if (end < start)
            {
                if (currentTime >= start || currentTime <= end)
                    return true;
            }
            else if (currentTime >= start && currentTime <= end)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasFutureWindowToday(int currentTime, IReadOnlyList<(int Start, int End)> windows)
    {
        return windows.Any(window => window.Start > currentTime && window.Start <= 2600);
    }

    private static int? GetNextWindowStartAfter(int currentTime, IReadOnlyList<(int Start, int End)> windows)
    {
        return windows
            .Where(window => window.Start > currentTime)
            .Select(window => (int?)window.Start)
            .DefaultIfEmpty(null)
            .Min();
    }

    private static string FormatWeatherDisplay(HashSet<string> weatherTokens)
    {
        bool sun = weatherTokens.Contains("sun");
        bool rain = weatherTokens.Contains("rain");
        if (sun && rain)
            return "both";
        if (rain)
            return "rainy";
        if (sun)
            return "sunny";
        return "unknown";
    }

    private static void AddFishDetail(List<string> lines, string title, IEnumerable<FishAvailabilityCandidate> fish)
    {
        FishAvailabilityCandidate[] list = fish.ToArray();
        lines.Add(string.Empty);
        lines.Add(title);

        if (list.Length == 0)
        {
            lines.Add("- none");
            return;
        }

        foreach (FishAvailabilityCandidate entry in list)
        {
            string timeText = entry.TimeWindows.Count > 0
                ? string.Join(", ", entry.TimeWindows.Select(window => $"{FormatTimeOfDayDisplay(window.Start)}-{FormatTimeOfDayDisplay(window.End)}"))
                : "time unknown";
            string locationText = entry.IsLocationAware ? entry.LocationText : "unresolved";
            lines.Add($"- {entry.FishName} (time {timeText}, weather {entry.WeatherText}, season {entry.SeasonsText}, location {locationText})");
        }
    }

    private static void AddNonstandardFishDetail(List<string> lines, string title, IEnumerable<FishAvailabilityCandidate> fish)
    {
        FishAvailabilityCandidate[] list = fish.ToArray();
        lines.Add(string.Empty);
        lines.Add(title);

        if (list.Length == 0)
        {
            lines.Add("- none");
            return;
        }

        foreach (FishAvailabilityCandidate entry in list)
            lines.Add($"- {entry.FishName} ({entry.CategoryLabel})");
    }

    private static string FormatTimeOfDayDisplay(int time)
    {
        if (!IsValidSdvTime(time))
            return "unknown";

        try
        {
            return Game1.getTimeOfDayString(time);
        }
        catch
        {
            return time.ToString("0000");
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

    private static string FormatProgress(string label, int current, int? total, string suffix)
    {
        return total.HasValue
            ? $"{label}: {current}/{total.Value}{suffix}"
            : $"{label}: {current}{suffix} (total unavailable)";
    }

    private static bool IsWeddingRingRecipe(string recipeName)
    {
        return string.Equals(recipeName, WeddingRingRecipeName, StringComparison.OrdinalIgnoreCase);
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

    private List<string> BuildTodayActionLines(
        Farmer player,
        bool detailsEnabled,
        SeasonalActionSnapshot? seasonal,
        int fishCaught,
        int? totalFish,
        int cookedRecipes,
        int? totalCookingRecipes,
        int craftedRecipes,
        int? totalCraftingRecipes)
    {
        List<string> lines = new();

        List<FriendshipTargetStatus> incompleteFriendships = this.GetFriendshipTargets(player)
            .Where(target => target.HeartsRemaining > 0)
            .OrderByDescending(target => target.HeartsRemaining)
            .ThenBy(target => target.NpcName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        lines.Add($"Today: {Game1.season} Day {Game1.dayOfMonth}, time {FormatTimeOfDayDisplay(Game1.timeOfDay)}");
        lines.Add("Scope: actionable hints from tracked sections only.");

        if (seasonal != null)
        {
            lines.Add($"Season window: {seasonal.GrowthDaysLeftThisSeason} growth day(s) left in {seasonal.CurrentSeason}.");
            if (seasonal.ViableNow.Count > 0)
                lines.Add($"Time-sensitive crops: {seasonal.ViableNow.Count} tracked crop outputs can still be planted today.");
            else if (seasonal.TooLateThisSeason.Count > 0)
                lines.Add("Time-sensitive crops: no remaining missing crop outputs can mature this season.");
            else
                lines.Add("Time-sensitive crops: no immediate crop shipping deadlines today.");
        }
        else
        {
            lines.Add("Time-sensitive crops: unavailable (crop data could not be loaded).");
        }

        if (incompleteFriendships.Count > 0)
            lines.Add($"Friendship priority: {incompleteFriendships.Count} target(s) still need hearts. Talk + gift at least one target today.");
        else
            lines.Add("Friendship priority: all tracked perfection friendship targets are complete.");

        int? missingFish = totalFish.HasValue ? Math.Max(0, totalFish.Value - fishCaught) : null;
        int? missingCooking = totalCookingRecipes.HasValue ? Math.Max(0, totalCookingRecipes.Value - cookedRecipes) : null;
        int? missingCrafting = totalCraftingRecipes.HasValue ? Math.Max(0, totalCraftingRecipes.Value - craftedRecipes) : null;

        lines.Add(FormatRemainingCount("Missing fish", missingFish));
        lines.Add(FormatRemainingCount("Missing cooked recipes", missingCooking));
        lines.Add(FormatRemainingCount("Missing crafted recipes", missingCrafting));

        if (!detailsEnabled)
        {
            lines.Add(string.Empty);
            lines.Add("Detailed action targets are hidden in summary-only mode.");
            lines.Add("Enable detailed spoilers and disable category-summary lock to see concrete today actions.");
            return lines;
        }

        lines.Add(string.Empty);
        lines.Add("Suggested actions today");

        if (seasonal?.ViableNow.Count > 0)
        {
            lines.Add("Plant today (can mature this season)");
            foreach (SeasonalCropCandidate candidate in seasonal.ViableNow.Take(6))
                lines.Add($"- Plant {candidate.SeedName} to ship {candidate.HarvestName} ({candidate.DaysToMature}d)");
            lines.Add(string.Empty);
        }

        if (incompleteFriendships.Count > 0)
        {
            lines.Add("Friendship actions");
            foreach (FriendshipTargetStatus target in incompleteFriendships.Take(6))
            {
                string giftHint = this.GetGiftHintForNpc(target.Npc);
                lines.Add($"- {target.NpcName}: {target.CurrentHearts}/{target.TargetHearts} hearts ({target.HeartsRemaining} remaining){giftHint}");
            }
            lines.Add(string.Empty);
        }

        AddDetailIfAny(lines, "Missing fish examples to target", this.GetMissingFishNames(maxItems: 4));
        AddDetailIfAny(lines, "Missing cooking recipe examples to complete", this.GetMissingCookingRecipeNames(maxItems: 4));

        if (Game1.timeOfDay >= 2200)
            lines.Add("Late-day note: prioritize quick actions (talk, gift, ship) before sleep.");

        if (lines.Count == 0)
            lines.Add("No high-value daily actions detected right now.");

        return lines;
    }

    private SeasonalActionSnapshot? TryBuildSeasonalActionSnapshot(Farmer player)
    {
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

            return new SeasonalActionSnapshot
            {
                DayOfMonth = dayOfMonth,
                CurrentSeason = currentSeason,
                GrowthDaysLeftThisSeason = growthDaysLeftThisSeason,
                MissingCandidates = missingCandidates,
                ViableNow = viableNow,
                TooLateThisSeason = tooLateThisSeason,
                NotThisSeason = notThisSeason
            };
        }
        catch
        {
            return null;
        }
    }

    private List<string> BuildSeasonalCropLines(SeasonalActionSnapshot? snapshot, bool detailsEnabled)
    {
        List<string> lines = new();
        if (snapshot == null)
        {
            lines.Add("Seasonal crop viability data unavailable.");
            lines.Add("Could not load crop/object game data for this save.");
            return lines;
        }

        lines.Add($"Season: {snapshot.CurrentSeason} Day {snapshot.DayOfMonth} (growth days left: {snapshot.GrowthDaysLeftThisSeason})");
        lines.Add("Scope: tracked crop-ship subset only (not full perfection crops planner).");
        lines.Add($"Missing shippable crop outputs: {snapshot.MissingCandidates.Count}");
        lines.Add($"Plant now + can mature this season: {snapshot.ViableNow.Count}");
        lines.Add($"Plantable now but too late this season: {snapshot.TooLateThisSeason.Count}");
        lines.Add($"Not growable this season: {snapshot.NotThisSeason.Count}");

        if (!detailsEnabled)
        {
            lines.Add(string.Empty);
            lines.Add("Detailed crop names are hidden in summary-only mode.");
            lines.Add("Enable detailed spoilers and disable category-summary lock to see exact crop recommendations.");
            return lines;
        }

        AddSeasonalDetail(lines, "Plant now (viable this season)", snapshot.ViableNow.Take(8), includeTimeLeft: false, snapshot.GrowthDaysLeftThisSeason);
        AddSeasonalDetail(lines, "Too late this season", snapshot.TooLateThisSeason.Take(8), includeTimeLeft: true, snapshot.GrowthDaysLeftThisSeason);
        AddSeasonalDetail(lines, "Not in current season", snapshot.NotThisSeason.Take(8), includeTimeLeft: false, snapshot.GrowthDaysLeftThisSeason);
        return lines;
    }

    private static string FormatRemainingCount(string label, int? remaining)
    {
        return remaining.HasValue
            ? $"{label}: {remaining.Value} remaining"
            : $"{label}: unavailable";
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
