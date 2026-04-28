using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using StardewValley.Mods;
using StardewValley.Objects;
using Meiameiameia.PowerGrid.Core;
using Meiameiameia.PowerGrid.UI;
using Meiameiameia.PowerGrid.Integrations;

namespace Meiameiameia.PowerGrid;

internal sealed class ModEntry : Mod
{
    internal static ModEntry Instance { get; private set; } = null!;

    internal ModConfig Config { get; private set; } = new();
    internal PowerManager PowerMgr { get; private set; } = null!;
    internal BatteryStateManager BatteryState { get; private set; } = null!;
    internal FuelManager FuelMgr { get; private set; } = null!;
    internal ConduitManager ConduitMgr { get; private set; } = null!;
    internal PowerQueryService PowerQuery { get; private set; } = null!;

    private int lastTimeOfDay;
    private bool debugOverlayActive;
    private Harmony? harmony;

    private const string DefaultBigCraftableTexture = "TileSheets/Craftables";
    private const string PersistedModDataPrefix = "meiameiameia.PowerGrid/";
    private const string PersistedChargeKey = PersistedModDataPrefix + "charge";
    private const string PersistedFuelTicksRemainingKey = PersistedModDataPrefix + "fuelTicksRemaining";
    private const string PersistedLinkedKey = PersistedModDataPrefix + "linked";
    private const string PersistedPartnerLocationKey = PersistedModDataPrefix + "partnerLocation";
    private const string PersistedPartnerTileKey = PersistedModDataPrefix + "partnerTile";
    private const string RuntimeOnlineKey = PersistedModDataPrefix + "online";
    private const string RuntimeGeneratedThisTickKey = PersistedModDataPrefix + "generatedThisTick";
    private const string BiofuelRecipeKey = "Biofuel";
    private const string CombustionGeneratorRecipeKey = "Combustion Generator";
    private const string IndustrialPreservesJarRecipeKey = "Industrial Preserves Jar";
    private const string MetalCaskRecipeKey = "Metal Cask";
    private const string MetalKegRecipeKey = "Metal Keg";
    private const string HardIridiumKegRecipeKey = "Hard Iridium Keg";
    private const string IndustrialPreservesJarSpriteName = "IndustrialPreservesJar";
    private const string MetalCaskSpriteName = "MetalCask";
    private const string MetalKegSpriteName = "MetalKeg";
    private const string HardIridiumKegSpriteName = "HardIridiumKeg";
    private const string IndustrialPreservesJarPoweredState = "powered";
    private const string IndustrialPreservesJarUnpoweredState = "unpowered";
    private const string GofHardwoodKegId = "GOF_Hardwood_Keg";
    private const float MetalCaskBaseAgingMultiplier = 0.50f;
    private const float MetalCaskPoweredBonusDaysAtFullPower = 1.50f;
    private const string MetalCaskPlacementError = "Metal Cask can only be placed in player-owned indoor spaces.";
    private const string MetalCaskMarkerKey = PersistedModDataPrefix + "MetalCask";
    private const string MetalCaskPowerModeKey = PersistedModDataPrefix + "MetalCaskPowerMode";
    private const string MetalCaskObservedPowerStateKey = PersistedModDataPrefix + "MetalCaskObservedPowerState";
    private const string MetalCaskObservedSpeedupKey = PersistedModDataPrefix + "MetalCaskObservedSpeedupFraction";
    private const string MetalCaskDaysToNextQualityKey = PersistedModDataPrefix + "MetalCaskDaysToNextQuality";
    private const string MetalCaskLastAppliedBonusDaysKey = PersistedModDataPrefix + "MetalCaskLastAppliedBonusDays";
    private const string MetalCaskLastAppliedDateKey = PersistedModDataPrefix + "MetalCaskLastAppliedDate";
    private const string SpriteDirectoryName = "Assets";
    private const float ChargedBatteryThreshold = 0.5f;
    private const string WindGeneratorWheelSpriteName = "WindGenerator__wheel";
    private const float WindGeneratorGeneratingRadiansPerSecond = 5.5f;
    private const float WindGeneratorIdleRadiansPerSecond = 0f;
    private const float WindGeneratorWheelLayerOffset = 0.0001f;
    private const float SteamActiveOverlayLayerOffset = 0.0002f;
    private const float DefaultBigCraftableLayerDepthYOffset = 24f;
    private const float FloorMountedLayerDepthInsetFromTileTop = 8f;
    private static readonly Rectangle BigCraftableSourceRect = new(0, 0, 16, 32);
    private static readonly Vector2 WindGeneratorWheelPivot = new(8f, 6f);
    private static readonly FieldInfo? CaskDaysToMatureField = AccessTools.Field(typeof(Cask), "daysToMature");
    private static readonly MethodInfo? CaskCheckForMaturityMethod = AccessTools.Method(typeof(Cask), "checkForMaturity");
    private static readonly MethodInfo? ResetParentSheetIndexMethod = AccessTools.Method(typeof(StardewValley.Object), "ResetParentSheetIndex");
    private static readonly MethodInfo? PerformObjectDropInActionMethod = AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performObjectDropInAction));
    private readonly HashSet<string> invalidStateSpritesLogged = new(StringComparer.Ordinal);
    private readonly HashSet<string> conduitRenderDiagnosticsLogged = new(StringComparer.Ordinal);
    private static readonly string[] RequiredSpriteNames =
    {
        "CopperCable",
        "IronCable",
        "IridiumCable",
        "Biofuel",
        "SteamGenerator",
        "SteamGenerator__off",
        "SteamGenerator__on",
        "CombustionGenerator",
        "CombustionGenerator__off",
        "CombustionGenerator__on",
        "WindGenerator",
        "WindGenerator__idle",
        "WindGenerator__generating",
        "WindGenerator__wheel",
        "BasicBattery",
        "BasicBattery__low",
        "BasicBattery__charged",
        "IridiumBattery",
        "IridiumBattery__low",
        "IridiumBattery__charged",
        "PowerConduit",
        "PowerConduit__unpaired",
        "PowerConduit__linked",
        "IndustrialPreservesJar",
        "IndustrialPreservesJar__powered",
        "IndustrialPreservesJar__unpowered",
        "MetalCask",
        "MetalCask__powered",
        "MetalCask__unpowered",
        "MetalKeg",
        "MetalKeg__powered",
        "MetalKeg__unpowered",
        "HardIridiumKeg",
        "HardIridiumKeg__powered",
        "HardIridiumKeg__unpowered"
    };
    private string? vanillaPreservesJarBigCraftableId;
    private string? vanillaCaskBigCraftableId;
    private string? vanillaKegBigCraftableId;
    private string? hardwoodKegBigCraftableId;
    private bool loggedMissingPreservesTemplate;
    private bool loggedMissingPreservesMachineTemplate;
    private bool loggedMissingCaskTemplate;
    private bool loggedMissingCaskMachineTemplate;
    private bool loggedMissingKegTemplate;
    private bool loggedHardIridiumFallbackToVanillaKeg;
    private bool loggedHardIridiumBoundToHardwoodKeg;
    private bool loggedMissingKegMachineTemplate;
    private bool attemptedWindWheelTextureLoad;
    private Texture2D? cachedWindWheelTexture;

    // Texture asset keys (lazy-computed from manifest)
    private string CopperCableTexture => GetTextureAsset("CopperCable");
    private string IronCableTexture => GetTextureAsset("IronCable");
    private string IridiumCableTexture => GetTextureAsset("IridiumCable");
    private string BiofuelTexture => GetTextureAsset("Biofuel");
    private string SteamGeneratorTexture => GetTextureAsset("SteamGenerator");
    private string CombustionGeneratorTexture => GetTextureAsset("CombustionGenerator");
    private string WindGeneratorTexture => GetTextureAsset("WindGenerator");
    private string WindGeneratorWheelTexture => GetTextureAsset(WindGeneratorWheelSpriteName);
    private string BasicBatteryTexture => GetTextureAsset("BasicBattery");
    private string IridiumBatteryTexture => GetTextureAsset("IridiumBattery");
    private string PowerConduitTexture => GetTextureAsset("PowerConduit");
    private string IndustrialPreservesJarTexture => GetTextureAsset(IndustrialPreservesJarSpriteName);
    private string MetalCaskTexture => GetTextureAsset(MetalCaskSpriteName);
    private string MetalKegTexture => GetTextureAsset(MetalKegSpriteName);
    private string HardIridiumKegTexture => GetTextureAsset(HardIridiumKegSpriteName);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions GameDataCloneJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        IncludeFields = true
    };

    private readonly record struct LegacyConduitEndpoint(string LocationName, Vector2 Tile, StardewValley.Object ConduitObject);

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
        bool migratedConfig = TryMigrateLegacyBalanceConfig(Config);
        migratedConfig |= TryMigrateHardIridiumKegSpeedConfig(Config);
        if (migratedConfig)
        {
            helper.WriteConfig(Config);
            Monitor.Log("[PowerGrid] Migrated legacy default balance values in config.json to the current defaults.", LogLevel.Info);
        }

        BatteryState = new BatteryStateManager(Monitor, Config);
        FuelMgr = new FuelManager(Monitor, Config);
        ConduitMgr = new ConduitManager(Monitor);
        PowerMgr = new PowerManager(Monitor, Config, BatteryState, FuelMgr);
        PowerQuery = new PowerQueryService(PowerMgr, BatteryState, FuelMgr);
        PowerMgr.SetConduitManager(ConduitMgr);
        ApplyPassabilityPatch();

        // SMAPI events
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.World.ObjectListChanged += OnObjectListChanged;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;

        // Console commands
        helper.ConsoleCommands.Add("powergrid_status", "Print power network status for current location.", CmdStatus);
        helper.ConsoleCommands.Add("powergrid_debug", "Toggle debug overlay.", CmdDebug);
        helper.ConsoleCommands.Add("powergrid_conduit_reset", "Cancel pending conduit pairing.", CmdConduitReset);
        helper.ConsoleCommands.Add("powergrid_extract_sprites", "Extract placeholder sprites to Assets folder for editing.", CmdExtractSprites);
        helper.ConsoleCommands.Add("powergrid_unlock", "Grant PowerGrid crafting recipes.\nUsage: powergrid_unlock [force]\n- force: bypass unlock conditions.", CmdUnlock);
        helper.ConsoleCommands.Add("powergrid_tab", "Open the global Power Tab menu.", CmdPowerTab);
        helper.ConsoleCommands.Add("powergrid_query_dump", "Dump read-only PowerGrid query snapshots.\nUsage: powergrid_query_dump [locationName]", CmdQueryDump);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GmcmIntegration.Register(Helper, ModManifest, Config, () =>
        {
            Config = Helper.ReadConfig<ModConfig>() ?? new ModConfig();
            RegisterPowerGridOwnedConsumers();
        });

        RegisterPowerGridOwnedConsumers();
        ValidateCriticalSpritePaths();
        LogConduitStateSpriteAvailability();
        Monitor.Log("[PowerGrid] Loaded. Waiting for save.", LogLevel.Info);
    }

    private void RegisterPowerGridOwnedConsumers()
    {
        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.IndustrialPreservesJarId),
            DemandPerTick = GetDemandPerTick(Config.IndustrialPreservesJarEUPerMinute),
            MaxSpeedupFraction = ClampSpeedup(Config.IndustrialPreservesJarMaxSpeedup),
            Priority = Math.Max(0, Config.IndustrialPreservesJarPriority),
            DisplayName = IndustrialPreservesJarRecipeKey
        });

        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.MetalCaskId),
            DemandPerTick = GetDemandPerTick(Config.MetalCaskEUPerMinute),
            MaxSpeedupFraction = ClampSpeedup(Config.MetalCaskMaxSpeedup),
            Priority = Math.Max(0, Config.MetalCaskPriority),
            DisplayName = MetalCaskRecipeKey
        });

        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.MetalKegId),
            DemandPerTick = GetDemandPerTick(Config.MetalKegEUPerMinute),
            MaxSpeedupFraction = ClampSpeedup(Config.MetalKegMaxSpeedup),
            Priority = Math.Max(0, Config.MetalKegPriority),
            DisplayName = MetalKegRecipeKey
        });

        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.HardIridiumKegId),
            DemandPerTick = GetDemandPerTick(Config.HardIridiumKegEUPerMinute),
            MaxSpeedupFraction = ClampSpeedup(Config.HardIridiumKegMaxSpeedup),
            Priority = Math.Max(0, Config.HardIridiumKegPriority),
            DisplayName = HardIridiumKegRecipeKey
        });
    }

    private static int GetDemandPerTick(int euPerMinute)
    {
        return Math.Max(0, euPerMinute) * PowerConstants.TickIntervalMinutes;
    }

    private static float ClampSpeedup(float speedup)
    {
        return Math.Clamp(speedup, 0f, 1f);
    }

    private static bool TryMigrateLegacyBalanceConfig(ModConfig config)
    {
        bool matchesLegacyDefaults =
            config.SteamGeneratorEUPerTick == 40
            && config.WindGeneratorEUPerTick == 20
            && config.CoalFuelTicks == 6
            && config.WoodFuelTicks == 2
            && config.HardwoodFuelTicks == 4
            && config.IndustrialPreservesJarEUPerMinute == 3
            && config.MetalCaskEUPerMinute == 6
            && config.MetalKegEUPerMinute == 2
            && config.HardIridiumKegEUPerMinute == 4;

        if (!matchesLegacyDefaults)
            return false;

        config.SteamGeneratorEUPerTick = 50;
        config.WindGeneratorEUPerTick = 25;
        config.CoalFuelTicks = 12;
        config.WoodFuelTicks = 4;
        config.HardwoodFuelTicks = 8;
        config.IndustrialPreservesJarEUPerMinute = 2;
        config.MetalCaskEUPerMinute = 4;
        config.MetalKegEUPerMinute = 1;
        config.HardIridiumKegEUPerMinute = 3;
        return true;
    }

    private static bool TryMigrateHardIridiumKegSpeedConfig(ModConfig config)
    {
        if (config.HardIridiumKegEUPerMinute != 3
            || Math.Abs(config.HardIridiumKegMaxSpeedup - 0.20f) > 0.0001f)
        {
            return false;
        }

        config.HardIridiumKegMaxSpeedup = 0.30f;
        return true;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        var batteryData = Helper.Data.ReadSaveData<Dictionary<string, int>>(PowerConstants.SaveDataKey);
        var conduitData = Helper.Data.ReadSaveData<List<ConduitLink>>(PowerConstants.ConduitSaveDataKey);
        var fuelData = Helper.Data.ReadSaveData<Dictionary<string, int>>(PowerConstants.FuelSaveDataKey);

        bool importedLegacyState = TryImportLegacyStateFromWorld(
            importBatteries: batteryData == null,
            importConduits: conduitData == null,
            importFuel: fuelData == null,
            out Dictionary<string, int>? legacyBatteryData,
            out List<ConduitLink>? legacyConduitData,
            out Dictionary<string, int>? legacyFuelData);

        if (batteryData == null && legacyBatteryData != null)
            batteryData = legacyBatteryData;
        if (conduitData == null && legacyConduitData != null)
            conduitData = legacyConduitData;
        if (fuelData == null && legacyFuelData != null)
            fuelData = legacyFuelData;

        BatteryState.ImportState(batteryData);
        ConduitMgr.ImportState(conduitData);
        FuelMgr.ImportState(fuelData);
        PruneStaleGeneratorFuelState();
        PruneStaleConduitLinks();
        RehydrateMetalCasks();
        RefreshMetalCaskTelemetry();

        PowerMgr.ResetRuntimeState();
        lastTimeOfDay = Game1.timeOfDay;

        if (importedLegacyState)
        {
            Monitor.Log(
                $"[PowerGrid] Imported compatibility state from persisted object modData for UniqueID migration. Batteries: {batteryData?.Count ?? 0}, conduit links: {conduitData?.Count ?? 0}, fueled generators: {fuelData?.Count ?? 0}.",
                LogLevel.Warn);
        }

        Monitor.Log($"[PowerGrid] Save loaded. Batteries: {BatteryState.TotalStoredEU()} EU stored. Conduit links: {ConduitMgr.GetAllLinks().Count}.", LogLevel.Info);

        TryGrantRecipes(reason: "SaveLoaded");
    }

    private bool TryImportLegacyStateFromWorld(
        bool importBatteries,
        bool importConduits,
        bool importFuel,
        out Dictionary<string, int>? batteryState,
        out List<ConduitLink>? conduitLinks,
        out Dictionary<string, int>? fuelState)
    {
        batteryState = null;
        conduitLinks = null;
        fuelState = null;

        if (!Context.IsWorldReady || (!importBatteries && !importConduits && !importFuel))
            return false;

        Dictionary<string, int>? importedBatteries = importBatteries ? new(StringComparer.Ordinal) : null;
        Dictionary<string, int>? importedFuel = importFuel ? new(StringComparer.Ordinal) : null;
        Dictionary<string, LegacyConduitEndpoint>? conduitsByEndpoint = importConduits ? new(StringComparer.Ordinal) : null;

        foreach (GameLocation location in EnumerateLoadedLocations())
        {
            string locationName = location.NameOrUniqueName;
            foreach (var pair in location.objects.Pairs)
            {
                Vector2 tile = pair.Key;
                StardewValley.Object obj = pair.Value;
                string itemId = obj.ItemId ?? "";

                if (importBatteries
                    && importedBatteries != null
                    && IsBatteryItem(itemId)
                    && TryReadPowerModDataInt(obj.modData, "charge", out int charge)
                    && charge > 0)
                {
                    importedBatteries[PowerConstants.MakeNodeKey(locationName, tile, itemId)] = charge;
                }

                if (importFuel
                    && importedFuel != null
                    && IsGeneratorItem(itemId)
                    && TryReadPowerModDataInt(obj.modData, "fuelTicksRemaining", out int fuelTicksRemaining)
                    && fuelTicksRemaining > 0)
                {
                    importedFuel[PowerConstants.MakeNodeKey(locationName, tile, itemId)] = fuelTicksRemaining;
                }

                if (importConduits
                    && conduitsByEndpoint != null
                    && itemId == PowerConstants.PowerConduitId)
                {
                    conduitsByEndpoint[MakeConduitEndpointKey(locationName, tile)] = new LegacyConduitEndpoint(locationName, tile, obj);
                }
            }
        }

        if (importConduits && conduitsByEndpoint != null && conduitsByEndpoint.Count > 0)
        {
            var importedLinks = new List<ConduitLink>();
            var seenLinks = new HashSet<string>(StringComparer.Ordinal);

            foreach (LegacyConduitEndpoint endpoint in conduitsByEndpoint.Values)
            {
                if (!TryReadPowerModDataString(endpoint.ConduitObject.modData, "linked", out string? linkedRaw)
                    || linkedRaw != "1")
                {
                    continue;
                }

                if (!TryReadPowerModDataString(endpoint.ConduitObject.modData, "partnerLocation", out string? partnerLocation)
                    || string.IsNullOrWhiteSpace(partnerLocation)
                    || !TryReadPowerModDataString(endpoint.ConduitObject.modData, "partnerTile", out string? partnerTileRaw)
                    || string.IsNullOrWhiteSpace(partnerTileRaw)
                    || !TryParseTile(partnerTileRaw, out Vector2 partnerTile))
                {
                    continue;
                }

                string partnerKey = MakeConduitEndpointKey(partnerLocation, partnerTile);
                if (!conduitsByEndpoint.ContainsKey(partnerKey))
                    continue;

                string linkKey = MakeCanonicalConduitLinkKey(endpoint.LocationName, endpoint.Tile, partnerLocation, partnerTile);
                if (!seenLinks.Add(linkKey))
                    continue;

                importedLinks.Add(new ConduitLink
                {
                    LocationA = endpoint.LocationName,
                    TileA = endpoint.Tile,
                    LocationB = partnerLocation,
                    TileB = partnerTile
                });
            }

            if (importedLinks.Count > 0)
                conduitLinks = importedLinks;
        }

        if (importBatteries && importedBatteries != null && importedBatteries.Count > 0)
            batteryState = importedBatteries;
        if (importFuel && importedFuel != null && importedFuel.Count > 0)
            fuelState = importedFuel;

        return batteryState != null || conduitLinks != null || fuelState != null;
    }

    private static IEnumerable<GameLocation> EnumerateLoadedLocations()
    {
        foreach (GameLocation location in Game1.locations)
        {
            yield return location;

            if (location.buildings == null)
                continue;

            foreach (var building in location.buildings)
            {
                GameLocation? interior = building.GetIndoors();
                if (interior != null)
                    yield return interior;
            }
        }
    }

    private static bool IsBatteryItem(string itemId)
    {
        return itemId == PowerConstants.BasicBatteryId || itemId == PowerConstants.IridiumBatteryId;
    }

    private static bool IsGeneratorItem(string itemId)
    {
        return itemId == PowerConstants.SteamGeneratorId
            || itemId == PowerConstants.CombustionGeneratorId
            || itemId == PowerConstants.WindGeneratorId;
    }

    private static bool TryReadNonNegativeInt(ModDataDictionary modData, string key, out int value)
    {
        value = 0;
        return modData.TryGetValue(key, out string? raw)
            && int.TryParse(raw, out value)
            && value >= 0;
    }

    private static bool TryReadPowerModDataInt(ModDataDictionary modData, string suffix, out int value)
    {
        return TryReadNonNegativeInt(modData, PersistedModDataPrefix + suffix, out value);
    }

    private static bool TryReadPowerModDataString(ModDataDictionary modData, string suffix, out string? value)
    {
        return modData.TryGetValue(PersistedModDataPrefix + suffix, out value);
    }

    private static bool TryParseTile(string raw, out Vector2 tile)
    {
        tile = Vector2.Zero;
        string[] parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out int tileX)
            || !int.TryParse(parts[1], out int tileY))
        {
            return false;
        }

        tile = new Vector2(tileX, tileY);
        return true;
    }

    private static string MakeConduitEndpointKey(string locationName, Vector2 tile)
    {
        return FormattableString.Invariant($"{locationName}|{tile.X}|{tile.Y}");
    }

    private static string MakeCanonicalConduitLinkKey(string locationA, Vector2 tileA, string locationB, Vector2 tileB)
    {
        string left = MakeConduitEndpointKey(locationA, tileA);
        string right = MakeConduitEndpointKey(locationB, tileB);
        return string.CompareOrdinal(left, right) <= 0 ? $"{left}<->{right}" : $"{right}<->{left}";
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        SavePowerState();
    }

    private void SavePowerState()
    {
        Helper.Data.WriteSaveData(PowerConstants.SaveDataKey, BatteryState.ExportState());
        Helper.Data.WriteSaveData(PowerConstants.ConduitSaveDataKey, ConduitMgr.ExportState());
        Helper.Data.WriteSaveData(PowerConstants.FuelSaveDataKey, FuelMgr.ExportState());
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || lastTimeOfDay <= 0)
            return;

        int elapsed = TimeDiffMinutes(lastTimeOfDay, 2600);
        if (elapsed < PowerConstants.TickIntervalMinutes)
            return;

        int ticks = elapsed / PowerConstants.TickIntervalMinutes;
        for (int i = 0; i < ticks; i++)
            PowerMgr.SimulateTick();

        lastTimeOfDay = 2600;
        SavePowerState();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        BatteryState.ApplyDailyLeak();
        RehydrateMetalCasks();
        RefreshMetalCaskTelemetry();
        PowerMgr.ResetRuntimeState();
        if (Context.IsMainPlayer)
        {
            PowerMgr.SimulateTick();
            RefreshMetalCaskTelemetry();
            TriggerFueledGeneratorWorkingAnimations();
        }
        lastTimeOfDay = Game1.timeOfDay;

        TryGrantRecipes(reason: "DayStarted");
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PowerMgr.ResetRuntimeState();
        lastTimeOfDay = 0;
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        int elapsed = TimeDiffMinutes(lastTimeOfDay, e.NewTime);
        lastTimeOfDay = e.NewTime;

        // Simulate every 10 in-game minutes
        if (elapsed >= PowerConstants.TickIntervalMinutes)
        {
            int ticks = elapsed / PowerConstants.TickIntervalMinutes;
            for (int i = 0; i < ticks; i++)
            {
                PowerMgr.SimulateTick();
                TriggerFueledGeneratorWorkingAnimations();
            }
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree || !Config.EnablePowerTab)
            return;

        if (!Config.PowerTabKeybind.JustPressed())
            return;

        OpenPowerTab();

        var pressed = Config.PowerTabKeybind.GetKeybindCurrentlyDown();
        if (pressed != null)
        {
            foreach (SButton button in pressed.Buttons)
                Helper.Input.Suppress(button);
        }
    }

    private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        bool removedConduitState = false;
        foreach ((Vector2 tile, StardewValley.Object removedObj) in e.Removed)
        {
            HandleRemovedGeneratorFuelState(e.Location, tile, removedObj);
            removedConduitState |= HandleRemovedConduitState(e.Location, tile, removedObj);
        }

        RehydrateMetalCasks(e.Location);
        RefreshMetalCaskTelemetry(e.Location);

        // Mark location dirty when objects are placed/removed.
        PowerMgr.MarkDirty(e.Location.NameOrUniqueName);
        if (removedConduitState)
            PowerMgr.MarkAllDirty();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // Debug overlay toggle
        if (Config.DebugOverlayEnabled
            && e.Button.ToString().Equals(Config.DebugOverlayKeybind, StringComparison.OrdinalIgnoreCase))
        {
            debugOverlayActive = !debugOverlayActive;
            Monitor.Log($"[PowerGrid] Debug overlay: {(debugOverlayActive ? "ON" : "OFF")}", LogLevel.Info);
            return;
        }

        // Right-click interaction with PowerGrid objects
        if (e.Button.IsActionButton())
        {
            Vector2 tile = e.Cursor.GrabTile;
            GameLocation loc = Game1.currentLocation;
            StardewValley.Object? obj = loc.getObjectAtTile((int)tile.X, (int)tile.Y);
            if (obj == null)
                return;

            string itemId = obj.ItemId ?? "";
            bool wantsLegacyMonitor = Helper.Input.IsDown(SButton.LeftShift) || Helper.Input.IsDown(SButton.RightShift);

            // Power Conduit interaction: pairing / unlink
            if (itemId == PowerConstants.PowerConduitId)
            {
                if (wantsLegacyMonitor)
                {
                    if (ConduitMgr.RemoveConduitState(loc.NameOrUniqueName, tile))
                    {
                        PowerMgr.MarkAllDirty();
                        Game1.addHUDMessage(new HUDMessage("Conduit link removed.", HUDMessage.error_type));
                    }
                    else if (ConduitMgr.HasPending)
                    {
                        ConduitMgr.CancelPairing();
                        Game1.addHUDMessage(new HUDMessage("Conduit pairing cancelled.", HUDMessage.error_type));
                    }
                    else
                    {
                        Game1.addHUDMessage(new HUDMessage("Conduit has no active link.", HUDMessage.newQuest_type));
                    }

                    Helper.Input.Suppress(e.Button);
                    return;
                }

                if (!ConduitMgr.HasPending)
                {
                    ConduitMgr.StartPairing(loc.NameOrUniqueName, tile);
                    Game1.addHUDMessage(new HUDMessage("Conduit pairing started. Interact with another conduit to link.", HUDMessage.newQuest_type));
                }
                else
                {
                    if (ConduitMgr.TryCompletePairing(loc.NameOrUniqueName, tile))
                    {
                        Game1.addHUDMessage(new HUDMessage("Conduits linked!", HUDMessage.achievement_type));
                        PowerMgr.MarkAllDirty();
                    }
                    else
                    {
                        ConduitMgr.CancelPairing();
                        Game1.addHUDMessage(new HUDMessage("Conduit pairing cancelled.", HUDMessage.error_type));
                    }
                }

                Helper.Input.Suppress(e.Button);
                return;
            }

            // Fuel generators: fuel insertion should happen on right-click with supported fuel in hand.
            if (IsFuelGeneratorItem(itemId) && TryInsertFuelIntoGenerator(loc, obj))
            {
                Helper.Input.Suppress(e.Button);
                PowerMgr.MarkDirty(loc.NameOrUniqueName);
                return;
            }

            // Legacy compatibility: convert old held fuel stacks into internal fuel ticks.
            if (itemId == PowerConstants.SteamGeneratorId && TryMigrateLegacySteamFuel(loc, obj))
            {
                Helper.Input.Suppress(e.Button);
                PowerMgr.MarkDirty(loc.NameOrUniqueName);
                return;
            }

            // Prevent vanilla machine behavior on steam generators (e.g. spitting back fuel).
            if (IsFuelGeneratorItem(itemId) && !wantsLegacyMonitor)
            {
                Helper.Input.Suppress(e.Button);
                return;
            }

            // Legacy location-scoped monitor is available with Shift+right-click.
            if (wantsLegacyMonitor && (itemId == PowerConstants.BasicBatteryId || itemId == PowerConstants.IridiumBatteryId ||
                IsGeneratorItem(itemId) ||
                IsConsumerObject(obj)))
            {
                var menu = new PowerMonitorMenu(loc, PowerQuery);
                Game1.activeClickableMenu = menu;
                Helper.Input.Suppress(e.Button);
                return;
            }
        }
    }

    private static bool IsFuelGeneratorItem(string itemId)
    {
        return itemId == PowerConstants.SteamGeneratorId
            || itemId == PowerConstants.CombustionGeneratorId;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (debugOverlayActive)
            DebugOverlay.Draw(e.SpriteBatch, Game1.currentLocation, PowerMgr, BatteryState);
    }

    private void DrawCables(SpriteBatch b, GameLocation location)
    {
        var networks = PowerMgr.GetNetworks(location);
        if (networks.Count == 0)
            return;

        foreach (var net in networks)
        {
            foreach (var cable in net.Cables)
            {
                if (cable.LocationName != location.NameOrUniqueName)
                    continue;

                string texAsset = cable.CableTier switch
                {
                    CableTier.Copper => CopperCableTexture,
                    CableTier.Iron => IronCableTexture,
                    CableTier.Iridium => IridiumCableTexture,
                    _ => CopperCableTexture
                };

                Texture2D? tex = null;
                try
                {
                    tex = Helper.GameContent.Load<Texture2D>(texAsset);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"[DrawCables] Error loading texture {texAsset}: {ex.Message}", LogLevel.Error);
                    continue;
                }

                if (tex == null)
                {
                    Monitor.Log($"[DrawCables] Texture {texAsset} is null!", LogLevel.Error);
                    continue;
                }

                int mask = cable.ConnectionMask;
                int col = mask % 4;
                int row = mask / 4;

                const int tileW = 16;
                const int tileH = 32;

                var sourceRect = new Rectangle(col * tileW, row * tileH, tileW, tileH);
                Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, new Vector2(cable.Tile.X * 64, cable.Tile.Y * 64));
                screenPos.Y -= 64; 

                float layerDepth = 1f;

                b.Draw(
                    texture: tex,
                    position: screenPos,
                    sourceRectangle: sourceRect,
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 4f,
                    effects: SpriteEffects.None,
                    layerDepth: layerDepth
                );
            }
        }
    }

    // ─────────────────────────────────────────────
    // Asset Registration
    // ─────────────────────────────────────────────

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(EditBigCraftables, AssetEditPriority.Late);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(EditObjects, AssetEditPriority.Late);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(EditCraftingRecipes, AssetEditPriority.Late);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
        {
            e.Edit(EditMachines, AssetEditPriority.Late);
        }

        // Texture loading for each PowerGrid object
        TryLoadTexture(e, CopperCableTexture, "CopperCable", new Color(200, 120, 60));
        TryLoadTexture(e, IronCableTexture, "IronCable", new Color(180, 180, 180));
        TryLoadTexture(e, IridiumCableTexture, "IridiumCable", new Color(120, 70, 200));
        TryLoadObjectTexture(e, BiofuelTexture, "Biofuel", new Color(84, 196, 118));
        TryLoadTexture(e, SteamGeneratorTexture, "SteamGenerator", new Color(140, 140, 160));
        TryLoadTexture(e, CombustionGeneratorTexture, "CombustionGenerator", new Color(120, 128, 144));
        TryLoadTexture(e, WindGeneratorTexture, "WindGenerator", new Color(100, 180, 220));
        TryLoadOptionalTexture(e, WindGeneratorWheelTexture, WindGeneratorWheelSpriteName);
        TryLoadTexture(e, BasicBatteryTexture, "BasicBattery", new Color(60, 180, 60));
        TryLoadTexture(e, IridiumBatteryTexture, "IridiumBattery", new Color(150, 80, 220));
        TryLoadTexture(e, PowerConduitTexture, "PowerConduit", new Color(220, 200, 60));
        TryLoadTexture(e, IndustrialPreservesJarTexture, IndustrialPreservesJarSpriteName, new Color(180, 120, 80));
        TryLoadTexture(e, MetalCaskTexture, MetalCaskSpriteName, new Color(130, 130, 140));
        TryLoadTexture(e, MetalKegTexture, MetalKegSpriteName, new Color(170, 170, 180));
        TryLoadTexture(e, HardIridiumKegTexture, HardIridiumKegSpriteName, new Color(150, 110, 210));

        TryLoadStateTexture(e, "SteamGenerator", "off", new Color(140, 140, 160));
        TryLoadStateTexture(e, "SteamGenerator", "on", new Color(140, 140, 160));
        TryLoadStateTexture(e, "CombustionGenerator", "off", new Color(120, 128, 144));
        TryLoadStateTexture(e, "CombustionGenerator", "on", new Color(120, 128, 144));
        TryLoadStateTexture(e, "WindGenerator", "idle", new Color(100, 180, 220));
        TryLoadStateTexture(e, "WindGenerator", "generating", new Color(100, 180, 220));
        TryLoadStateTexture(e, "BasicBattery", "low", new Color(60, 180, 60));
        TryLoadStateTexture(e, "BasicBattery", "charged", new Color(60, 180, 60));
        TryLoadStateTexture(e, "IridiumBattery", "low", new Color(150, 80, 220));
        TryLoadStateTexture(e, "IridiumBattery", "charged", new Color(150, 80, 220));
        TryLoadStateTexture(e, "PowerConduit", "unpaired", new Color(220, 200, 60));
        TryLoadStateTexture(e, "PowerConduit", "linked", new Color(220, 200, 60));
        TryLoadStateTexture(e, IndustrialPreservesJarSpriteName, IndustrialPreservesJarPoweredState, new Color(180, 120, 80));
        TryLoadStateTexture(e, IndustrialPreservesJarSpriteName, IndustrialPreservesJarUnpoweredState, new Color(180, 120, 80));
        TryLoadStateTexture(e, MetalCaskSpriteName, IndustrialPreservesJarPoweredState, new Color(130, 130, 140));
        TryLoadStateTexture(e, MetalCaskSpriteName, IndustrialPreservesJarUnpoweredState, new Color(130, 130, 140));
        TryLoadStateTexture(e, MetalKegSpriteName, IndustrialPreservesJarPoweredState, new Color(170, 170, 180));
        TryLoadStateTexture(e, MetalKegSpriteName, IndustrialPreservesJarUnpoweredState, new Color(170, 170, 180));
        TryLoadStateTexture(e, HardIridiumKegSpriteName, IndustrialPreservesJarPoweredState, new Color(150, 110, 210));
        TryLoadStateTexture(e, HardIridiumKegSpriteName, IndustrialPreservesJarUnpoweredState, new Color(150, 110, 210));
    }

    private void TryLoadTexture(AssetRequestedEventArgs e, string assetKey, string spriteName, Color tint)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(assetKey))
            return;

        e.LoadFrom(() => LoadTextureOrFallback(spriteName, spriteName, tint), AssetLoadPriority.Medium);
    }

    private void TryLoadObjectTexture(AssetRequestedEventArgs e, string assetKey, string spriteName, Color tint)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(assetKey))
            return;

        e.LoadFrom(() => LoadObjectTextureOrFallback(spriteName, tint), AssetLoadPriority.Medium);
    }

    private void TryLoadOptionalTexture(AssetRequestedEventArgs e, string assetKey, string spriteName)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(assetKey))
            return;

        string customPath = GetSpriteRelativePath(spriteName);
        string fullPath = Path.Combine(Helper.DirectoryPath, customPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return;

        e.LoadFrom(() => Helper.ModContent.Load<Texture2D>(customPath), AssetLoadPriority.Medium);
    }

    private void TryLoadStateTexture(AssetRequestedEventArgs e, string baseSpriteName, string stateName, Color tint)
    {
        string stateSpriteName = GetStateSpriteName(baseSpriteName, stateName);
        string assetKey = GetTextureAsset(stateSpriteName);
        if (!e.NameWithoutLocale.IsEquivalentTo(assetKey))
            return;

        if (baseSpriteName == "PowerConduit")
        {
            string fullPath = GetSpriteDiskPath(stateSpriteName);
            LogConduitDiagnosticOnce(
                $"asset-request|{stateSpriteName}",
                $"[PowerGrid] Conduit state asset requested: assetKey={assetKey}, fileExists={File.Exists(fullPath)}, path={fullPath}");
        }

        e.LoadFrom(() => LoadTextureOrFallback(stateSpriteName, baseSpriteName, tint), AssetLoadPriority.Medium);
    }

    private Texture2D LoadTextureOrFallback(string spriteName, string fallbackSpriteName, Color tint)
    {
        string customPath = GetSpriteRelativePath(spriteName);
        string fullPath = Path.Combine(Helper.DirectoryPath, customPath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
        {
            try
            {
                return Helper.ModContent.Load<Texture2D>(customPath);
            }
            catch (Exception ex)
            {
                if (invalidStateSpritesLogged.Add(spriteName))
                {
                    Monitor.Log(
                        $"[PowerGrid] Failed to load sprite asset '{customPath}'. Falling back to '{fallbackSpriteName}'. {ex.Message}",
                        LogLevel.Warn);
                }
            }
        }

        if (!string.Equals(spriteName, fallbackSpriteName, StringComparison.Ordinal))
            return LoadTextureOrFallback(fallbackSpriteName, fallbackSpriteName, tint);

        return BuildPlaceholderSprite(fallbackSpriteName, tint);
    }

    private Texture2D LoadObjectTextureOrFallback(string spriteName, Color tint)
    {
        string customPath = GetSpriteRelativePath(spriteName);
        string fullPath = Path.Combine(Helper.DirectoryPath, customPath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
        {
            try
            {
                return Helper.ModContent.Load<Texture2D>(customPath);
            }
            catch (Exception ex)
            {
                if (invalidStateSpritesLogged.Add(spriteName))
                {
                    Monitor.Log(
                        $"[PowerGrid] Failed to load item sprite asset '{customPath}'. Falling back to a placeholder. {ex.Message}",
                        LogLevel.Warn);
                }
            }
        }

        return BuildPlaceholderObjectSprite(tint);
    }

    private static Texture2D BuildPlaceholderSprite(string spriteName, Color tint)
    {
        const int tileW = 16;
        const int tileH = 32;

        // Cables need a full 16-frame spritesheet (4x4 of 16x32 sprites)
        bool isCable = spriteName.EndsWith("Cable");
        int sheetW = isCable ? tileW * 4 : tileW;
        int sheetH = isCable ? tileH * 4 : tileH;

        Texture2D result = new(Game1.graphics.GraphicsDevice, sheetW, sheetH);
        var pixels = new Color[sheetW * sheetH];

        // For cables, we just create a tinted semi-transparent box for each frame
        // (In a real sprite, each frame would have different connector arms)
        if (isCable)
        {
            for (int mask = 0; mask < 16; mask++)
            {
                int col = mask % 4;
                int row = mask / 4;
                int startX = col * tileW;
                int startY = row * tileH;

                for (int y = 0; y < tileH; y++)
                {
                    for (int x = 0; x < tileW; x++)
                    {
                        // Draw a central dot
                        bool isCenter = x >= 6 && x <= 9 && y >= 22 && y <= 25;
                        
                        // Draw arms based on bitmask (Up=1, Right=2, Down=4, Left=8)
                        bool isUp = (mask & 1) != 0 && x >= 6 && x <= 9 && y >= 16 && y < 22;
                        bool isRight = (mask & 2) != 0 && x > 9 && x <= 15 && y >= 22 && y <= 25;
                        bool isDown = (mask & 4) != 0 && x >= 6 && x <= 9 && y > 25 && y <= 31;
                        bool isLeft = (mask & 8) != 0 && x >= 0 && x < 6 && y >= 22 && y <= 25;

                        if (isCenter || isUp || isRight || isDown || isLeft)
                        {
                            pixels[(startY + y) * sheetW + (startX + x)] = tint;
                        }
                    }
                }
            }
            result.SetData(pixels);
            return result;
        }

        // For non-cables, use vanilla sprite indices as placeholders
        int sourceIndex = spriteName switch
        {
            "Biofuel" => 92,          // Sap
            "SteamGenerator" => 13,   // Furnace
            "CombustionGenerator" => 13,
            "WindGenerator" => 10,    // Bee House
            "BasicBattery" => 36,     // Lightning Rod
            "IridiumBattery" => 36,
            "PowerConduit" => 8,      // Scarecrow
            "IndustrialPreservesJar" => 15,
            "MetalCask" => 163,
            "MetalKeg" => 12,
            "HardIridiumKeg" => 12,
            _ => 12                   // Keg fallback
        };

        Texture2D baseTexture = Game1.content.Load<Texture2D>(DefaultBigCraftableTexture);
        int tilesPerRow = Math.Max(1, baseTexture.Width / tileW);
        int srcCol = sourceIndex % tilesPerRow;
        int srcRow = sourceIndex / tilesPerRow;

        var srcRect = new Rectangle(srcCol * tileW, srcRow * tileH, tileW, tileH);
        var basePixels = new Color[tileW * tileH];
        baseTexture.GetData(0, srcRect, basePixels, 0, basePixels.Length);

        for (int i = 0; i < basePixels.Length; i++)
        {
            Color p = basePixels[i];
            if (p.A == 0) continue;
            pixels[i] = new Color(
                (byte)(p.R * tint.R / 255),
                (byte)(p.G * tint.G / 255),
                (byte)(p.B * tint.B / 255),
                p.A);
        }

        result.SetData(pixels);
        return result;
    }

    private static Texture2D BuildPlaceholderObjectSprite(Color tint)
    {
        const int tileW = 16;
        const int tileH = 16;

        Texture2D result = new(Game1.graphics.GraphicsDevice, tileW, tileH);
        var pixels = new Color[tileW * tileH];

        for (int y = 0; y < tileH; y++)
        {
            for (int x = 0; x < tileW; x++)
            {
                bool body = x >= 4 && x <= 11 && y >= 3 && y <= 12;
                bool cap = x >= 6 && x <= 9 && y >= 1 && y <= 3;
                bool highlight = x >= 5 && x <= 6 && y >= 5 && y <= 9;
                if (!body && !cap)
                    continue;

                Color pixel = highlight
                    ? new Color(Math.Min(255, tint.R + 40), Math.Min(255, tint.G + 40), Math.Min(255, tint.B + 24))
                    : tint;
                pixels[y * tileW + x] = pixel;
            }
        }

        result.SetData(pixels);
        return result;
    }

    private void EditBigCraftables(IAssetData asset)
    {
        var dict = asset.AsDictionary<string, BigCraftableData>().Data;

        // Find a template BigCraftable for cloning (vanilla Furnace at index 13 is reliable)
        BigCraftableData? template = null;
        if (dict.TryGetValue("13", out BigCraftableData? furnace))
            template = furnace;
        else if (dict.Count > 0)
            template = dict.Values.First();

        if (template == null)
        {
            Monitor.Log("[PowerGrid] No BigCraftable template found. Cannot register items.", LogLevel.Error);
            return;
        }

        // Cables (inventory/world base icon uses sprite index 0; connected arms are overlaid dynamically).
        RegisterBigCraftable(dict, template, PowerConstants.CopperCableId, "Copper Cable",
            "A copper electrical cable. Connects power grid components. Throughput: low.", CopperCableTexture, passable: true);
        RegisterBigCraftable(dict, template, PowerConstants.IronCableId, "Iron Cable",
            "An iron electrical cable. Better throughput than copper.", IronCableTexture, passable: true);
        RegisterBigCraftable(dict, template, PowerConstants.IridiumCableId, "Iridium Cable",
            "An iridium electrical cable. Maximum throughput.", IridiumCableTexture, passable: true);

        // Generators
        RegisterBigCraftable(dict, template, PowerConstants.SteamGeneratorId, "Steam Generator",
            "Burns fuel (Coal, Wood, Hardwood) to produce EU. Place fuel inside to power it.", SteamGeneratorTexture);
        RegisterBigCraftable(dict, template, PowerConstants.CombustionGeneratorId, "Combustion Generator",
            "Burns Biofuel to produce stable midgame EU. Place fuel inside to power it.", CombustionGeneratorTexture);
        RegisterBigCraftable(dict, template, PowerConstants.WindGeneratorId, "Wind Generator",
            "Produces EU passively. Output varies with weather (rain/storm = bonus, snow = reduced).", WindGeneratorTexture);

        // Batteries
        RegisterBigCraftable(dict, template, PowerConstants.BasicBatteryId, "Basic Power Battery",
            "Stores EU for later use. Small capacity with minor daily leak.", BasicBatteryTexture);
        RegisterBigCraftable(dict, template, PowerConstants.IridiumBatteryId, "Iridium Power Battery",
            "High-capacity EU storage. Minor daily leak.", IridiumBatteryTexture);

        // Power Conduit
        RegisterBigCraftable(dict, template, PowerConstants.PowerConduitId, "Power Conduit",
            "Links power networks across locations. Interact with two conduits to pair them.", PowerConduitTexture);

        RegisterIndustrialPreservesJar(dict);
        RegisterMetalCask(dict);
        RegisterPoweredArtisanMachines(dict);
    }

    private void EditObjects(IAssetData asset)
    {
        var dict = asset.AsDictionary<string, ObjectData>().Data;
        dict[PowerConstants.BiofuelId] = CreateBiofuelObjectData();
    }

    private ObjectData CreateBiofuelObjectData()
    {
        ObjectData? data = JsonSerializer.Deserialize<ObjectData>(
            $$"""
            {
              "Name": "{{BiofuelRecipeKey}}",
              "DisplayName": "{{BiofuelRecipeKey}}",
              "Description": "A dense processed fuel pellet for Combustion Generators.",
              "Type": "Basic",
              "Category": 0,
              "Price": 80,
              "Texture": "{{BiofuelTexture}}",
              "SpriteIndex": 0,
              "Edibility": -300,
              "CanBeGivenAsGift": false,
              "CanBeTrashed": true,
              "ExcludeFromShippingCollection": true,
              "ExcludeFromRandomSale": true,
              "ContextTags": [ "powergrid_biofuel", "powergrid_fuel" ]
            }
            """,
            GameDataCloneJsonOptions);

        return data ?? throw new InvalidOperationException("Failed to build Biofuel object data.");
    }

    private void RegisterIndustrialPreservesJar(IDictionary<string, BigCraftableData> dict)
    {
        KeyValuePair<string, BigCraftableData>? templateOpt =
            FindBigCraftableTemplate(dict, targetDisplayName: "Preserves Jar", preferredKey: "15");

        if (templateOpt == null)
        {
            if (!loggedMissingPreservesTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Preserves Jar template in Data/BigCraftables; Industrial Preserves Jar was not added.", LogLevel.Error);
                loggedMissingPreservesTemplate = true;
            }

            return;
        }

        KeyValuePair<string, BigCraftableData> template = templateOpt.Value;
        vanillaPreservesJarBigCraftableId = template.Key;

        BigCraftableData industrialData = CloneJson(template.Value);
        industrialData.Name = IndustrialPreservesJarRecipeKey;
        industrialData.DisplayName = IndustrialPreservesJarRecipeKey;
        industrialData.Description = "An industrial preserves jar built for powered throughput.";
        industrialData.Texture = IndustrialPreservesJarTexture;
        industrialData.SpriteIndex = 0;

        dict[PowerConstants.IndustrialPreservesJarId] = industrialData;
    }

    private void RegisterMetalCask(IDictionary<string, BigCraftableData> dict)
    {
        KeyValuePair<string, BigCraftableData>? templateOpt =
            FindBigCraftableTemplate(dict, targetDisplayName: "Cask", preferredKey: "163");

        if (templateOpt == null)
        {
            if (!loggedMissingCaskTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Cask template in Data/BigCraftables; Metal Cask was not added.", LogLevel.Error);
                loggedMissingCaskTemplate = true;
            }

            return;
        }

        KeyValuePair<string, BigCraftableData> template = templateOpt.Value;
        vanillaCaskBigCraftableId = template.Key;

        BigCraftableData metalCaskData = CloneJson(template.Value);
        metalCaskData.Name = MetalCaskRecipeKey;
        metalCaskData.DisplayName = MetalCaskRecipeKey;
        metalCaskData.Description = "An industrial cask that can age artisan goods in player-owned indoor spaces. Slower than a cellar cask unless powered.";
        metalCaskData.Texture = MetalCaskTexture;
        metalCaskData.SpriteIndex = 0;
        dict[PowerConstants.MetalCaskId] = metalCaskData;
    }

    private void RegisterPoweredArtisanMachines(IDictionary<string, BigCraftableData> dict)
    {
        KeyValuePair<string, BigCraftableData>? kegTemplateOpt =
            FindBigCraftableTemplate(dict, targetDisplayName: "Keg", preferredKey: "12");

        if (kegTemplateOpt == null)
        {
            if (!loggedMissingKegTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Keg template in Data/BigCraftables; Metal Keg and Hard Iridium Keg were not added.", LogLevel.Error);
                loggedMissingKegTemplate = true;
            }

            return;
        }

        KeyValuePair<string, BigCraftableData> kegTemplate = kegTemplateOpt.Value;
        vanillaKegBigCraftableId = kegTemplate.Key;

        BigCraftableData metalKegData = CloneJson(kegTemplate.Value);
        metalKegData.Name = MetalKegRecipeKey;
        metalKegData.DisplayName = MetalKegRecipeKey;
        metalKegData.Description = "A sturdy metal keg built for powered throughput.";
        metalKegData.Texture = MetalKegTexture;
        metalKegData.SpriteIndex = 0;
        dict[PowerConstants.MetalKegId] = metalKegData;

        KeyValuePair<string, BigCraftableData>? hardwoodTemplateOpt = FindHardwoodKegBigCraftableTemplate(dict);
        if (hardwoodTemplateOpt != null)
            hardwoodKegBigCraftableId = hardwoodTemplateOpt.Value.Key;
        else if (!loggedHardIridiumFallbackToVanillaKeg)
        {
            Monitor.Log("[PowerGrid] Hardwood Keg template was not found in Data/BigCraftables; Hard Iridium Keg will use vanilla Keg item data.", LogLevel.Info);
            loggedHardIridiumFallbackToVanillaKeg = true;
        }

        BigCraftableData hardIridiumData = CloneJson(hardwoodTemplateOpt?.Value ?? kegTemplate.Value);
        hardIridiumData.Name = HardIridiumKegRecipeKey;
        hardIridiumData.DisplayName = HardIridiumKegRecipeKey;
        hardIridiumData.Description = "An iridium-reinforced keg. Uses Hardwood Keg behavior when available, otherwise vanilla Keg behavior.";
        hardIridiumData.Texture = HardIridiumKegTexture;
        hardIridiumData.SpriteIndex = 0;
        dict[PowerConstants.HardIridiumKegId] = hardIridiumData;
    }

    private void RegisterBigCraftable(IDictionary<string, BigCraftableData> dict, BigCraftableData template,
        string itemId, string displayName, string description, string textureAsset, bool passable = false)
    {
        BigCraftableData data = CloneJson(template);
        data.DisplayName = displayName;
        data.Name = displayName;
        data.Description = description;
        data.Texture = textureAsset;
        data.SpriteIndex = 0;

        if (passable)
            TrySetPassableFlags(data);

        dict[itemId] = data;
    }

    private void EditCraftingRecipes(IAssetData asset)
    {
        var dict = asset.AsDictionary<string, string>().Data;

        // Copper Cable: 334 (Copper Bar) x3 => 10 cables
        dict["Copper Cable"] = $"334 3/Field/{PowerConstants.CopperCableId} 10/true/null/";
        // Iron Cable: 335 (Iron Bar) x3 => 10 cables
        dict["Iron Cable"] = $"335 3/Field/{PowerConstants.IronCableId} 10/true/null/";
        // Iridium Cable: 337 (Iridium Bar) x2, 338 (Refined Quartz) x1 => 10 cables
        dict["Iridium Cable"] = $"337 2 338 1/Field/{PowerConstants.IridiumCableId} 10/true/null/";

        // Biofuel: 92 (Sap) x30, 382 (Coal) x2 => 1 Biofuel
        dict[BiofuelRecipeKey] = $"92 30 382 2/Field/{PowerConstants.BiofuelId}/false/null/";

        // Steam Generator: 335 (Iron Bar) x6, 334 (Copper Bar) x3, 382 (Coal) x8, 338 (Refined Quartz) x2
        dict["Steam Generator"] = $"335 6 334 3 382 8 338 2/Field/{PowerConstants.SteamGeneratorId}/true/null/";
        // Combustion Generator: Steam Generator x1, Iron Bar x10, Gold Bar x6, Refined Quartz x4
        dict[CombustionGeneratorRecipeKey] = $"{PowerConstants.SteamGeneratorId} 1 335 10 336 6 338 4/Field/{PowerConstants.CombustionGeneratorId}/true/null/";
        // Wind Generator: 335 (Iron Bar) x6, 338 (Refined Quartz) x4, 787 (Battery Pack) x1, 382 (Coal) x4
        dict["Wind Generator"] = $"335 6 338 4 787 1 382 4/Field/{PowerConstants.WindGeneratorId}/true/null/";

        // Basic Power Battery: 787 (Battery Pack) x1, 334 (Copper Bar) x5, 338 (Refined Quartz) x2
        dict["Basic Power Battery"] = $"787 1 334 5 338 2/Field/{PowerConstants.BasicBatteryId}/true/null/";
        // Iridium Power Battery: 787 (Battery Pack) x3, 337 (Iridium Bar) x2, 338 (Refined Quartz) x5
        dict["Iridium Power Battery"] = $"787 3 337 2 338 5/Field/{PowerConstants.IridiumBatteryId}/true/null/";

        // Power Conduit: 337 (Iridium Bar) x1, 787 (Battery Pack) x1, 338 (Refined Quartz) x2
        dict["Power Conduit"] = $"337 1 787 1 338 2/Field/{PowerConstants.PowerConduitId}/true/null/";

        string? preservesTemplate = dict.TryGetValue("Preserves Jar", out string? value) ? value : null;
        dict[IndustrialPreservesJarRecipeKey] = BuildRecipeFromTemplate(
            preservesTemplate,
            ingredients: "388 30 382 10 335 8 338 1",
            resultItemId: PowerConstants.IndustrialPreservesJarId);

        string? caskTemplate = dict.TryGetValue("Cask", out string? caskValue) ? caskValue : null;
        dict[MetalCaskRecipeKey] = BuildRecipeFromTemplate(
            caskTemplate,
            ingredients: "709 10 335 12 337 3 338 1",
            resultItemId: PowerConstants.MetalCaskId);

        string? kegTemplate = dict.TryGetValue("Keg", out string? kegValue) ? kegValue : null;
        dict[MetalKegRecipeKey] = BuildRecipeFromTemplate(
            kegTemplate,
            ingredients: "335 12 334 6 338 1",
            resultItemId: PowerConstants.MetalKegId);

        string? hardwoodKegTemplate = dict.TryGetValue("Hardwood Keg", out string? hardwoodValue) ? hardwoodValue : null;
        dict[HardIridiumKegRecipeKey] = BuildRecipeFromTemplate(
            hardwoodKegTemplate ?? kegTemplate,
            ingredients: "337 5 335 8 338 2",
            resultItemId: PowerConstants.HardIridiumKegId);
    }

    private void EditMachines(IAssetData asset)
    {
        IDictionary<string, MachineData> machines = asset.AsDictionary<string, MachineData>().Data;
        RegisterSteamGeneratorMachine(machines);
        RegisterCombustionGeneratorMachine(machines);

        string? templateKey = GetPreservesJarMachineKey(machines, vanillaPreservesJarBigCraftableId);
        if (templateKey == null || !machines.TryGetValue(templateKey, out MachineData? template))
        {
            if (!loggedMissingPreservesMachineTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Preserves Jar machine entry in Data/Machines; Industrial Preserves Jar machine rules were not added.", LogLevel.Error);
                loggedMissingPreservesMachineTemplate = true;
            }
        }
        else
        {
            MachineData machine = CloneMachineByJson(template);
            machines[PowerConstants.Q(PowerConstants.IndustrialPreservesJarId)] = machine;
            machines[PowerConstants.IndustrialPreservesJarId] = machine;
        }

        RegisterMetalCaskMachine(machines);
        RegisterMetalKegMachines(machines);
    }

    private void RegisterSteamGeneratorMachine(IDictionary<string, MachineData> machines)
    {
        MachineData machine = CreateInputOnlyMachineData();
        machines[PowerConstants.Q(PowerConstants.SteamGeneratorId)] = machine;
        machines[PowerConstants.SteamGeneratorId] = machine;
    }

    private void RegisterCombustionGeneratorMachine(IDictionary<string, MachineData> machines)
    {
        MachineData machine = CreateInputOnlyMachineData();
        machines[PowerConstants.Q(PowerConstants.CombustionGeneratorId)] = machine;
        machines[PowerConstants.CombustionGeneratorId] = machine;
    }

    private void RegisterMetalCaskMachine(IDictionary<string, MachineData> machines)
    {
        string? caskBaseKey = GetCaskMachineKey(machines, vanillaCaskBigCraftableId);
        if (caskBaseKey == null || !machines.TryGetValue(caskBaseKey, out MachineData? caskMachine))
        {
            if (!loggedMissingCaskMachineTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Cask machine entry in Data/Machines; Metal Cask machine rules were not added.", LogLevel.Error);
                loggedMissingCaskMachineTemplate = true;
            }

            return;
        }

        MachineData metalCaskMachine = CreateMetalCaskMachine(caskMachine);
        machines[PowerConstants.Q(PowerConstants.MetalCaskId)] = metalCaskMachine;
        machines[PowerConstants.MetalCaskId] = metalCaskMachine;
    }

    private void RegisterMetalKegMachines(IDictionary<string, MachineData> machines)
    {
        string? kegBaseKey = GetKegMachineKey(machines, vanillaKegBigCraftableId);
        if (kegBaseKey == null || !machines.TryGetValue(kegBaseKey, out MachineData? kegMachine))
        {
            if (!loggedMissingKegMachineTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Keg machine entry in Data/Machines; Metal Keg and Hard Iridium Keg machine rules were not added.", LogLevel.Error);
                loggedMissingKegMachineTemplate = true;
            }

            return;
        }

        MachineData metalKegMachine = CloneMachineByJson(kegMachine);
        machines[PowerConstants.Q(PowerConstants.MetalKegId)] = metalKegMachine;
        machines[PowerConstants.MetalKegId] = metalKegMachine;

        MachineData hardIridiumTemplate = kegMachine;
        string? hardwoodKey = GetHardwoodKegMachineKey(machines, hardwoodKegBigCraftableId);
        if (hardwoodKey != null && machines.TryGetValue(hardwoodKey, out MachineData? hardwoodMachine))
        {
            hardIridiumTemplate = hardwoodMachine;
            if (!loggedHardIridiumBoundToHardwoodKeg)
            {
                Monitor.Log($"[PowerGrid] Hard Iridium Keg machine behavior cloned from '{hardwoodKey}'.", LogLevel.Info);
                loggedHardIridiumBoundToHardwoodKeg = true;
            }
        }
        else if (!loggedHardIridiumFallbackToVanillaKeg)
        {
            Monitor.Log("[PowerGrid] Hardwood Keg machine template was not found in Data/Machines; Hard Iridium Keg will use vanilla Keg machine behavior.", LogLevel.Info);
            loggedHardIridiumFallbackToVanillaKeg = true;
        }

        MachineData hardIridiumMachine = CloneMachineByJson(hardIridiumTemplate);
        machines[PowerConstants.Q(PowerConstants.HardIridiumKegId)] = hardIridiumMachine;
        machines[PowerConstants.HardIridiumKegId] = hardIridiumMachine;
    }

    private static string BuildRecipeFromTemplate(string? template, string resultItemId)
    {
        if (string.IsNullOrWhiteSpace(template))
            return $"388 50 334 1/Home/{resultItemId}/true/null/";

        string[] parts = template.Split('/');
        if (parts.Length < 5)
            return $"388 50 334 1/Home/{resultItemId}/true/null/";

        parts[2] = resultItemId;
        parts[3] = "true";

        string rebuilt = string.Join("/", parts);
        if (!rebuilt.EndsWith("/", StringComparison.Ordinal))
            rebuilt += "/";
        return rebuilt;
    }

    private static string BuildRecipeFromTemplate(string? template, string ingredients, string resultItemId)
    {
        if (string.IsNullOrWhiteSpace(template))
            return $"{ingredients}/Field/{resultItemId}/true/null/";

        string[] parts = template.Split('/');
        if (parts.Length < 5)
            return $"{ingredients}/Field/{resultItemId}/true/null/";

        parts[0] = ingredients;
        parts[2] = resultItemId;
        parts[3] = "true";

        string rebuilt = string.Join("/", parts);
        if (!rebuilt.EndsWith("/", StringComparison.Ordinal))
            rebuilt += "/";
        return rebuilt;
    }

    private static string? GetPreservesJarMachineKey(IDictionary<string, MachineData> machines, string? vanillaBigCraftableId)
    {
        if (!string.IsNullOrWhiteSpace(vanillaBigCraftableId))
        {
            string qualified = "(BC)" + vanillaBigCraftableId;
            if (machines.ContainsKey(qualified))
                return qualified;

            if (machines.ContainsKey(vanillaBigCraftableId))
                return vanillaBigCraftableId;
        }

        if (machines.ContainsKey("(BC)15"))
            return "(BC)15";
        if (machines.ContainsKey("15"))
            return "15";

        foreach (string key in machines.Keys)
        {
            if (key.Contains("PreservesJar", StringComparison.OrdinalIgnoreCase)
                || key.Contains("Preserves Jar", StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return null;
    }

    private static string? GetKegMachineKey(IDictionary<string, MachineData> machines, string? vanillaBigCraftableId)
    {
        if (!string.IsNullOrWhiteSpace(vanillaBigCraftableId))
        {
            string qualified = "(BC)" + vanillaBigCraftableId;
            if (machines.ContainsKey(qualified))
                return qualified;

            if (machines.ContainsKey(vanillaBigCraftableId))
                return vanillaBigCraftableId;
        }

        if (machines.ContainsKey("(BC)12"))
            return "(BC)12";
        if (machines.ContainsKey("12"))
            return "12";

        foreach (string key in machines.Keys)
        {
            if (key.Contains("Keg", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("Hardwood", StringComparison.OrdinalIgnoreCase)
                && !key.Contains("Iridium", StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return null;
    }

    private static string? GetCaskMachineKey(IDictionary<string, MachineData> machines, string? vanillaBigCraftableId)
    {
        if (!string.IsNullOrWhiteSpace(vanillaBigCraftableId))
        {
            string qualified = "(BC)" + vanillaBigCraftableId;
            if (machines.ContainsKey(qualified))
                return qualified;

            if (machines.ContainsKey(vanillaBigCraftableId))
                return vanillaBigCraftableId;
        }

        if (machines.ContainsKey("(BC)163"))
            return "(BC)163";
        if (machines.ContainsKey("163"))
            return "163";

        foreach (string key in machines.Keys)
        {
            if (key.Contains("Cask", StringComparison.OrdinalIgnoreCase))
                return key;
        }

        return null;
    }

    private static string? GetHardwoodKegMachineKey(IDictionary<string, MachineData> machines, string? hardwoodBigCraftableId)
    {
        foreach (string candidate in new[] { PowerConstants.Q(GofHardwoodKegId), GofHardwoodKegId })
        {
            if (machines.ContainsKey(candidate))
                return candidate;
        }

        if (!string.IsNullOrWhiteSpace(hardwoodBigCraftableId))
        {
            string qualified = "(BC)" + hardwoodBigCraftableId;
            if (machines.ContainsKey(qualified))
                return qualified;

            if (machines.ContainsKey(hardwoodBigCraftableId))
                return hardwoodBigCraftableId;
        }

        foreach (string key in machines.Keys)
        {
            if (key.Contains("Hardwood", StringComparison.OrdinalIgnoreCase)
                && key.Contains("Keg", StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return null;
    }

    private static KeyValuePair<string, BigCraftableData>? FindHardwoodKegBigCraftableTemplate(
        IDictionary<string, BigCraftableData> dict)
    {
        foreach (string candidate in new[] { GofHardwoodKegId, PowerConstants.Q(GofHardwoodKegId) })
        {
            if (dict.TryGetValue(candidate, out BigCraftableData? byExplicitId))
                return new KeyValuePair<string, BigCraftableData>(candidate, byExplicitId);
        }

        return FindBigCraftableTemplate(dict, targetDisplayName: "Hardwood Keg", preferredKey: null);
    }

    private static KeyValuePair<string, BigCraftableData>? FindBigCraftableTemplate(
        IDictionary<string, BigCraftableData> dict,
        string targetDisplayName,
        string? preferredKey)
    {
        if (!string.IsNullOrWhiteSpace(preferredKey)
            && dict.TryGetValue(preferredKey, out BigCraftableData? byKey)
            && HasDisplayName(byKey, targetDisplayName))
        {
            return new KeyValuePair<string, BigCraftableData>(preferredKey, byKey);
        }

        foreach (KeyValuePair<string, BigCraftableData> pair in dict)
        {
            if (HasDisplayName(pair.Value, targetDisplayName))
                return pair;
        }

        return null;
    }

    private static bool HasDisplayName(BigCraftableData data, string displayName)
    {
        if (!string.IsNullOrWhiteSpace(data.DisplayName) && data.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(data.Name) && data.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static MachineData CloneMachineByJson(MachineData template)
    {
        string json = JsonSerializer.Serialize(template, GameDataCloneJsonOptions);
        MachineData? clone = JsonSerializer.Deserialize<MachineData>(json, GameDataCloneJsonOptions);
        if (clone == null)
            throw new InvalidOperationException("Failed to clone MachineData via JSON.");
        return clone;
    }

    private static MachineData CreateMetalCaskMachine(MachineData template)
    {
        JsonNode? root = JsonNode.Parse(JsonSerializer.Serialize(template, GameDataCloneJsonOptions));
        JsonArray? rules = root?["OutputRules"] as JsonArray;
        if (rules != null)
        {
            foreach (JsonNode? ruleNode in rules)
            {
                if (ruleNode is not JsonObject ruleObject)
                    continue;

                JsonObject customData = ruleObject["CustomData"] as JsonObject ?? new JsonObject();

                float multiplier = 1f;
                if (customData["AgingMultiplier"] != null
                    && float.TryParse(
                        customData["AgingMultiplier"]!.GetValue<string>(),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float existingMultiplier)
                    && existingMultiplier > 0f)
                {
                    multiplier = existingMultiplier;
                }

                float slowedMultiplier = multiplier * MetalCaskBaseAgingMultiplier;
                customData["AgingMultiplier"] = slowedMultiplier.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                ruleObject["CustomData"] = customData;
            }
        }

        MachineData? machine = root?.Deserialize<MachineData>(GameDataCloneJsonOptions);
        if (machine == null)
            throw new InvalidOperationException("Failed to build Metal Cask machine data.");

        return machine;
    }

    private static MachineData CreateInputOnlyMachineData()
    {
        MachineData? machine = JsonSerializer.Deserialize<MachineData>(
            """
            {
              "HasInput": true
            }
            """,
            GameDataCloneJsonOptions);

        return machine ?? throw new InvalidOperationException("Failed to build input-only machine data.");
    }

    // ─────────────────────────────────────────────
    // Console Commands
    // ─────────────────────────────────────────────

    private void CmdStatus(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save first.", LogLevel.Info);
            return;
        }

        string locationName = Game1.currentLocation.NameOrUniqueName;
        var networks = PowerQuery.GetNetworkSnapshots(locationName);

        if (networks.Count == 0)
        {
            Monitor.Log($"[PowerGrid] No power networks in '{locationName}'.", LogLevel.Info);
            return;
        }

        var allConsumers = PowerQuery.GetConsumerSnapshots();
        var allBatteries = PowerQuery.GetBatterySnapshots();

        foreach (var net in networks.OrderBy(n => n.NetworkId))
        {
            string locations = net.LocationNames.Length > 0 ? string.Join(", ", net.LocationNames) : "(none)";
            string throughput = net.CableThroughputCap <= 0 ? "unlimited" : net.CableThroughputCap.ToString();

            Monitor.Log($"--- Network #{net.NetworkId} ---", LogLevel.Info);
            Monitor.Log($"  Locations: [{locations}]", LogLevel.Info);
            Monitor.Log($"  Generators: {net.GeneratorCount}, Cables: {net.CableCount}, Batteries: {net.BatteryCount}, Consumers: {net.ConsumerCount}, Conduits: {net.ConduitCount}", LogLevel.Info);
            Monitor.Log($"  Generation/tick: {net.TotalGenerationPerTick} EU, Demand/tick: {net.TotalDemandPerTick} EU", LogLevel.Info);
            Monitor.Log($"  Cable throughput cap: {throughput} EU", LogLevel.Info);
            Monitor.Log($"  Battery capacity: {net.TotalBatteryCapacity} EU", LogLevel.Info);
            Monitor.Log($"  Last tick: Generated={net.LastTickGenerated}, FromBatteries={net.LastTickFromBatteries}, Consumed={net.LastTickConsumed}, StoredInBatteries={net.LastTickStoredInBatteries}", LogLevel.Info);

            foreach (var bat in allBatteries
                .Where(b => b.NetworkId == net.NetworkId)
                .OrderBy(b => b.LocationName, StringComparer.Ordinal)
                .ThenBy(b => b.TileX)
                .ThenBy(b => b.TileY))
            {
                Monitor.Log($"    Battery [{bat.LocationName} @ {bat.TileX},{bat.TileY}]: {bat.Charge}/{bat.Capacity} EU", LogLevel.Info);
            }

            foreach (var consumer in allConsumers
                .Where(c => c.NetworkId == net.NetworkId)
                .OrderBy(c => c.LocationName, StringComparer.Ordinal)
                .ThenBy(c => c.TileX)
                .ThenBy(c => c.TileY))
            {
                Monitor.Log($"    Consumer [{consumer.LocationName} @ {consumer.TileX},{consumer.TileY}] {consumer.ItemId}: {consumer.EUAllocated}/{consumer.DemandPerTick} EU, speedup={consumer.SpeedupFraction:P0}, accel={consumer.MinutesAccelerated}min, {FormatConsumerProgress(consumer)}", LogLevel.Info);
            }
        }
    }

    private void CmdDebug(string command, string[] args)
    {
        debugOverlayActive = !debugOverlayActive;
        Monitor.Log($"[PowerGrid] Debug overlay: {(debugOverlayActive ? "ON" : "OFF")}", LogLevel.Info);
    }

    private void CmdConduitReset(string command, string[] args)
    {
        ConduitMgr.CancelPairing();
        Monitor.Log("[PowerGrid] Conduit pairing cancelled.", LogLevel.Info);
    }

    private void CmdExtractSprites(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("[PowerGrid] Must be in-game to extract sprites.", LogLevel.Error);
            return;
        }

        PlaceholderExtractor.ExtractAll(Helper, Monitor);
        Monitor.Log("[PowerGrid] Placeholder sprites extracted to Assets/ folder. You can now edit them!", LogLevel.Info);
    }

    private void CmdPowerTab(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save first.", LogLevel.Info);
            return;
        }

        OpenPowerTab();
    }

    private void CmdQueryDump(string command, string[] args)
    {
        string? locationName = args.Length > 0 ? args[0] : null;

        var networks = PowerQuery.GetNetworkSnapshots(locationName);
        var consumers = PowerQuery.GetConsumerSnapshots(locationName);
        var generators = PowerQuery.GetGeneratorSnapshots(locationName);
        var batteries = PowerQuery.GetBatterySnapshots(locationName);

        string scope = string.IsNullOrWhiteSpace(locationName) ? "all loaded locations" : locationName;
        Monitor.Log($"[PowerGrid] Query dump for {scope}: networks={networks.Count}, consumers={consumers.Count}, generators={generators.Count}, batteries={batteries.Count}", LogLevel.Info);

        foreach (var network in networks)
        {
            string locations = network.LocationNames.Length > 0 ? string.Join(", ", network.LocationNames) : "(none)";
            string throughput = network.CableThroughputCap <= 0 ? "unlimited" : network.CableThroughputCap.ToString();
            Monitor.Log($"  Network #{network.NetworkId}: locations=[{locations}], cables={network.CableCount}, generators={network.GeneratorCount}, batteries={network.BatteryCount}, consumers={network.ConsumerCount}, conduits={network.ConduitCount}, gen/tick={network.TotalGenerationPerTick}, demand/tick={network.TotalDemandPerTick}, throughput={throughput}, stored={network.TotalStoredEU}/{network.TotalBatteryCapacity} EU, lastTick(gen={network.LastTickGenerated}, used={network.LastTickConsumed}, batteryOut={network.LastTickFromBatteries}, batteryIn={network.LastTickStoredInBatteries})", LogLevel.Info);
        }

        foreach (var consumer in consumers)
        {
            Monitor.Log($"  Consumer [{consumer.LocationName} @ {consumer.TileX},{consumer.TileY}] {consumer.DisplayName}: processing={consumer.IsProcessing}, powered={consumer.IsPowered}, alloc={consumer.EUAllocated}/{consumer.DemandPerTick}, speedup={consumer.SpeedupFraction:P0}, {FormatConsumerProgress(consumer)}", LogLevel.Info);
        }

        foreach (var generator in generators)
        {
            Monitor.Log($"  Generator [{generator.LocationName} @ {generator.TileX},{generator.TileY}] {generator.DisplayName}: online={generator.IsOnline}, generated={generator.GeneratedThisTick}/{generator.GenerationPerTick}, fuelTicks={generator.FuelTicksRemaining}", LogLevel.Info);
        }

        foreach (var battery in batteries)
        {
            Monitor.Log($"  Battery [{battery.LocationName} @ {battery.TileX},{battery.TileY}] {battery.DisplayName}: charge={battery.Charge}/{battery.Capacity}, drained={battery.DrainedThisTick}, stored={battery.StoredThisTick}", LogLevel.Info);
        }
    }

    private static string FormatConsumerProgress(PowerConsumerSnapshot consumer)
    {
        if (consumer.ProgressMode == "days" && !string.IsNullOrWhiteSpace(consumer.ProgressText))
            return $"progress=\"{consumer.ProgressText}\"";

        return $"remaining={consumer.MinutesRemaining}min";
    }

    private static readonly string[] GridStarterRecipeKeys =
    {
        "Copper Cable",
        "Steam Generator",
        "Basic Power Battery"
    };

    private static readonly string[] PoweredArtisanRecipeKeys =
    {
        IndustrialPreservesJarRecipeKey,
        MetalKegRecipeKey
    };

    private static readonly string[] FuelTechRecipeKeys =
    {
        BiofuelRecipeKey,
        "Iron Cable",
        CombustionGeneratorRecipeKey
    };

    private static readonly string[] AdvancedGridRecipeKeys =
    {
        "Iridium Cable",
        "Wind Generator",
        "Iridium Power Battery",
        "Power Conduit",
        MetalCaskRecipeKey,
        HardIridiumKegRecipeKey
    };

    private void CmdUnlock(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save first.", LogLevel.Info);
            return;
        }

        bool force = args.Length > 0 && args[0].Equals("force", StringComparison.OrdinalIgnoreCase);
        TryGrantRecipes(reason: $"Console:{command}", force: force);
    }

    private void TryGrantRecipes(string reason, bool force = false)
    {
        if (!Context.IsWorldReady)
            return;

        if (!Config.AutoGrantRecipes && !force)
            return;

        string unlockMode = (Config.UnlockMode ?? "existingProgress").Trim();
        if (unlockMode.Equals("disabled", StringComparison.OrdinalIgnoreCase) && !force)
            return;

        bool any = false;
        foreach (Farmer farmer in Game1.getAllFarmers())
        {
            any |= GrantPowerGridRecipes(farmer, unlockMode, force);
        }

        if (any)
            Monitor.Log($"[PowerGrid] Granted crafting recipes after {reason} (mode='{unlockMode}', force={force}).", LogLevel.Info);
        else
            Monitor.Log($"[PowerGrid] No recipes granted after {reason} (mode='{unlockMode}', force={force}).", LogLevel.Trace);
    }

    private static bool GrantPowerGridRecipes(Farmer player, string unlockMode, bool force)
    {
        if (force || unlockMode.Equals("always", StringComparison.OrdinalIgnoreCase))
            return GrantRecipeSet(player, GridStarterRecipeKeys)
                | GrantRecipeSet(player, PoweredArtisanRecipeKeys)
                | GrantRecipeSet(player, FuelTechRecipeKeys)
                | GrantRecipeSet(player, AdvancedGridRecipeKeys);

        bool any = false;
        bool knowsLightningRod = player.craftingRecipes.ContainsKey("Lightning Rod");
        bool knowsKeg = player.craftingRecipes.ContainsKey("Keg");
        bool knowsPreservesJar = player.craftingRecipes.ContainsKey("Preserves Jar");

        if (player.MiningLevel >= 5 || knowsLightningRod)
            any |= GrantRecipeSet(player, GridStarterRecipeKeys);

        if (knowsPreservesJar || knowsKeg)
            any |= GrantRecipeSet(player, PoweredArtisanRecipeKeys);

        if (player.MiningLevel >= 7 && knowsLightningRod)
            any |= GrantRecipeSet(player, FuelTechRecipeKeys);

        if (player.MiningLevel >= 9 && knowsLightningRod)
            any |= GrantRecipeSet(player, AdvancedGridRecipeKeys);

        return any;
    }

    private static bool GrantRecipeSet(Farmer player, IEnumerable<string> recipeKeys)
    {
        bool any = false;
        foreach (string key in recipeKeys)
        {
            if (player.craftingRecipes.ContainsKey(key))
                continue;

            player.craftingRecipes.Add(key, 0);
            any = true;
        }

        return any;
    }

    public override object? GetApi()
    {
        return new PowerGridApi();
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private void OpenPowerTab()
    {
        if (Game1.activeClickableMenu != null)
            return;

        Game1.activeClickableMenu = new PowerTabMenu(PowerMgr, PowerQuery, BatteryState, FuelMgr, ConduitMgr);
    }

    private static bool IsSupportedGeneratorFuel(string generatorItemId, StardewValley.Object fuel, bool allowLegacyBatteryPack = false)
    {
        return generatorItemId switch
        {
            PowerConstants.SteamGeneratorId => fuel.QualifiedItemId switch
            {
                "(O)382" => true, // Coal
                "(O)388" => true, // Wood
                "(O)709" => true, // Hardwood
                "(O)787" when allowLegacyBatteryPack => true,
                _ => false
            },
            PowerConstants.CombustionGeneratorId => fuel.QualifiedItemId == PowerConstants.QObject(PowerConstants.BiofuelId),
            _ => false
        };
    }

    private static bool IsConsumerObject(StardewValley.Object obj)
    {
        string qualifiedId = obj.QualifiedItemId ?? "";
        return ConsumerRegistry.Instance.GetConsumerDef(qualifiedId) != null;
    }

    private void ApplyPassabilityPatch()
    {
        try
        {
            harmony = new Harmony(ModManifest.UniqueID);

            MethodInfo? target = typeof(StardewValley.Object).GetMethod(
                "isPassable",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null
            )
            ?? typeof(StardewValley.Object).GetMethod(
                "isPassable",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(Character) },
                modifiers: null
            );

            if (target == null)
            {
                Monitor.Log("[PowerGrid] Couldn't patch Object.isPassable; cables may still block movement.", LogLevel.Warn);
                return;
            }

            harmony.Patch(target, postfix: new HarmonyMethod(typeof(ModEntry), nameof(CablePassablePostfix)));

            MethodInfo? drawTarget = typeof(StardewValley.Object).GetMethod(
                "draw",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) },
                modifiers: null);
            if (drawTarget != null)
            {
                harmony.Patch(
                    drawTarget,
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(StatefulPowerGridTileDrawPrefix)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(StatefulObjectTileDrawPostfix)));
            }

            MethodInfo? drawWithLayerTarget = typeof(StardewValley.Object).GetMethod(
                "draw",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(float) },
                modifiers: null);
            if (drawWithLayerTarget != null)
            {
                harmony.Patch(drawWithLayerTarget, postfix: new HarmonyMethod(typeof(ModEntry), nameof(StatefulObjectScreenDrawPostfix)));
            }

            MethodInfo? drawAboveFrontLayerTarget = typeof(StardewValley.Object).GetMethod(
                "drawAboveFrontLayer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) },
                modifiers: null);
            if (drawAboveFrontLayerTarget != null)
            {
                harmony.Patch(
                    drawAboveFrontLayerTarget,
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(StatefulObjectAboveFrontLayerPrefix)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(StatefulObjectAboveFrontLayerPostfix)));
            }

            MethodInfo? drawFloorDecorationsTarget = typeof(GameLocation).GetMethod(
                "drawFloorDecorations",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(SpriteBatch) },
                modifiers: null);
            if (drawFloorDecorationsTarget != null)
            {
                harmony.Patch(drawFloorDecorationsTarget, postfix: new HarmonyMethod(typeof(ModEntry), nameof(GameLocationDrawFloorDecorationsPostfix)));
            }
            else
            {
                Monitor.Log("[PowerGrid] Couldn't patch GameLocation.drawFloorDecorations(SpriteBatch); cables may not render.", LogLevel.Warn);
            }

            MethodInfo? placementTarget = typeof(StardewValley.Object).GetMethod(
                "placementAction",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) },
                modifiers: null);
            if (placementTarget != null)
            {
                harmony.Patch(
                    placementTarget,
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(MetalCaskPlacementPrefix)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(MetalCaskPlacementPostfix)));
            }

            if (PerformObjectDropInActionMethod != null)
            {
                harmony.Patch(
                    PerformObjectDropInActionMethod,
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(SteamGeneratorDropInActionPrefix)));
            }

            MethodInfo? isValidCaskLocationTarget = typeof(Cask).GetMethod(
                "IsValidCaskLocation",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (isValidCaskLocationTarget != null)
                harmony.Patch(isValidCaskLocationTarget, postfix: new HarmonyMethod(typeof(ModEntry), nameof(MetalCaskLocationPostfix)));

            MethodInfo? dayUpdateTarget = typeof(Cask).GetMethod(
                "DayUpdate",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (dayUpdateTarget != null)
                harmony.Patch(dayUpdateTarget, postfix: new HarmonyMethod(typeof(ModEntry), nameof(MetalCaskDayUpdatePostfix)));
        }
        catch (Exception ex)
        {
            Monitor.Log($"[PowerGrid] Failed to apply runtime object patches: {ex}", LogLevel.Error);
        }
    }

    private static bool MetalCaskPlacementPrefix(StardewValley.Object __instance, object[] __args, ref bool __result)
    {
        if (Instance == null || __instance?.QualifiedItemId != PowerConstants.Q(PowerConstants.MetalCaskId))
            return true;

        if (__args.Length < 1 || __args[0] is not GameLocation location)
            return true;

        if (Instance.IsValidMetalCaskLocation(location))
            return true;

        Game1.showRedMessage(MetalCaskPlacementError);
        __result = false;
        return false;
    }

    private static void MetalCaskPlacementPostfix(StardewValley.Object __instance, object[] __args, ref bool __result)
    {
        if (Instance == null || !__result || __instance?.QualifiedItemId != PowerConstants.Q(PowerConstants.MetalCaskId))
            return;

        if (__args.Length < 3
            || __args[0] is not GameLocation location
            || __args[1] is not int x
            || __args[2] is not int y)
        {
            return;
        }

        Vector2 tile = new(x / 64, y / 64);
        if (!location.objects.TryGetValue(tile, out StardewValley.Object? placedObject) || placedObject == null)
            return;

        location.objects[tile] = Instance.CreatePlacedMetalCask(tile, placedObject, location);
        Instance.PowerMgr.MarkDirty(location.NameOrUniqueName);
    }

    private static void MetalCaskLocationPostfix(Cask __instance, ref bool __result)
    {
        if (Instance == null || __instance == null || __result || !Instance.IsMetalCask(__instance))
            return;

        __result = Instance.IsValidMetalCaskLocation(__instance.Location);
    }

    private static void MetalCaskDayUpdatePostfix(Cask __instance)
    {
        if (Instance == null || __instance == null || !Instance.IsMetalCask(__instance))
            return;

        Instance.ApplyMetalCaskPowerBonus(__instance, __instance.Location);
    }

    private static bool SteamGeneratorDropInActionPrefix(StardewValley.Object __instance, Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed, ref bool __result)
    {
        if (Instance == null
            || __instance == null
            || dropInItem is not StardewValley.Object fuel
            || !IsFuelGeneratorItem(__instance.ItemId ?? "")
            || !IsSupportedGeneratorFuel(__instance.ItemId ?? "", fuel))
        {
            return true;
        }

        GameLocation? location = __instance.Location;
        if (location == null || StardewValley.Object.autoLoadFrom == null)
            return true;

        if (probe)
        {
            __result = Instance.CanAutoLoadGeneratorFuel(location, __instance);
            return false;
        }

        __result = Instance.TryAutoLoadGeneratorFuel(location, __instance, fuel);
        return false;
    }

    private static void GameLocationDrawFloorDecorationsPostfix(GameLocation __instance, SpriteBatch b)
    {
        if (Instance == null || !Context.IsWorldReady || __instance == null || b == null)
            return;

        Instance.DrawCables(b, __instance);
    }

    private static void CablePassablePostfix(StardewValley.Object __instance, ref bool __result)
    {
        string itemId = __instance?.ItemId ?? "";
        if (itemId == PowerConstants.CopperCableId
            || itemId == PowerConstants.IronCableId
            || itemId == PowerConstants.IridiumCableId)
        {
            __result = true;
        }
    }

    private static bool StatefulPowerGridTileDrawPrefix(StardewValley.Object __instance, object[] __args)
    {
        if (Instance == null || __instance == null || __args.Length < 4 || __args[0] is not SpriteBatch spriteBatch)
            return true;

        if (IsCableItem(__instance.ItemId))
            return false;

        if (!IsStatefulPowerGridItem(__instance.ItemId))
            return true;

        if (__args[1] is not int tileX || __args[2] is not int tileY)
            return true;

        float alpha = __args[3] is float drawAlpha ? drawAlpha : 1f;
        return !Instance.TryDrawStatefulTileReplacement(__instance, spriteBatch, tileX, tileY, alpha, "tile-prefix");
    }

    private static void StatefulObjectTileDrawPostfix(StardewValley.Object __instance, object[] __args)
    {
        if (Instance == null || __args.Length < 4 || __args[0] is not SpriteBatch spriteBatch)
            return;

        if (__instance == null || IsStatefulPowerGridItem(__instance.ItemId))
            return;

        if (__args[1] is not int tileX || __args[2] is not int tileY)
            return;

        float alpha = __args[3] is float drawAlpha ? drawAlpha : 1f;
        Instance.DrawStatefulObjectOverlayAtTile(__instance, spriteBatch, tileX, tileY, alpha);
    }

    private static void StatefulObjectScreenDrawPostfix(StardewValley.Object __instance, object[] __args)
    {
        if (Instance == null || __args.Length < 5 || __args[0] is not SpriteBatch spriteBatch)
            return;

        if (__args[1] is not int xNonTile || __args[2] is not int yNonTile)
            return;

        float? layerDepth = __args[3] is float explicitLayerDepth ? explicitLayerDepth : null;
        float alpha = __args[4] is float explicitAlpha ? explicitAlpha : 1f;
        Instance.LogConduitRenderDiagnostic(__instance, "screen-postfix", __args);
        Instance.DrawStatefulObjectOverlayAtScreen(__instance, spriteBatch, xNonTile, yNonTile, alpha, layerDepth);
    }

    private static void StatefulObjectAboveFrontLayerPostfix(StardewValley.Object __instance, object[] __args)
    {
        Instance?.LogConduitRenderDiagnostic(__instance, "above-front-postfix", __args);
    }

    private static bool StatefulObjectAboveFrontLayerPrefix(StardewValley.Object __instance, object[] __args)
    {
        if (__instance?.ItemId != PowerConstants.PowerConduitId)
            return true;

        Instance?.LogConduitRenderDiagnostic(__instance, "above-front-prefix-suppressed", __args);
        return false;
    }

    private void DrawStatefulObjectOverlayAtTile(StardewValley.Object obj, SpriteBatch spriteBatch, int tileX, int tileY, float alpha)
    {
        if (!Context.IsWorldReady || Game1.currentLocation == null || obj == null || !obj.bigCraftable.Value)
            return;

        if (Game1.currentLocation.getObjectAtTile(tileX, tileY) != obj)
            return;

        Vector2 tile = new(tileX, tileY);
        if (!TryGetStatefulSpriteName(obj, tile, out string? stateSpriteName))
            return;

        if (!TryLoadStateTextureForDraw(stateSpriteName!, out Texture2D? texture))
            return;

        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX * 64, tileY * 64 - 64));
        float layerDepth = GetStatefulObjectLayerDepth(obj, tileX, tileY);

        DrawStatefulTexture(spriteBatch, texture!, screenPos, alpha, layerDepth, obj, stateSpriteName!);
    }

    private void DrawStatefulObjectOverlayAtScreen(StardewValley.Object obj, SpriteBatch spriteBatch, int xNonTile, int yNonTile, float alpha, float? layerDepthOverride)
    {
        if (!Context.IsWorldReady || Game1.currentLocation == null || obj == null || !obj.bigCraftable.Value)
            return;

        Vector2 tile = obj.TileLocation;
        int tileX = (int)tile.X;
        int tileY = (int)tile.Y;

        if (Game1.currentLocation.getObjectAtTile(tileX, tileY) != obj)
            return;

        if (!TryGetStatefulSpriteName(obj, tile, out string? stateSpriteName))
            return;

        if (!TryLoadStateTextureForDraw(stateSpriteName!, out Texture2D? texture))
            return;

        Vector2 screenPos = new(xNonTile, yNonTile);
        float layerDepth = GetStatefulObjectLayerDepth(obj, tileX, tileY, layerDepthOverride);

        DrawStatefulTexture(spriteBatch, texture!, screenPos, alpha, layerDepth, obj, stateSpriteName!);
    }

    private void DrawStatefulTexture(SpriteBatch spriteBatch, Texture2D texture, Vector2 screenPos, float alpha, float layerDepth, StardewValley.Object obj, string stateSpriteName)
    {
        if (IsAnimatedStatefulBigCraftable(obj.ItemId))
        {
            DrawAnimatedBigCraftableTexture(spriteBatch, texture, screenPos, alpha, layerDepth, obj);
        }
        else
        {
            spriteBatch.Draw(
                texture,
                screenPos,
                BigCraftableSourceRect,
                Color.White * alpha,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                layerDepth);
        }

        DrawFuelGeneratorActiveOverlay(spriteBatch, screenPos, alpha, layerDepth, obj, stateSpriteName);
        DrawWindGeneratorWheelOverlay(spriteBatch, screenPos, alpha, layerDepth, obj, stateSpriteName);
        DrawStatefulReadyIndicator(spriteBatch, alpha, layerDepth, obj);
    }

    private static bool IsAnimatedStatefulBigCraftable(string? itemId)
    {
        return itemId == PowerConstants.IndustrialPreservesJarId
            || itemId == PowerConstants.MetalCaskId
            || itemId == PowerConstants.MetalKegId
            || itemId == PowerConstants.HardIridiumKegId;
    }

    private static bool UsesStatefulReadyIndicator(string? itemId)
    {
        return itemId == PowerConstants.IndustrialPreservesJarId
            || itemId == PowerConstants.MetalCaskId
            || itemId == PowerConstants.MetalKegId
            || itemId == PowerConstants.HardIridiumKegId;
    }

    private static void DrawAnimatedBigCraftableTexture(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Vector2 screenPos,
        float alpha,
        float layerDepth,
        StardewValley.Object obj)
    {
        Vector2 drawScale = obj.getScale() * 4f;
        int shakeX = obj.shakeTimer > 0 ? Game1.random.Next(-1, 2) : 0;
        int shakeY = obj.shakeTimer > 0 ? Game1.random.Next(-1, 2) : 0;

        Rectangle destination = new(
            (int)(screenPos.X - drawScale.X / 2f) + shakeX,
            (int)(screenPos.Y - drawScale.Y / 2f) + shakeY,
            Math.Max(1, (int)(64f + drawScale.X)),
            Math.Max(1, (int)(128f + drawScale.Y / 2f)));

        spriteBatch.Draw(
            texture,
            destination,
            BigCraftableSourceRect,
            Color.White * alpha,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            layerDepth);
    }

    private static void DrawStatefulReadyIndicator(SpriteBatch spriteBatch, float alpha, float layerDepth, StardewValley.Object obj)
    {
        if (!UsesStatefulReadyIndicator(obj.ItemId) || !obj.readyForHarvest.Value)
            return;

        Vector2 tile = obj.TileLocation;
        float bobOffset = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250d), 2);
        float bubbleLayerDepth = Math.Min(1f, layerDepth + 0.00001f);

        spriteBatch.Draw(
            Game1.mouseCursors,
            Game1.GlobalToLocal(
                Game1.viewport,
                new Vector2(tile.X * 64f - 8f, tile.Y * 64f - 112f + bobOffset)),
            new Rectangle(141, 465, 20, 24),
            Color.White * (0.75f * alpha),
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            bubbleLayerDepth);

        StardewValley.Object? heldObject = obj.heldObject.Value;
        if (heldObject == null)
            return;

        if (heldObject is ColoredObject coloredObject)
        {
            coloredObject.drawInMenu(
                spriteBatch,
                Game1.GlobalToLocal(
                    Game1.viewport,
                    new Vector2(tile.X * 64f, tile.Y * 64f - 104f + bobOffset)),
                1f,
                0.75f * alpha,
                Math.Min(1f, bubbleLayerDepth + 0.00001f));
            return;
        }

        var heldData = ItemRegistry.GetDataOrErrorItem(heldObject.QualifiedItemId);
        Texture2D heldTexture = heldData.GetTexture();
        Rectangle heldSource = heldData.GetSourceRect(0);
        Vector2 iconPosition = Game1.GlobalToLocal(
            Game1.viewport,
            new Vector2(tile.X * 64f + 32f, tile.Y * 64f - 72f + bobOffset));

        spriteBatch.Draw(
            heldTexture,
            iconPosition,
            heldSource,
            Color.White * (0.75f * alpha),
            0f,
            new Vector2(8f, 8f),
            4f,
            SpriteEffects.None,
            Math.Min(1f, bubbleLayerDepth + 0.00001f));
    }

    private void DrawFuelGeneratorActiveOverlay(SpriteBatch spriteBatch, Vector2 screenPos, float alpha, float layerDepth, StardewValley.Object obj, string stateSpriteName)
    {
        if (!IsFuelGeneratorItem(obj.ItemId ?? "") || !stateSpriteName.EndsWith("__on", StringComparison.Ordinal))
            return;

        double totalSeconds = Game1.currentGameTime?.TotalGameTime.TotalSeconds ?? 0d;
        float pulse = 0.55f + ((float)Math.Sin(totalSeconds * 7f) + 1f) * 0.225f;
        float drift = (float)Math.Sin(totalSeconds * 3.2f);
        float flicker = (float)Math.Sin(totalSeconds * 16f) * 0.6f;
        float overlayDepth = Math.Min(1f, layerDepth + SteamActiveOverlayLayerOffset);

        bool isCombustion = obj.ItemId == PowerConstants.CombustionGeneratorId;

        // Chamber glow anchored to the lit viewport on the generator face.
        Vector2 fireboxCenter = isCombustion
            ? new Vector2(screenPos.X + 24f + flicker, screenPos.Y + 88f)
            : new Vector2(screenPos.X + 22f + flicker, screenPos.Y + 90f);
        Color outerGlow = isCombustion
            ? new Color(72, 212, 132) * Math.Min(1f, alpha * (0.28f + pulse * 0.24f))
            : new Color(255, 160, 72) * Math.Min(1f, alpha * (0.32f + pulse * 0.28f));
        Color midGlow = isCombustion
            ? new Color(116, 240, 160) * Math.Min(1f, alpha * (0.48f + pulse * 0.22f))
            : new Color(255, 198, 96) * Math.Min(1f, alpha * (0.5f + pulse * 0.25f));
        Color coreGlow = isCombustion
            ? new Color(196, 255, 220) * Math.Min(1f, alpha * (0.7f + pulse * 0.18f))
            : new Color(255, 236, 156) * Math.Min(1f, alpha * (0.75f + pulse * 0.2f));

        Rectangle outerRect = new((int)fireboxCenter.X - 7, (int)fireboxCenter.Y - 4, 14, 8);
        Rectangle midRect = new((int)fireboxCenter.X - 5, (int)fireboxCenter.Y - 3, 10, 6);
        Rectangle coreRect = new((int)fireboxCenter.X - 2, (int)fireboxCenter.Y - 1, 5, 3);

        spriteBatch.Draw(Game1.staminaRect, outerRect, null, outerGlow, 0f, Vector2.Zero, SpriteEffects.None, overlayDepth);
        spriteBatch.Draw(Game1.staminaRect, midRect, null, midGlow, 0f, Vector2.Zero, SpriteEffects.None, overlayDepth);
        spriteBatch.Draw(Game1.staminaRect, coreRect, null, coreGlow, 0f, Vector2.Zero, SpriteEffects.None, overlayDepth);

        // Steam puffs near the stack to make active state readable at a glance.
        Color steamColor = new Color(224, 230, 236) * Math.Min(1f, alpha * (0.4f + pulse * 0.5f));
        float stackCenterX = screenPos.X + 22f;
        Rectangle puffA = new(
            (int)stackCenterX - 3,
            (int)(screenPos.Y + 18 - drift * 2f),
            7,
            4);
        Rectangle puffB = new(
            (int)stackCenterX - 3,
            (int)(screenPos.Y + 12 - drift * 3f),
            6,
            3);
        Rectangle puffCore = new(
            (int)stackCenterX - 1,
            (int)(screenPos.Y + 10 - drift * 2.2f),
            3,
            2);

        spriteBatch.Draw(
            Game1.staminaRect,
            puffA,
            null,
            steamColor,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            overlayDepth);

        spriteBatch.Draw(
            Game1.staminaRect,
            puffB,
            null,
            steamColor * 0.85f,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            overlayDepth);

        spriteBatch.Draw(
            Game1.staminaRect,
            puffCore,
            null,
            steamColor * 0.95f,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            overlayDepth);
    }

    private bool TryDrawStatefulTileReplacement(StardewValley.Object obj, SpriteBatch spriteBatch, int tileX, int tileY, float alpha, string source)
    {
        if (!Context.IsWorldReady || Game1.currentLocation == null || Game1.currentLocation.getObjectAtTile(tileX, tileY) != obj)
            return false;

        Vector2 tile = new(tileX, tileY);
        if (!TryGetStatefulSpriteName(obj, tile, out string? stateSpriteName))
            return false;

        if (!TryLoadStateTextureForDraw(stateSpriteName!, out Texture2D? texture))
            return false;

        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX * 64, tileY * 64 - 64));
        float layerDepth = GetStatefulObjectLayerDepth(obj, tileX, tileY);
        LogConduitRenderDiagnostic(obj, source, new object[] { tileX, tileY, alpha, stateSpriteName! });
        DrawStatefulTexture(spriteBatch, texture!, screenPos, alpha, layerDepth, obj, stateSpriteName!);
        return true;
    }

    private float GetStatefulObjectLayerDepth(StardewValley.Object obj, int tileX, int tileY, float? layerDepthOverride = null)
    {
        if (obj.ItemId == PowerConstants.PowerConduitId)
            return GetFloorMountedLayerDepth(tileX, tileY);

        return layerDepthOverride ?? GetBigCraftableLayerDepth(tileX, tileY, DefaultBigCraftableLayerDepthYOffset);
    }

    private static float GetBigCraftableLayerDepth(int tileX, int tileY, float yOffset)
    {
        return Math.Max(0f, ((tileY + 1) * 64f - yOffset) / 10000f + tileX / 1000000f);
    }

    private static float GetFloorMountedLayerDepth(int tileX, int tileY)
    {
        return Math.Max(0f, (tileY * 64f + FloorMountedLayerDepthInsetFromTileTop) / 10000f + tileX / 1000000f);
    }

    private void DrawWindGeneratorWheelOverlay(SpriteBatch spriteBatch, Vector2 screenPos, float alpha, float layerDepth, StardewValley.Object obj, string stateSpriteName)
    {
        if (obj.ItemId != PowerConstants.WindGeneratorId)
            return;

        if (!TryLoadWindGeneratorWheelTexture(out Texture2D? wheelTexture))
            return;

        bool generating = stateSpriteName.EndsWith("__generating", StringComparison.Ordinal);
        float angularSpeed = generating ? WindGeneratorGeneratingRadiansPerSecond : WindGeneratorIdleRadiansPerSecond;
        double totalSeconds = Game1.currentGameTime?.TotalGameTime.TotalSeconds ?? 0d;
        float rotation = (float)(totalSeconds * angularSpeed);

        Vector2 pivotScreenPos = screenPos + WindGeneratorWheelPivot * 4f;
        float wheelLayerDepth = Math.Min(1f, layerDepth + WindGeneratorWheelLayerOffset);

        spriteBatch.Draw(
            wheelTexture!,
            pivotScreenPos,
            BigCraftableSourceRect,
            Color.White * alpha,
            rotation,
            WindGeneratorWheelPivot,
            4f,
            SpriteEffects.None,
            wheelLayerDepth);
    }

    private bool TryLoadWindGeneratorWheelTexture(out Texture2D? texture)
    {
        texture = cachedWindWheelTexture;
        if (texture != null)
            return true;

        if (attemptedWindWheelTextureLoad)
            return false;

        attemptedWindWheelTextureLoad = true;

        string customPath = GetSpriteRelativePath(WindGeneratorWheelSpriteName);
        string fullPath = Path.Combine(Helper.DirectoryPath, customPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return false;

        try
        {
            cachedWindWheelTexture = Helper.GameContent.Load<Texture2D>(WindGeneratorWheelTexture);
            texture = cachedWindWheelTexture;
            return texture != null;
        }
        catch (Exception ex)
        {
            if (invalidStateSpritesLogged.Add(WindGeneratorWheelSpriteName))
            {
                Monitor.Log(
                    $"[PowerGrid] Failed to load optional wind wheel sprite '{customPath}'. Wind generator will render without wheel animation. {ex.Message}",
                    LogLevel.Warn);
            }

            return false;
        }
    }

    private static bool IsStatefulPowerGridItem(string? itemId)
    {
        return itemId == PowerConstants.SteamGeneratorId
            || itemId == PowerConstants.CombustionGeneratorId
            || itemId == PowerConstants.WindGeneratorId
            || itemId == PowerConstants.BasicBatteryId
            || itemId == PowerConstants.IridiumBatteryId
            || itemId == PowerConstants.PowerConduitId
            || itemId == PowerConstants.IndustrialPreservesJarId
            || itemId == PowerConstants.MetalCaskId
            || itemId == PowerConstants.MetalKegId
            || itemId == PowerConstants.HardIridiumKegId;
    }

    private static bool IsCableItem(string? itemId)
    {
        return itemId == PowerConstants.CopperCableId
            || itemId == PowerConstants.IronCableId
            || itemId == PowerConstants.IridiumCableId;
    }

    private bool TryGetStatefulSpriteName(StardewValley.Object obj, Vector2 tile, out string? stateSpriteName)
    {
        stateSpriteName = null;

        string itemId = obj.ItemId ?? "";
        string locationName = Game1.currentLocation?.NameOrUniqueName ?? "";

        string? stateName = itemId switch
        {
            PowerConstants.SteamGeneratorId => GetSteamGeneratorState(locationName, tile),
            PowerConstants.CombustionGeneratorId => GetCombustionGeneratorState(locationName, tile),
            PowerConstants.WindGeneratorId => GetWindGeneratorState(obj),
            PowerConstants.BasicBatteryId => GetBatteryState(locationName, tile, itemId),
            PowerConstants.IridiumBatteryId => GetBatteryState(locationName, tile, itemId),
            PowerConstants.PowerConduitId => GetConduitState(locationName, tile),
            PowerConstants.IndustrialPreservesJarId => GetIndustrialPreservesJarState(obj),
            PowerConstants.MetalCaskId => GetPoweredMachineState(obj),
            PowerConstants.MetalKegId => GetPoweredMachineState(obj),
            PowerConstants.HardIridiumKegId => GetPoweredMachineState(obj),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(stateName))
            return false;

        string? baseSpriteName = GetBaseSpriteName(itemId);
        if (string.IsNullOrWhiteSpace(baseSpriteName))
            return false;

        stateSpriteName = GetStateSpriteName(baseSpriteName, stateName);
        return true;
    }

    private bool TryLoadStateTextureForDraw(string stateSpriteName, out Texture2D? texture)
    {
        texture = null;

        string customPath = GetSpriteRelativePath(stateSpriteName);
        string fullPath = Path.Combine(Helper.DirectoryPath, customPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return false;

        try
        {
            texture = Helper.GameContent.Load<Texture2D>(GetTextureAsset(stateSpriteName));
            if (stateSpriteName.StartsWith("PowerConduit__", StringComparison.Ordinal))
            {
                LogConduitDiagnosticOnce(
                    $"draw-load|{stateSpriteName}",
                    $"[PowerGrid] Conduit state texture loaded for draw: assetKey={GetTextureAsset(stateSpriteName)}, path={customPath}, size={texture.Width}x{texture.Height}");
            }
            return true;
        }
        catch (Exception ex)
        {
            if (invalidStateSpritesLogged.Add(stateSpriteName))
            {
                Monitor.Log(
                    $"[PowerGrid] Failed to load state sprite '{customPath}'. Falling back to the base sprite. {ex.Message}",
                    LogLevel.Warn);
            }

            return false;
        }
    }

    private string? GetSteamGeneratorState(string locationName, Vector2 tile)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return null;

        return GetFueledGeneratorState(locationName, tile, PowerConstants.SteamGeneratorId);
    }

    private string? GetCombustionGeneratorState(string locationName, Vector2 tile)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return null;

        return GetFueledGeneratorState(locationName, tile, PowerConstants.CombustionGeneratorId);
    }

    private string GetFueledGeneratorState(string locationName, Vector2 tile, string itemId)
    {
        string generatorKey = PowerConstants.MakeNodeKey(locationName, tile, itemId);
        return FuelMgr.GetFuelTicksRemaining(generatorKey) > 0 ? "on" : "off";
    }

    private StardewValley.Object CreatePlacedMetalCask(Vector2 tile, StardewValley.Object placedObject, GameLocation location)
    {
        Cask metalCask = new(tile);
        ApplyMetalCaskIdentity(metalCask, placedObject);

        metalCask.modData.Clear();
        foreach (KeyValuePair<string, string> pair in placedObject.modData.Pairs)
            metalCask.modData[pair.Key] = pair.Value;

        metalCask.modData[MetalCaskMarkerKey] = "true";
        UpdateMetalCaskPowerTelemetry(metalCask, location, observedSpeedup: 0f, lastAppliedBonusDays: null);

        return metalCask;
    }

    private void ApplyMetalCaskIdentity(Cask cask, StardewValley.Object? sourceObject = null)
    {
        StardewValley.Object identityTemplate = sourceObject ?? ItemRegistry.Create<StardewValley.Object>(PowerConstants.Q(PowerConstants.MetalCaskId));

        cask.ItemId = PowerConstants.MetalCaskId;
        cask.Name = identityTemplate.Name;
        cask.Price = identityTemplate.Price;
        cask.Type = identityTemplate.Type;
        cask.Category = identityTemplate.Category;
        cask.bigCraftable.Value = identityTemplate.bigCraftable.Value;
        cask.CanBeSetDown = identityTemplate.CanBeSetDown;
        cask.CanBeGrabbed = identityTemplate.CanBeGrabbed;
        cask.setOutdoors.Value = identityTemplate.setOutdoors.Value;
        cask.setIndoors.Value = identityTemplate.setIndoors.Value;
        cask.Fragility = identityTemplate.Fragility;
        cask.isLamp.Value = identityTemplate.isLamp.Value;
        cask.ParentSheetIndex = identityTemplate.ParentSheetIndex;
        ResetParentSheetIndexMethod?.Invoke(cask, Array.Empty<object>());
        cask.modData[MetalCaskMarkerKey] = "true";
    }

    private void RehydrateMetalCasks()
    {
        foreach (GameLocation location in EnumerateLoadedLocations())
            RehydrateMetalCasks(location);
    }

    private void RehydrateMetalCasks(GameLocation location)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.objects.Pairs)
        {
            if (pair.Value is not Cask cask || !IsMetalCask(cask))
                continue;

            if (cask.QualifiedItemId == PowerConstants.Q(PowerConstants.MetalCaskId))
                continue;

            ApplyMetalCaskIdentity(cask);
        }
    }

    private void RefreshMetalCaskTelemetry()
    {
        foreach (GameLocation location in EnumerateLoadedLocations())
            RefreshMetalCaskTelemetry(location);
    }

    private void RefreshMetalCaskTelemetry(GameLocation location)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.objects.Pairs)
        {
            if (pair.Value is not Cask cask || !IsMetalCask(cask))
                continue;

            float observedSpeedup = GetObservedSpeedup(location, cask);
            UpdateMetalCaskPowerTelemetry(cask, location, observedSpeedup, lastAppliedBonusDays: null);
        }
    }

    private bool IsValidMetalCaskLocation(GameLocation location)
    {
        if (location == null || location.IsOutdoors)
            return false;

        return location is Cellar
            || location is FarmHouse
            || location is Cabin
            || location is Shed
            || location is AnimalHouse
            || location is SlimeHutch
            || string.Equals(location.NameOrUniqueName, "Greenhouse", StringComparison.OrdinalIgnoreCase)
            || string.Equals(location.NameOrUniqueName, "IslandFarmHouse", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyMetalCaskPowerBonus(Cask cask, GameLocation? location)
    {
        float observedSpeedup = 0f;
        float? appliedBonusDays = null;

        if (location != null
            && cask.heldObject.Value != null
            && TryGetMetalCaskDaysToMature(cask, out float currentDays)
            && currentDays > 0f)
        {
            float configuredMaxSpeedup = ClampSpeedup(Config.MetalCaskMaxSpeedup);
            if (configuredMaxSpeedup > 0f)
            {
                observedSpeedup = GetObservedSpeedup(location, cask);
                if (observedSpeedup > 0f)
                {
                    float normalizedPower = Math.Clamp(observedSpeedup / configuredMaxSpeedup, 0f, 1f);
                    float bonusDays = normalizedPower * MetalCaskPoweredBonusDaysAtFullPower;
                    if (bonusDays > 0f && CaskDaysToMatureField?.GetValue(cask) is NetFloat daysToMature)
                    {
                        daysToMature.Value = Math.Max(0f, daysToMature.Value - bonusDays);
                        CaskCheckForMaturityMethod?.Invoke(cask, Array.Empty<object>());
                        appliedBonusDays = bonusDays;
                    }
                }
            }
        }

        UpdateMetalCaskPowerTelemetry(cask, location, observedSpeedup, appliedBonusDays);
    }

    private float GetObservedSpeedup(GameLocation location, Cask cask)
    {
        AllocationResult? allocation = PowerMgr.GetLastAllocationAtTile(location.NameOrUniqueName, cask.TileLocation);
        return allocation?.SpeedupFraction ?? 0f;
    }

    private void UpdateMetalCaskPowerTelemetry(Cask cask, GameLocation? location, float observedSpeedup, float? lastAppliedBonusDays)
    {
        cask.modData[MetalCaskPowerModeKey] = "days";

        if (lastAppliedBonusDays.HasValue && lastAppliedBonusDays.Value > 0f)
        {
            cask.modData[MetalCaskLastAppliedBonusDaysKey] = lastAppliedBonusDays.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            cask.modData[MetalCaskLastAppliedDateKey] = BuildCurrentGameDateLabel();
        }

        cask.modData[MetalCaskObservedSpeedupKey] = observedSpeedup.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

        if (TryGetMetalCaskDaysToMature(cask, out float daysRemaining))
            cask.modData[MetalCaskDaysToNextQualityKey] = daysRemaining.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        else
            cask.modData.Remove(MetalCaskDaysToNextQualityKey);

        string state = cask.heldObject.Value == null
            ? "empty"
            : !TryGetMetalCaskDaysToMature(cask, out daysRemaining)
                ? "aging"
                : daysRemaining <= 0f
                    ? "ready"
                    : observedSpeedup > 0f
                        ? "powered"
                        : "unpowered";

        cask.modData[MetalCaskObservedPowerStateKey] = state;
    }

    private static bool TryGetMetalCaskDaysToMature(Cask cask, out float daysRemaining)
    {
        daysRemaining = 0f;
        if (CaskDaysToMatureField?.GetValue(cask) is not NetFloat netFloat)
            return false;

        daysRemaining = netFloat.Value;
        return true;
    }

    private static string BuildCurrentGameDateLabel()
    {
        if (!Context.IsWorldReady)
            return "unknown";

        return $"Year {Game1.year} {Game1.currentSeason} {Game1.dayOfMonth}";
    }

    private bool IsMetalCask(Cask cask)
    {
        return cask.QualifiedItemId == PowerConstants.Q(PowerConstants.MetalCaskId)
            || (cask.modData?.ContainsKey(MetalCaskMarkerKey) ?? false);
    }

    private static string? GetWindGeneratorState(StardewValley.Object obj)
    {
        if (obj.modData.TryGetValue(RuntimeGeneratedThisTickKey, out string? generatedText)
            && int.TryParse(generatedText, out int generatedThisTick))
        {
            return generatedThisTick > 0 ? "generating" : "idle";
        }

        if (obj.modData.TryGetValue(RuntimeOnlineKey, out string? onlineText))
            return onlineText == "1" ? "generating" : "idle";

        // Default to idle so wind visuals (including wheel layer) are present as soon as placed.
        return "idle";
    }

    private void TriggerFueledGeneratorWorkingAnimations()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        foreach (GameLocation location in EnumerateLoadedLocations())
        {
            if (location.farmers.Count == 0)
                continue;

            foreach ((Vector2 tile, StardewValley.Object obj) in location.objects.Pairs)
            {
                if (!IsFuelGeneratorItem(obj.ItemId ?? ""))
                    continue;

                string generatorKey = PowerConstants.MakeNodeKey(location.NameOrUniqueName, tile, obj.ItemId ?? "");
                if (FuelMgr.GetFuelTicksRemaining(generatorKey) <= 0)
                    continue;

                // Keep a guaranteed visible active cue even when machine-data working effects are unavailable.
                obj.shakeTimer = Math.Max(obj.shakeTimer, 120);
                obj.addWorkingAnimation();
            }
        }
    }

    private void PruneStaleGeneratorFuelState()
    {
        var validKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (GameLocation location in EnumerateLoadedLocations())
        {
            string locationName = location.NameOrUniqueName;
            foreach ((Vector2 tile, StardewValley.Object obj) in location.objects.Pairs)
            {
                if (!IsFuelGeneratorItem(obj.ItemId ?? ""))
                    continue;

                validKeys.Add(PowerConstants.MakeNodeKey(locationName, tile, obj.ItemId ?? ""));
            }
        }

        int removed = FuelMgr.PruneToKeys(validKeys);
        if (removed > 0)
            Monitor.Log($"[PowerGrid] Pruned {removed} stale fueled-generator state entr{(removed == 1 ? "y" : "ies")}.", LogLevel.Trace);
    }

    private void PruneStaleConduitLinks()
    {
        var validEndpoints = new HashSet<string>(StringComparer.Ordinal);
        foreach (GameLocation location in EnumerateLoadedLocations())
        {
            string locationName = location.NameOrUniqueName;
            foreach ((Vector2 tile, StardewValley.Object obj) in location.objects.Pairs)
            {
                if (obj.ItemId != PowerConstants.PowerConduitId)
                    continue;

                validEndpoints.Add(MakeConduitEndpointKey(locationName, tile));
            }
        }

        int removed = ConduitMgr.PruneToEndpoints(validEndpoints);
        if (removed > 0)
            Monitor.Log($"[PowerGrid] Pruned {removed} stale conduit link(s).", LogLevel.Trace);
    }

    private void HandleRemovedGeneratorFuelState(GameLocation location, Vector2 tile, StardewValley.Object removedObj)
    {
        if (!IsFuelGeneratorItem(removedObj.ItemId ?? "") && removedObj.ItemId != PowerConstants.WindGeneratorId)
            return;

        string itemId = removedObj.ItemId ?? "";
        string key = PowerConstants.MakeNodeKey(location.NameOrUniqueName, tile, itemId);
        if (!FuelMgr.TryRemoveFuelState(key, out int removedTicks) || removedTicks <= 0)
            return;

        if (IsFuelGeneratorItem(itemId))
            DropFuelFromTicks(location, tile, itemId, removedTicks);
    }

    private bool HandleRemovedConduitState(GameLocation location, Vector2 tile, StardewValley.Object removedObj)
    {
        if (removedObj.ItemId != PowerConstants.PowerConduitId)
            return false;

        bool removed = ConduitMgr.RemoveConduitState(location.NameOrUniqueName, tile);
        if (removed)
            Monitor.Log($"[PowerGrid] Removed conduit link state for broken conduit at {location.NameOrUniqueName} ({tile.X},{tile.Y}).", LogLevel.Trace);

        return removed;
    }

    private void DropFuelFromTicks(GameLocation location, Vector2 tile, string generatorItemId, int ticks)
    {
        string qualifiedFuelId;
        int ticksPerUnit;
        switch (generatorItemId)
        {
            case PowerConstants.SteamGeneratorId:
                qualifiedFuelId = "(O)382";
                ticksPerUnit = Math.Max(1, Config.CoalFuelTicks);
                break;
            case PowerConstants.CombustionGeneratorId:
                qualifiedFuelId = PowerConstants.QObject(PowerConstants.BiofuelId);
                ticksPerUnit = Math.Max(1, Config.BiofuelFuelTicks);
                break;
            default:
                return;
        }

        int unitsToDrop = (ticks + ticksPerUnit - 1) / ticksPerUnit;
        if (unitsToDrop <= 0)
            return;

        Vector2 dropPos = new(tile.X * 64f + 32f, tile.Y * 64f + 32f);
        while (unitsToDrop > 0)
        {
            int stack = Math.Min(unitsToDrop, 999);
            Game1.createItemDebris(ItemRegistry.Create(qualifiedFuelId, stack), dropPos, -1, location);
            unitsToDrop -= stack;
        }
    }

    private string? GetBatteryState(string locationName, Vector2 tile, string itemId)
    {
        int capacity = GetBatteryCapacity(itemId);
        if (string.IsNullOrWhiteSpace(locationName) || capacity <= 0)
            return null;

        var batteryNode = new PowerNode
        {
            NodeType = PowerNodeType.Battery,
            LocationName = locationName,
            Tile = tile,
            ItemId = itemId,
            Capacity = capacity
        };

        float chargePercent = capacity > 0
            ? (float)BatteryState.GetCharge(batteryNode) / capacity
            : 0f;

        return chargePercent >= ChargedBatteryThreshold ? "charged" : "low";
    }

    private string GetConduitState(string locationName, Vector2 tile)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return "unpaired";

        return ConduitMgr.GetPartner(locationName, tile) != null ? "linked" : "unpaired";
    }

    private static string GetIndustrialPreservesJarState(StardewValley.Object obj)
    {
        return GetPoweredMachineState(obj);
    }

    private static string GetPoweredMachineState(StardewValley.Object obj)
    {
        bool hasEnergized = TryReadPowerModDataString(obj.modData, "energized", out string? energizedText);
        bool hasPowered = TryReadPowerModDataString(obj.modData, "powered", out string? poweredText);
        bool isPowered = hasEnergized
            ? energizedText == "1"
            : hasPowered && poweredText == "1";

        return isPowered ? IndustrialPreservesJarPoweredState : IndustrialPreservesJarUnpoweredState;
    }

    private int GetBatteryCapacity(string itemId)
    {
        return itemId switch
        {
            PowerConstants.BasicBatteryId => Config.BasicBatteryCapacity,
            PowerConstants.IridiumBatteryId => Config.IridiumBatteryCapacity,
            _ => 0
        };
    }

    private string? GetBaseSpriteName(string itemId)
    {
        return itemId switch
        {
            PowerConstants.SteamGeneratorId => "SteamGenerator",
            PowerConstants.CombustionGeneratorId => "CombustionGenerator",
            PowerConstants.WindGeneratorId => "WindGenerator",
            PowerConstants.BasicBatteryId => "BasicBattery",
            PowerConstants.IridiumBatteryId => "IridiumBattery",
            PowerConstants.PowerConduitId => "PowerConduit",
            PowerConstants.IndustrialPreservesJarId => IndustrialPreservesJarSpriteName,
            PowerConstants.MetalCaskId => MetalCaskSpriteName,
            PowerConstants.MetalKegId => MetalKegSpriteName,
            PowerConstants.HardIridiumKegId => HardIridiumKegSpriteName,
            _ => null
        };
    }

    private string GetTextureAsset(string spriteName)
    {
        return PowerConstants.TextureAsset(ModManifest.UniqueID, spriteName);
    }

    private string GetSpriteRelativePath(string spriteName)
    {
        return $"{SpriteDirectoryName}/{spriteName}.png";
    }

    private string GetSpriteDiskPath(string spriteName)
    {
        return Path.Combine(Helper.DirectoryPath, SpriteDirectoryName, $"{spriteName}.png");
    }

    private static string GetStateSpriteName(string baseSpriteName, string stateName)
    {
        return $"{baseSpriteName}__{stateName}";
    }

    private void ValidateCriticalSpritePaths()
    {
        int missingCount = 0;
        foreach (string spriteName in RequiredSpriteNames)
        {
            string expectedPath = GetSpriteDiskPath(spriteName);
            if (File.Exists(expectedPath))
                continue;

            missingCount++;
            string legacyPath = Path.Combine(Helper.DirectoryPath, "assets", $"{spriteName}.png");
            bool caseMismatchLikely = !string.Equals(SpriteDirectoryName, "assets", StringComparison.Ordinal)
                && File.Exists(legacyPath);

            if (caseMismatchLikely)
            {
                Monitor.Log(
                    $"[PowerGrid] Critical asset case mismatch: expected '{expectedPath}' but found '{legacyPath}'. Linux/macOS are case-sensitive; keep sprite directory casing consistent.",
                    LogLevel.Error);
            }
            else
            {
                Monitor.Log($"[PowerGrid] Missing critical sprite asset: {expectedPath}", LogLevel.Error);
            }
        }

        if (missingCount == 0)
        {
            Monitor.Log(
                $"[PowerGrid] Critical sprite path check passed for {RequiredSpriteNames.Length} assets under '{SpriteDirectoryName}/'.",
                LogLevel.Trace);
        }
    }

    private void LogConduitStateSpriteAvailability()
    {
        string linkedPath = GetSpriteDiskPath("PowerConduit__linked");
        string unpairedPath = GetSpriteDiskPath("PowerConduit__unpaired");
        LogConduitDiagnosticOnce(
            "startup-assets",
            $"[PowerGrid] Conduit state sprite availability: linkedExists={File.Exists(linkedPath)} ({linkedPath}), unpairedExists={File.Exists(unpairedPath)} ({unpairedPath})");
    }

    private void LogConduitRenderDiagnostic(StardewValley.Object obj, string pathName, object[] args)
    {
        if (obj?.ItemId != PowerConstants.PowerConduitId || Game1.currentLocation == null)
            return;

        Vector2 tile = obj.TileLocation;
        string locationName = Game1.currentLocation.NameOrUniqueName;
        string stateName = GetConduitState(locationName, tile);
        string stateSpriteName = GetStateSpriteName("PowerConduit", stateName);
        string argsSummary = string.Join(", ", args.Select(arg => arg == null ? "null" : $"{arg.GetType().Name}:{arg}"));

        LogConduitDiagnosticOnce(
            $"render|{pathName}|{locationName}|{tile.X}|{tile.Y}|{stateName}",
            $"[PowerGrid] Conduit render path: path={pathName}, location={locationName}, tile=({tile.X},{tile.Y}), state={stateName}, stateSprite={stateSpriteName}, args=[{argsSummary}]");
    }

    private void LogConduitDiagnosticOnce(string key, string message)
    {
        if (conduitRenderDiagnosticsLogged.Add(key))
            Monitor.Log(message, LogLevel.Info);
    }

    private static void TrySetPassableFlags(BigCraftableData data)
    {
        SetBoolMemberIfExists(data, "CanBePassable", true);
        SetBoolMemberIfExists(data, "IsPassable", true);
        SetBoolMemberIfExists(data, "CanPassThrough", true);
        SetBoolMemberIfExists(data, "Passable", true);
        SetBoolMemberIfExists(data, "CanWalkThrough", true);
    }

    private static void SetBoolMemberIfExists(BigCraftableData data, string memberName, bool value)
    {
        PropertyInfo? property = typeof(BigCraftableData).GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (property?.CanWrite == true && property.PropertyType == typeof(bool))
        {
            property.SetValue(data, value);
            return;
        }

        FieldInfo? field = typeof(BigCraftableData).GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(bool))
            field.SetValue(data, value);
    }

    private bool TryInsertFuelIntoGenerator(GameLocation location, StardewValley.Object generator)
    {
        StardewValley.Object? activeFuel = Game1.player.ActiveObject;
        string generatorItemId = generator.ItemId ?? "";
        if (activeFuel == null || !IsSupportedGeneratorFuel(generatorItemId, activeFuel))
            return false;

        if (!TryAddGeneratorFuel(location, generator, activeFuel, 1, out int ticksAdded))
            return false;

        Game1.player.reduceActiveItemByOne();
        Game1.playSound("Ship");
        PowerMgr.MarkDirty(location.NameOrUniqueName);

        int minutesAdded = ticksAdded * PowerConstants.TickIntervalMinutes;
        Game1.addHUDMessage(new HUDMessage($"+{minutesAdded}m generator fuel", HUDMessage.newQuest_type));
        return true;
    }

    private bool CanAutoLoadGeneratorFuel(GameLocation location, StardewValley.Object generator)
    {
        string generatorKey = PowerConstants.MakeNodeKey(location.NameOrUniqueName, generator.TileLocation, generator.ItemId ?? "");
        return FuelMgr.GetFuelTicksRemaining(generatorKey) <= 0;
    }

    private bool TryAutoLoadGeneratorFuel(GameLocation location, StardewValley.Object generator, StardewValley.Object fuel)
    {
        if (!CanAutoLoadGeneratorFuel(location, generator))
            return false;

        if (!TryAddGeneratorFuel(location, generator, fuel, 1, out _))
            return false;

        ConsumeAutoLoadedItem(fuel);
        PowerMgr.MarkDirty(location.NameOrUniqueName);
        return true;
    }

    private bool TryAddGeneratorFuel(GameLocation location, StardewValley.Object generator, StardewValley.Object fuelItem, int amount, out int ticksAdded)
    {
        ticksAdded = 0;
        if (amount <= 0)
            return false;

        string generatorItemId = generator.ItemId ?? "";
        string generatorKey = PowerConstants.MakeNodeKey(location.NameOrUniqueName, generator.TileLocation, generatorItemId);
        return FuelMgr.TryAddFuel(generatorKey, generatorItemId, fuelItem, amount, out ticksAdded);
    }

    private static void ConsumeAutoLoadedItem(Item item)
    {
        StardewValley.Inventories.IInventory? inventory = StardewValley.Object.autoLoadFrom;
        if (inventory == null)
            return;

        for (int i = 0; i < inventory.Count; i++)
        {
            if (!ReferenceEquals(inventory[i], item))
                continue;

            if (item.Stack > 1)
                item.Stack -= 1;
            else
                inventory[i] = null;

            return;
        }
    }

    private bool TryMigrateLegacySteamFuel(GameLocation location, StardewValley.Object generator)
    {
        StardewValley.Object? heldFuel = generator.heldObject.Value;
        if (heldFuel == null || heldFuel.Stack <= 0 || !IsSupportedGeneratorFuel(PowerConstants.SteamGeneratorId, heldFuel, allowLegacyBatteryPack: true))
            return false;

        string generatorKey = PowerConstants.MakeNodeKey(location.NameOrUniqueName, generator.TileLocation, generator.ItemId ?? "");
        if (!FuelMgr.TryAddFuel(generatorKey, PowerConstants.SteamGeneratorId, heldFuel, heldFuel.Stack, out int ticksAdded))
            return false;

        generator.heldObject.Value = null;
        int minutesAdded = ticksAdded * PowerConstants.TickIntervalMinutes;
        Game1.addHUDMessage(new HUDMessage($"Migrated +{minutesAdded}m legacy fuel", HUDMessage.newQuest_type));
        return true;
    }

    private static int TimeDiffMinutes(int oldTime, int newTime)
    {
        int oldHour = oldTime / 100;
        int oldMin = oldTime % 100;
        int newHour = newTime / 100;
        int newMin = newTime % 100;
        return (newHour - oldHour) * 60 + (newMin - oldMin);
    }

    private static T CloneJson<T>(T obj)
    {
        string json = JsonSerializer.Serialize(obj, JsonOpts);
        return JsonSerializer.Deserialize<T>(json, JsonOpts)
            ?? throw new InvalidOperationException($"Failed to clone {typeof(T).FullName}");
    }
}
