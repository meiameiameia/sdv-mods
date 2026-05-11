using System.Collections;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using Meiameiameia.PowerGrid.Core;
using Object = StardewValley.Object;

namespace Meiameiameia.PowerGrid.Integrations;

internal static class UiInfoSuiteIntegration
{
    private const string UiInfoSuite2UniqueId = "Annosz.UiInfoSuite2";
    private const string UiInfoSuite2AltUniqueId = "DazUki.UIInfoSuite2Alt";
    private const string MdPrefix = "meiameiameia.PowerGrid/";
    private const string MdType = MdPrefix + "type";
    private const string MdNetworkId = MdPrefix + "networkId";
    private const string MdEuPerTick = MdPrefix + "euPerTick";
    private const string MdGeneratedThisTick = MdPrefix + "generatedThisTick";
    private const string MdRequiresFuel = MdPrefix + "requiresFuel";
    private const string MdFuelTicksRemaining = MdPrefix + "fuelTicksRemaining";
    private const string MdOnline = MdPrefix + "online";
    private const string MdCharge = MdPrefix + "charge";
    private const string MdCapacity = MdPrefix + "capacity";
    private const string MdChargePercent = MdPrefix + "chargePercent";
    private const string MdDrainedThisTick = MdPrefix + "drainedThisTick";
    private const string MdStoredThisTick = MdPrefix + "storedThisTick";
    private const string MdPowered = MdPrefix + "powered";
    private const string MdEnergized = MdPrefix + "energized";
    private const string MdEuAllocated = MdPrefix + "euAllocated";
    private const string MdEuDemanded = MdPrefix + "euDemanded";
    private const string MdSpeedup = MdPrefix + "speedupFraction";
    private const string MdMinutesAccelerated = MdPrefix + "minutesAccelerated";
    private const string MdMinutesRemaining = MdPrefix + "minutesRemaining";
    private const string MetalCaskMarkerKey = MdPrefix + "MetalCask";
    private const string MetalCaskDaysToNextQualityKey = MdPrefix + "MetalCaskDaysToNextQuality";
    private const float MetalCaskBaseMaturityPerDay = 0.50f;
    private const float MetalCaskPoweredBonusMaturityPerDayAtFullPower = 1.50f;

    private static readonly FieldInfo? CaskDaysToMatureField = typeof(Cask).GetField("daysToMature", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CaskAgingRateField = typeof(Cask).GetField("agingRate", BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool patched;

    public static void TryRegister(IModHelper helper, IMonitor monitor, Harmony harmony)
    {
        if (patched || (!helper.ModRegistry.IsLoaded(UiInfoSuite2UniqueId) && !helper.ModRegistry.IsLoaded(UiInfoSuite2AltUniqueId)))
            return;

        try
        {
            List<string> patchedTargets = new();
            TryPatchMachineTimeRenderer(
                harmony,
                "UIInfoSuite2.UIElements.ShowCropAndBarrelTime+DetailRenderers",
                "UI Info Suite 2",
                patchedTargets);
            TryPatchMachineTimeRenderer(
                harmony,
                "UIInfoSuite2Alt.UIElements.ShowTileTooltips+DetailRenderers",
                "UI Info Suite 2 Alternative",
                patchedTargets);

            if (patchedTargets.Count == 0)
            {
                monitor.Log("[PowerGrid] UI Info Suite is installed, but its machine tooltip renderer was not found. PowerGrid tooltip details will use the built-in Power Tab only.", LogLevel.Warn);
                return;
            }

            patched = true;
            monitor.Log($"[PowerGrid] Machine tooltip support enabled for {string.Join(", ", patchedTargets)}.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            monitor.Log($"[PowerGrid] Failed to enable UI Info Suite tooltip support: {ex.Message}", LogLevel.Warn);
        }
    }

    private static void TryPatchMachineTimeRenderer(Harmony harmony, string rendererTypeName, string label, List<string> patchedTargets)
    {
        Type? rendererType = AccessTools.TypeByName(rendererTypeName);
        MethodInfo? machineTimeMethod = rendererType == null
            ? null
            : AccessTools.Method(rendererType, "MachineTime");

        if (machineTimeMethod == null)
            return;

        harmony.Patch(machineTimeMethod, postfix: new HarmonyMethod(typeof(UiInfoSuiteIntegration), nameof(MachineTimePostfix)));
        patchedTargets.Add(label);
    }

    private static void MachineTimePostfix(Object? tileObject, object? entries, ref bool __result)
    {
        if (tileObject == null || ModEntry.Instance == null)
            return;

        if (!TooltipEntries.TryCreate(entries, out TooltipEntries tooltipEntries))
            return;

        int startCount = tooltipEntries.Count;
        if (AppendGeneratorLines(tileObject, tooltipEntries) || AppendConsumerLines(tileObject, tooltipEntries) || AppendBatteryLines(tileObject, tooltipEntries))
            __result = tooltipEntries.Count > startCount || __result;
    }

    private static bool AppendGeneratorLines(Object tileObject, TooltipEntries entries)
    {
        string itemId = tileObject.ItemId ?? "";
        if (!IsPowerGridGenerator(itemId) && !IsModDataType(tileObject, "Generator"))
            return false;

        int euPerTick = ReadInt(tileObject, MdEuPerTick, GetConfiguredGeneratorOutput(itemId));
        int generatedThisTick = ReadInt(tileObject, MdGeneratedThisTick, 0);
        bool requiresFuel = ReadBool(tileObject, MdRequiresFuel, itemId != PowerConstants.WindGeneratorId);
        int fuelTicks = ReadInt(tileObject, MdFuelTicksRemaining, requiresFuel ? 0 : -1);
        bool online = ReadBool(tileObject, MdOnline, generatedThisTick > 0 || (!requiresFuel && euPerTick > 0));
        int networkId = ReadInt(tileObject, MdNetworkId, 0);

        AddSectionBreak(entries);
        entries.Add($"PowerGrid: {(online ? "online" : "offline")}");
        entries.Add($"Output: {(online ? euPerTick : 0)} EU/tick");

        if (requiresFuel)
            entries.Add(fuelTicks > 0 ? $"Fuel: {FormatTicks(fuelTicks)} remaining" : "Fuel: empty");
        else
            entries.Add("Fuel: passive");

        if (networkId > 0)
            entries.Add($"Network: #{networkId}");

        return true;
    }

    private static bool AppendConsumerLines(Object tileObject, TooltipEntries entries)
    {
        if (!IsPowerGridConsumer(tileObject.ItemId ?? "") && !IsModDataType(tileObject, "Consumer"))
            return false;

        int euAllocated = ReadInt(tileObject, MdEuAllocated, 0);
        int euDemanded = ReadInt(tileObject, MdEuDemanded, GetConfiguredConsumerDemand(tileObject.ItemId ?? ""));
        float speedup = ReadFloat(tileObject, MdSpeedup, 0f);
        int minutesRemaining = ReadInt(tileObject, MdMinutesRemaining, tileObject.MinutesUntilReady);
        bool powered = ReadBool(tileObject, MdPowered, euAllocated > 0 || speedup > 0f);
        bool energized = ReadBool(tileObject, MdEnergized, false);
        int networkId = ReadInt(tileObject, MdNetworkId, 0);
        bool isMetalCask = IsMetalCask(tileObject, tileObject.ItemId ?? "");
        bool isProcessing = IsConsumerProcessing(tileObject, isMetalCask);

        entries.Clear();
        if (tileObject.heldObject.Value != null)
            entries.Add(tileObject.heldObject.Value.DisplayName);

        AddSectionBreak(entries);
        entries.Add($"PowerGrid: {GetConsumerPowerState(isProcessing, powered, energized, networkId, euAllocated, euDemanded)}");
        entries.Add($"Power: {euAllocated}/{euDemanded} EU/tick");
        if (speedup > 0f)
            entries.Add($"Speed bonus: {speedup.ToString("P0", CultureInfo.InvariantCulture)}");

        if (isProcessing)
        {
            if (TryBuildMetalCaskEta(tileObject, speedup, out string eta))
                entries.Add(eta);
            else
                entries.Add($"ETA: {FormatDuration(GetProjectedMinutesRemaining(minutesRemaining, speedup, powered))}");
        }
        else if (tileObject.heldObject.Value == null)
        {
            entries.Add("Status: ready for input");
        }
        else
        {
            entries.Add("Status: ready to collect");
        }

        if (networkId > 0)
            entries.Add($"Network: #{networkId}");

        return true;
    }

    private static string GetConsumerPowerState(bool isProcessing, bool powered, bool energized, int networkId, int euAllocated, int euDemanded)
    {
        if (isProcessing)
        {
            if (powered)
                return euDemanded <= 0 || euAllocated >= euDemanded ? "powered" : "low power";

            return "processing unpowered";
        }

        if (networkId <= 0)
            return "not connected";

        return energized ? "standby" : "grid offline";
    }

    private static bool IsConsumerProcessing(Object tileObject, bool isMetalCask)
    {
        if (isMetalCask && tileObject is Cask cask)
        {
            Object? heldObject = cask.heldObject.Value;
            if (heldObject == null)
                return false;

            if (TryGetCaskDaysToMature(cask, out float daysRemaining) || TryReadFloat(tileObject, MetalCaskDaysToNextQualityKey, out daysRemaining))
                return daysRemaining > 0f || heldObject.Quality < 4;

            return heldObject.Quality < 4;
        }

        return tileObject.MinutesUntilReady > 0;
    }

    private static int GetProjectedMinutesRemaining(int minutesRemaining, float speedup, bool powered)
    {
        if (!powered || speedup <= 0f)
            return Math.Max(0, minutesRemaining);

        return Math.Max(0, (int)Math.Ceiling(minutesRemaining / (1f + speedup)));
    }

    private static bool TryBuildMetalCaskEta(Object tileObject, float speedup, out string eta)
    {
        eta = "";
        if (tileObject is not Cask cask || !IsMetalCask(tileObject, tileObject.ItemId ?? ""))
            return false;

        Object? heldObject = cask.heldObject.Value;
        if (heldObject == null)
            return false;

        if (!TryGetCaskDaysToMature(cask, out float daysRemaining) && !TryReadFloat(tileObject, MetalCaskDaysToNextQualityKey, out daysRemaining))
            return false;

        int currentQuality = heldObject.Quality;
        if (currentQuality >= 4 && daysRemaining <= 0f)
            return false;

        int nextQuality = cask.GetNextQuality(currentQuality);
        float nextQualityThreshold = cask.GetDaysForQuality(nextQuality);
        float maturityToNextQuality = Math.Max(0f, daysRemaining - nextQualityThreshold);
        float projectedDailyMaturity = GetProjectedDailyCaskMaturity(cask, speedup);
        if (projectedDailyMaturity <= 0f)
            return false;

        int overnights = Math.Max(1, (int)Math.Ceiling(maturityToNextQuality / projectedDailyMaturity));
        eta = $"ETA: {FormatDuration(overnights * 24 * 60)} to {FormatQualityName(nextQuality)}";
        return true;
    }

    private static bool AppendBatteryLines(Object tileObject, TooltipEntries entries)
    {
        string itemId = tileObject.ItemId ?? "";
        if (!IsPowerGridBattery(itemId) && !IsModDataType(tileObject, "Battery"))
            return false;

        int charge = ReadInt(tileObject, MdCharge, 0);
        int capacity = ReadInt(tileObject, MdCapacity, GetConfiguredBatteryCapacity(itemId));
        float chargePercent = ReadFloat(tileObject, MdChargePercent, capacity > 0 ? (float)charge / capacity : 0f);
        int drained = ReadInt(tileObject, MdDrainedThisTick, 0);
        int stored = ReadInt(tileObject, MdStoredThisTick, 0);
        int networkId = ReadInt(tileObject, MdNetworkId, 0);
        int net = stored - drained;

        string state = net > 0
            ? "charging"
            : net < 0
                ? "discharging"
                : capacity > 0 && charge >= capacity
                    ? "full"
                    : charge <= 0
                        ? "empty"
                        : "idle";

        AddSectionBreak(entries);
        entries.Add($"PowerGrid: battery {state}");
        entries.Add($"Charge: {charge}/{capacity} EU ({chargePercent.ToString("P0", CultureInfo.InvariantCulture)})");
        entries.Add(net == 0 ? "Last tick: idle" : $"Last tick: {(net > 0 ? "+" : "")}{net} EU");

        if (networkId > 0)
            entries.Add($"Network: #{networkId}");

        return true;
    }

    private static bool IsPowerGridGenerator(string itemId)
    {
        return itemId == PowerConstants.SteamGeneratorId
            || itemId == PowerConstants.CombustionGeneratorId
            || itemId == PowerConstants.RadioisotopeGeneratorId
            || itemId == PowerConstants.WindGeneratorId;
    }

    private static bool IsPowerGridConsumer(string itemId)
    {
        return itemId == PowerConstants.IndustrialPreservesJarId
            || itemId == PowerConstants.MetalCaskId
            || itemId == PowerConstants.MetalKegId
            || itemId == PowerConstants.HardIridiumKegId
            || itemId == PowerConstants.ElectricSmelterId
            || itemId == PowerConstants.IndustrialRecyclerId
            || itemId == PowerConstants.PoweredDehydratorId;
    }

    private static bool IsMetalCask(Object tileObject, string itemId)
    {
        return tileObject is Cask
            && (itemId == PowerConstants.MetalCaskId
                || tileObject.QualifiedItemId == PowerConstants.Q(PowerConstants.MetalCaskId)
                || tileObject.modData.ContainsKey(MetalCaskMarkerKey));
    }

    private static bool IsPowerGridBattery(string itemId)
    {
        return itemId == PowerConstants.BasicBatteryId
            || itemId == PowerConstants.IridiumBatteryId;
    }

    private static int GetConfiguredGeneratorOutput(string itemId)
    {
        ModConfig config = ModEntry.Instance.Config;
        return itemId switch
        {
            PowerConstants.SteamGeneratorId => config.SteamGeneratorEUPerTick,
            PowerConstants.CombustionGeneratorId => config.CombustionGeneratorEUPerTick,
            PowerConstants.RadioisotopeGeneratorId => config.RadioisotopeGeneratorEUPerTick,
            PowerConstants.WindGeneratorId => config.WindGeneratorEUPerTick,
            _ => 0
        };
    }

    private static int GetConfiguredConsumerDemand(string itemId)
    {
        ModConfig config = ModEntry.Instance.Config;
        return itemId switch
        {
            PowerConstants.IndustrialPreservesJarId => Math.Max(0, config.IndustrialPreservesJarEUPerMinute * PowerConstants.TickIntervalMinutes),
            PowerConstants.MetalCaskId => Math.Max(0, config.MetalCaskEUPerMinute * PowerConstants.TickIntervalMinutes),
            PowerConstants.MetalKegId => Math.Max(0, config.MetalKegEUPerMinute * PowerConstants.TickIntervalMinutes),
            PowerConstants.HardIridiumKegId => Math.Max(0, config.HardIridiumKegEUPerMinute * PowerConstants.TickIntervalMinutes),
            PowerConstants.ElectricSmelterId => Math.Max(0, config.ElectricSmelterEUPerTick),
            PowerConstants.IndustrialRecyclerId => Math.Max(0, config.IndustrialRecyclerEUPerTick),
            PowerConstants.PoweredDehydratorId => Math.Max(0, config.PoweredDehydratorEUPerTick),
            _ => 0
        };
    }

    private static int GetConfiguredBatteryCapacity(string itemId)
    {
        ModConfig config = ModEntry.Instance.Config;
        return itemId switch
        {
            PowerConstants.BasicBatteryId => config.BasicBatteryCapacity,
            PowerConstants.IridiumBatteryId => config.IridiumBatteryCapacity,
            _ => 0
        };
    }

    private static bool IsModDataType(Object tileObject, string type)
    {
        return tileObject.modData.TryGetValue(MdType, out string? value)
            && string.Equals(value, type, StringComparison.Ordinal);
    }

    private static float GetProjectedDailyCaskMaturity(Cask cask, float speedup)
    {
        float agingRate = TryGetCaskAgingRate(cask, out float parsedAgingRate)
            ? Math.Max(0f, parsedAgingRate)
            : 0f;
        if (agingRate <= 0f)
            agingRate = MetalCaskBaseMaturityPerDay;

        float poweredBonus = 0f;
        float maxSpeedup = Math.Max(0f, ModEntry.Instance.Config.MetalCaskMaxSpeedup);
        if (speedup > 0f && maxSpeedup > 0f)
        {
            float normalizedPower = Math.Clamp(speedup / maxSpeedup, 0f, 1f);
            poweredBonus = normalizedPower * MetalCaskPoweredBonusMaturityPerDayAtFullPower;
        }

        return agingRate + poweredBonus;
    }

    private static string FormatQualityName(int quality)
    {
        return quality switch
        {
            1 => "silver",
            2 => "gold",
            4 => "iridium",
            _ => "higher quality"
        };
    }

    private static void AddSectionBreak(TooltipEntries entries)
    {
        if (entries.Count > 0)
            entries.Add("");
    }

    private readonly struct TooltipEntries
    {
        private readonly IList entries;
        private readonly Type? elementType;
        private readonly ConstructorInfo? textConstructor;

        private TooltipEntries(IList entries, Type? elementType, ConstructorInfo? textConstructor)
        {
            this.entries = entries;
            this.elementType = elementType;
            this.textConstructor = textConstructor;
        }

        public int Count => entries.Count;

        public void Clear()
        {
            entries.Clear();
        }

        public static bool TryCreate(object? entries, out TooltipEntries tooltipEntries)
        {
            tooltipEntries = default;
            if (entries is not IList list)
                return false;

            Type entriesType = entries.GetType();
            Type? elementType = entriesType.IsGenericType ? entriesType.GetGenericArguments()[0] : typeof(string);
            ConstructorInfo? textConstructor = null;

            if (elementType != typeof(string))
            {
                textConstructor = elementType
                    .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(ctor =>
                    {
                        ParameterInfo[] parameters = ctor.GetParameters();
                        return parameters.Length > 0 && parameters[0].ParameterType == typeof(string);
                    });

                if (textConstructor == null)
                    return false;
            }

            tooltipEntries = new TooltipEntries(list, elementType, textConstructor);
            return true;
        }

        public void Add(string text)
        {
            if (elementType == typeof(string))
            {
                entries.Add(text);
                return;
            }

            if (textConstructor == null)
                return;

            ParameterInfo[] parameters = textConstructor.GetParameters();
            object?[] args = new object?[parameters.Length];
            args[0] = text;
            object? line = textConstructor.Invoke(args);
            if (line != null)
                entries.Add(line);
        }
    }

    private static int ReadInt(Object tileObject, string key, int fallback)
    {
        return tileObject.modData.TryGetValue(key, out string? value)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
    }

    private static float ReadFloat(Object tileObject, string key, float fallback)
    {
        return tileObject.modData.TryGetValue(key, out string? value)
            && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : fallback;
    }

    private static bool TryReadFloat(Object tileObject, string key, out float parsed)
    {
        parsed = 0f;
        return tileObject.modData.TryGetValue(key, out string? value)
            && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }

    private static bool ReadBool(Object tileObject, string key, bool fallback)
    {
        if (!tileObject.modData.TryGetValue(key, out string? value))
            return fallback;

        return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetCaskDaysToMature(Cask cask, out float daysRemaining)
    {
        daysRemaining = 0f;
        if (CaskDaysToMatureField?.GetValue(cask) is not NetFloat netFloat)
            return false;

        daysRemaining = netFloat.Value;
        return true;
    }

    private static bool TryGetCaskAgingRate(Cask cask, out float agingRate)
    {
        agingRate = 0f;
        if (CaskAgingRateField?.GetValue(cask) is not NetFloat netFloat)
            return false;

        agingRate = netFloat.Value;
        return true;
    }

    private static string FormatTicks(int ticks)
    {
        int minutes = Math.Max(0, ticks) * PowerConstants.TickIntervalMinutes;
        return FormatDuration(minutes);
    }

    private static string FormatDuration(int minutes)
    {
        minutes = Math.Max(0, minutes);
        if (minutes >= 24 * 60)
        {
            int hours = (int)Math.Ceiling(minutes / 60f);
            int days = hours / 24;
            int remainderHours = hours % 24;
            return $"{FormatUnit(days, "day")} and {FormatUnit(remainderHours, "hour")}";
        }

        int displayMinutes = Math.Max(1, minutes);
        int hoursBelowDay = displayMinutes / 60;
        int remainder = displayMinutes % 60;
        return $"{FormatUnit(hoursBelowDay, "hour")} and {FormatUnit(remainder, "minute")}";
    }

    private static string FormatUnit(int value, string unit)
    {
        return value == 1 ? $"{value} {unit}" : $"{value} {unit}s";
    }

}
