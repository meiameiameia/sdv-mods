using System.Globalization;
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
    private const string RadioisotopeFuelRecipeKey = "Radioisotope Fuel";
    private const string HeatingCoilRecipeKey = "Heating Coil";
    private const string EfficiencyCoreRecipeKey = "Efficiency Core";
    private const string CatalystChamberRecipeKey = "Catalyst Chamber";
    private const string SortingMagnetRecipeKey = "Sorting Magnet";
    private const string DryingRackArrayRecipeKey = "Drying Rack Array";
    private const string HeatRegulatorRecipeKey = "Heat Regulator";
    private const string CombustionGeneratorRecipeKey = "Combustion Generator";
    private const string RadioisotopeGeneratorRecipeKey = "Radioisotope Generator";
    private const string IndustrialPreservesJarRecipeKey = "Industrial Preserves Jar";
    private const string MetalCaskRecipeKey = "Metal Cask";
    private const string MetalKegRecipeKey = "Metal Keg";
    private const string HardIridiumKegRecipeKey = "Hard Iridium Keg";
    private const string ElectricSmelterRecipeKey = "Electric Smelter";
    private const string IndustrialRecyclerRecipeKey = "Industrial Recycler";
    private const string PoweredDehydratorRecipeKey = "Powered Dehydrator";
    private const string IndustrialPreservesJarSpriteName = "IndustrialPreservesJar";
    private const string MetalCaskSpriteName = "MetalCask";
    private const string MetalKegSpriteName = "MetalKeg";
    private const string HardIridiumKegSpriteName = "HardIridiumKeg";
    private const string ElectricSmelterSpriteName = "ElectricSmelter";
    private const string ElectricSmelterOfflineState = "offline";
    private const string ElectricSmelterStandbyState = "standby";
    private const string ElectricSmelterProcessingPoweredState = "processing_powered";
    private const string HeatingCoilSpriteName = "HeatingCoil";
    private const string EfficiencyCoreSpriteName = "EfficiencyCore";
    private const string CatalystChamberSpriteName = "CatalystChamber";
    private const string IndustrialRecyclerSpriteName = "IndustrialRecycler";
    private const string SortingMagnetSpriteName = "SortingMagnet";
    private const string DryingRackArraySpriteName = "DryingRackArray";
    private const string HeatRegulatorSpriteName = "HeatRegulator";
    private const string PoweredDehydratorSpriteName = "PoweredDehydrator";
    private const string IndustrialPreservesJarPoweredState = "powered";
    private const string IndustrialPreservesJarUnpoweredState = "unpowered";
    private const string GofHardwoodKegId = "GOF_Hardwood_Keg";
    private const float MetalCaskBaseAgingMultiplier = 0.50f;
    private const float MetalCaskPoweredBonusDaysAtFullPower = 1.50f;
    private const int ElectricSmelterDemandPerTick = 40;
    private const float ElectricSmelterMaxSpeedup = 0.50f;
    private const int ElectricSmelterPriority = 10;
    private const int ElectricSmelterUpgradeSlots = 1;
    private const float ElectricSmelterBonusOutputChance = 0.05f;
    private const float ElectricSmelterCatalystBonusOutputChance = 0.15f;
    private const float ElectricSmelterHeatingCoilSpeedMultiplier = 0.65f;
    private const int IndustrialRecyclerDemandPerTick = 20;
    private const float IndustrialRecyclerMaxSpeedup = 0.35f;
    private const int IndustrialRecyclerPriority = 20;
    private const int IndustrialRecyclerUpgradeSlots = 1;
    private const int IndustrialRecyclerBaseMinutes = 60;
    private const int PoweredDehydratorDemandPerTick = 20;
    private const float PoweredDehydratorMaxSpeedup = 0.40f;
    private const int PoweredDehydratorPriority = 20;
    private const int PoweredDehydratorUpgradeSlots = 1;
    private const float PoweredDehydratorHeatRegulatorSpeedMultiplier = 0.65f;
    private const double PoweredDehydratorDryingRackExtraOutputChance = 0.15;
    private const string MetalCaskMarkerKey = PersistedModDataPrefix + "MetalCask";
    private const string MetalCaskPowerModeKey = PersistedModDataPrefix + "MetalCaskPowerMode";
    private const string MetalCaskObservedPowerStateKey = PersistedModDataPrefix + "MetalCaskObservedPowerState";
    private const string MetalCaskObservedSpeedupKey = PersistedModDataPrefix + "MetalCaskObservedSpeedupFraction";
    private const string MetalCaskDaysToNextQualityKey = PersistedModDataPrefix + "MetalCaskDaysToNextQuality";
    private const string MetalCaskLastAppliedBonusDaysKey = PersistedModDataPrefix + "MetalCaskLastAppliedBonusDays";
    private const string MetalCaskLastAppliedDateKey = PersistedModDataPrefix + "MetalCaskLastAppliedDate";
    private const string ElectricSmelterInputItemKey = PersistedModDataPrefix + "ElectricSmelterInputItem";
    private const string ElectricSmelterInputCountKey = PersistedModDataPrefix + "ElectricSmelterInputCount";
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
    private static readonly string[] ElectricSmelterUpgradeIds =
    {
        PowerConstants.HeatingCoilId,
        PowerConstants.EfficiencyCoreId,
        PowerConstants.CatalystChamberId
    };
    private static readonly string[] IndustrialRecyclerUpgradeIds =
    {
        PowerConstants.SortingMagnetId,
        PowerConstants.EfficiencyCoreId
    };
    private static readonly string[] PoweredDehydratorUpgradeIds =
    {
        PowerConstants.HeatRegulatorId,
        PowerConstants.EfficiencyCoreId,
        PowerConstants.DryingRackArrayId
    };
    private static readonly Vector2 WindGeneratorWheelPivot = new(8f, 6f);
    private static readonly FieldInfo? CaskDaysToMatureField = AccessTools.Field(typeof(Cask), "daysToMature");
    private static readonly MethodInfo? CaskCheckForMaturityMethod = AccessTools.Method(typeof(Cask), "checkForMaturity");
    private static readonly MethodInfo? ResetParentSheetIndexMethod = AccessTools.Method(typeof(StardewValley.Object), "ResetParentSheetIndex");
    private static readonly MethodInfo? PerformObjectDropInActionMethod = AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performObjectDropInAction));
    private static readonly MethodInfo? PerformToolActionMethod = AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performToolAction), new[] { typeof(Tool) });
    private static readonly MethodInfo? PerformRemoveActionMethod = AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performRemoveAction));
    private static readonly MethodInfo? DropObjectMethod = AccessTools.Method(typeof(GameLocation), nameof(GameLocation.dropObject), new[] { typeof(StardewValley.Object), typeof(Vector2), typeof(xTile.Dimensions.Rectangle), typeof(bool), typeof(Farmer) });
    private readonly HashSet<string> invalidStateSpritesLogged = new(StringComparer.Ordinal);
    private readonly HashSet<string> conduitRenderDiagnosticsLogged = new(StringComparer.Ordinal);
    private readonly List<PendingBatteryCharge> pendingBatteryCharges = new();
    private readonly List<PendingMachineUpgradeData> pendingMachineUpgrades = new();
    private static readonly string[] RequiredSpriteNames =
    {
        "CopperCable",
        "IronCable",
        "IridiumCable",
        "EnergizedIridiumCable",
        "Biofuel",
        "RadioisotopeFuel",
        "SteamGenerator",
        "SteamGenerator__off",
        "SteamGenerator__on",
        "CombustionGenerator",
        "CombustionGenerator__off",
        "CombustionGenerator__on",
        "RadioisotopeGenerator",
        "RadioisotopeGenerator__off",
        "RadioisotopeGenerator__on",
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
        "HardIridiumKeg__unpowered",
        "ElectricSmelter",
        "ElectricSmelter__offline",
        "ElectricSmelter__standby",
        "ElectricSmelter__processing_powered",
        "IndustrialRecycler",
        "IndustrialRecycler__powered",
        "IndustrialRecycler__unpowered",
        "PoweredDehydrator",
        "PoweredDehydrator__powered",
        "PoweredDehydrator__unpowered"
    };
    private string? vanillaPreservesJarBigCraftableId;
    private string? vanillaCaskBigCraftableId;
    private string? vanillaKegBigCraftableId;
    private string? vanillaDehydratorBigCraftableId;
    private string? hardwoodKegBigCraftableId;
    private bool loggedMissingPreservesTemplate;
    private bool loggedMissingPreservesMachineTemplate;
    private bool loggedMissingCaskTemplate;
    private bool loggedMissingCaskMachineTemplate;
    private bool loggedMissingKegTemplate;
    private bool loggedMissingFurnaceTemplate;
    private bool loggedMissingRecyclerTemplate;
    private bool loggedMissingDehydratorTemplate;
    private bool loggedMissingDehydratorMachineTemplate;
    private bool loggedHardIridiumFallbackToVanillaKeg;
    private bool loggedHardIridiumBoundToHardwoodKeg;
    private bool loggedMissingKegMachineTemplate;
    private bool attemptedWindWheelTextureLoad;
    private Texture2D? cachedWindWheelTexture;

    // Texture asset keys (lazy-computed from manifest)
    private string CopperCableTexture => GetTextureAsset("CopperCable");
    private string IronCableTexture => GetTextureAsset("IronCable");
    private string IridiumCableTexture => GetTextureAsset("IridiumCable");
    private string EnergizedIridiumCableTexture => GetTextureAsset("EnergizedIridiumCable");
    private string BiofuelTexture => GetTextureAsset("Biofuel");
    private string RadioisotopeFuelTexture => GetTextureAsset("RadioisotopeFuel");
    private string HeatingCoilTexture => GetTextureAsset(HeatingCoilSpriteName);
    private string EfficiencyCoreTexture => GetTextureAsset(EfficiencyCoreSpriteName);
    private string CatalystChamberTexture => GetTextureAsset(CatalystChamberSpriteName);
    private string SortingMagnetTexture => GetTextureAsset(SortingMagnetSpriteName);
    private string DryingRackArrayTexture => GetTextureAsset(DryingRackArraySpriteName);
    private string HeatRegulatorTexture => GetTextureAsset(HeatRegulatorSpriteName);
    private string SteamGeneratorTexture => GetTextureAsset("SteamGenerator");
    private string CombustionGeneratorTexture => GetTextureAsset("CombustionGenerator");
    private string RadioisotopeGeneratorTexture => GetTextureAsset("RadioisotopeGenerator");
    private string WindGeneratorTexture => GetTextureAsset("WindGenerator");
    private string WindGeneratorWheelTexture => GetTextureAsset(WindGeneratorWheelSpriteName);
    private string BasicBatteryTexture => GetTextureAsset("BasicBattery");
    private string IridiumBatteryTexture => GetTextureAsset("IridiumBattery");
    private string PowerConduitTexture => GetTextureAsset("PowerConduit");
    private string IndustrialPreservesJarTexture => GetTextureAsset(IndustrialPreservesJarSpriteName);
    private string MetalCaskTexture => GetTextureAsset(MetalCaskSpriteName);
    private string MetalKegTexture => GetTextureAsset(MetalKegSpriteName);
    private string HardIridiumKegTexture => GetTextureAsset(HardIridiumKegSpriteName);
    private string ElectricSmelterTexture => GetTextureAsset(ElectricSmelterSpriteName);
    private string IndustrialRecyclerTexture => GetTextureAsset(IndustrialRecyclerSpriteName);
    private string PoweredDehydratorTexture => GetTextureAsset(PoweredDehydratorSpriteName);

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
    private sealed record ElectricSmelterRecipe(string InputQualifiedItemId, string OutputQualifiedItemId, int InputCount, int BaseMinutes);
    private sealed record IndustrialRecyclerRecipe(string InputQualifiedItemId, string OutputQualifiedItemId, int OutputCount);

    private static readonly Dictionary<string, ElectricSmelterRecipe> ElectricSmelterRecipes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["(O)378"] = new("(O)378", "(O)334", 5, 30),
        ["(O)380"] = new("(O)380", "(O)335", 5, 120),
        ["(O)384"] = new("(O)384", "(O)336", 5, 300),
        ["(O)386"] = new("(O)386", "(O)337", 5, 480),
        ["(O)909"] = new("(O)909", "(O)910", 5, 600)
    };

    private static readonly Dictionary<string, IndustrialRecyclerRecipe> IndustrialRecyclerRecipes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["(O)168"] = new("(O)168", "(O)390", 2),
        ["(O)169"] = new("(O)169", "(O)388", 2),
        ["(O)170"] = new("(O)170", "(O)338", 1),
        ["(O)171"] = new("(O)171", "(O)338", 1),
        ["(O)172"] = new("(O)172", "(O)771", 3),
        ["(O)167"] = new("(O)167", "(O)382", 1)
    };

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        I18n.Init(helper.Translation);
        Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
        bool migratedConfig = TryMigrateLegacyBalanceConfig(Config);
        migratedConfig |= TryMigrateHardIridiumKegSpeedConfig(Config);
        migratedConfig |= TryMigrateModerateBalanceConfig(Config);
        migratedConfig |= TryMigrateDenseBiofuelConfig(Config);
        migratedConfig |= TryMigrateSmoothSteamFuelConfig(Config);
        migratedConfig |= TryMigrateRadioisotopeFuelConfig(Config);
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
        helper.Events.Player.InventoryChanged += OnInventoryChanged;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;

        // Console commands
        helper.ConsoleCommands.Add("powergrid_status", I18n.Get("command.powergrid_status"), CmdStatus);
        helper.ConsoleCommands.Add("powergrid_debug", I18n.Get("command.powergrid_debug"), CmdDebug);
        helper.ConsoleCommands.Add("powergrid_conduit_reset", I18n.Get("command.powergrid_conduit_reset"), CmdConduitReset);
        helper.ConsoleCommands.Add("powergrid_unlock", I18n.Get("command.powergrid_unlock"), CmdUnlock);
        helper.ConsoleCommands.Add("powergrid_tab", I18n.Get("command.powergrid_tab"), CmdPowerTab);
        helper.ConsoleCommands.Add("powergrid_query_dump", I18n.Get("command.powergrid_query_dump"), CmdQueryDump);
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
        if (harmony != null)
            UiInfoSuiteIntegration.TryRegister(Helper, Monitor, harmony);
        Monitor.Log("[PowerGrid] Loaded. Waiting for save.", LogLevel.Trace);
    }

    private void RegisterPowerGridOwnedConsumers()
    {
        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.IndustrialPreservesJarId),
            DemandPerTick = GetDemandPerTick(Config.IndustrialPreservesJarEUPerMinute),
            MaxSpeedupFraction = ClampSpeedup(Config.IndustrialPreservesJarMaxSpeedup),
            Priority = Math.Max(0, Config.IndustrialPreservesJarPriority),
            DisplayName = I18n.Get("item.industrial-preserves-jar.name")
        });

        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.MetalCaskId),
            DemandPerTick = GetDemandPerTick(Config.MetalCaskEUPerMinute),
            MaxSpeedupFraction = ClampSpeedup(Config.MetalCaskMaxSpeedup),
            Priority = Math.Max(0, Config.MetalCaskPriority),
            DisplayName = I18n.Get("item.metal-cask.name")
        });

        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.MetalKegId),
            DemandPerTick = GetDemandPerTick(Config.MetalKegEUPerMinute),
            MaxSpeedupFraction = ClampSpeedup(Config.MetalKegMaxSpeedup),
            Priority = Math.Max(0, Config.MetalKegPriority),
            DisplayName = I18n.Get("item.metal-keg.name")
        });

        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.HardIridiumKegId),
            DemandPerTick = GetDemandPerTick(Config.HardIridiumKegEUPerMinute),
            MaxSpeedupFraction = ClampSpeedup(Config.HardIridiumKegMaxSpeedup),
            Priority = Math.Max(0, Config.HardIridiumKegPriority),
            DisplayName = I18n.Get("item.hard-iridium-keg.name")
        });

        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.ElectricSmelterId),
            DemandPerTick = Math.Max(0, Config.ElectricSmelterEUPerTick),
            MaxSpeedupFraction = ClampSpeedup(Config.ElectricSmelterMaxSpeedup),
            Priority = Math.Max(0, Config.ElectricSmelterPriority),
            DisplayName = I18n.Get("item.electric-smelter.name")
        });

        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.IndustrialRecyclerId),
            DemandPerTick = Math.Max(0, Config.IndustrialRecyclerEUPerTick),
            MaxSpeedupFraction = ClampSpeedup(Config.IndustrialRecyclerMaxSpeedup),
            Priority = Math.Max(0, Config.IndustrialRecyclerPriority),
            DisplayName = I18n.Get("item.industrial-recycler.name")
        });

        ConsumerRegistry.Instance.Register(new ConsumerDefinition
        {
            QualifiedItemId = PowerConstants.Q(PowerConstants.PoweredDehydratorId),
            DemandPerTick = Math.Max(0, Config.PoweredDehydratorEUPerTick),
            MaxSpeedupFraction = ClampSpeedup(Config.PoweredDehydratorMaxSpeedup),
            Priority = Math.Max(0, Config.PoweredDehydratorPriority),
            DisplayName = I18n.Get("item.powered-dehydrator.name")
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

    private static bool TryMigrateModerateBalanceConfig(ModConfig config)
    {
        bool matchesCurrentDefaults =
            config.CopperCableThroughput == 50
            && config.IronCableThroughput == 150
            && config.IridiumCableThroughput == 500
            && config.SteamGeneratorEUPerTick == 50
            && config.CombustionGeneratorEUPerTick == 120
            && config.WindGeneratorEUPerTick == 25
            && config.CoalFuelTicks == 12
            && config.WoodFuelTicks == 4
            && config.HardwoodFuelTicks == 8
            && config.BiofuelFuelTicks == 18
            && config.IndustrialPreservesJarEUPerMinute == 2
            && config.MetalCaskEUPerMinute == 4
            && config.MetalKegEUPerMinute == 1
            && config.HardIridiumKegEUPerMinute == 3
            && Math.Abs(config.IndustrialPreservesJarMaxSpeedup - 0.20f) <= 0.0001f
            && Math.Abs(config.MetalCaskMaxSpeedup - 0.50f) <= 0.0001f
            && Math.Abs(config.MetalKegMaxSpeedup - 0.20f) <= 0.0001f
            && Math.Abs(config.HardIridiumKegMaxSpeedup - 0.30f) <= 0.0001f;

        if (!matchesCurrentDefaults)
            return false;

        config.IronCableThroughput = 250;
        config.IridiumCableThroughput = 1000;
        config.SteamGeneratorEUPerTick = 75;
        config.CombustionGeneratorEUPerTick = 240;
        config.CoalFuelTicks = 24;
        config.WoodFuelTicks = 8;
        config.HardwoodFuelTicks = 16;
        config.BiofuelFuelTicks = 30;
        return true;
    }

    private static bool TryMigrateDenseBiofuelConfig(ModConfig config)
    {
        if (config.BiofuelFuelTicks != 30)
            return false;

        config.BiofuelFuelTicks = 60;
        return true;
    }

    private static bool TryMigrateSmoothSteamFuelConfig(ModConfig config)
    {
        bool matchesCurrentDefaults =
            config.CopperCableThroughput == 50
            && config.IronCableThroughput == 250
            && config.IridiumCableThroughput == 1000
            && config.EnergizedIridiumCableThroughput == 3000
            && config.SteamGeneratorEUPerTick == 75
            && config.CombustionGeneratorEUPerTick == 240
            && config.RadioisotopeGeneratorEUPerTick == 900
            && config.WindGeneratorEUPerTick == 25
            && config.CoalFuelTicks == 24
            && config.WoodFuelTicks == 8
            && config.HardwoodFuelTicks == 16
            && config.BiofuelFuelTicks == 60
            && config.RadioactiveBarFuelTicks == 360;

        if (!matchesCurrentDefaults)
            return false;

        config.CoalFuelTicks = 30;
        config.WoodFuelTicks = 10;
        config.HardwoodFuelTicks = 20;
        return true;
    }

    private static bool TryMigrateRadioisotopeFuelConfig(ModConfig config)
    {
        if (config.RadioisotopeFuelFuelTicks != 0)
            return false;

        config.RadioisotopeFuelFuelTicks = 720;
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
        RehydrateBatteryChargeState();
        PruneStaleGeneratorFuelState();
        PruneStaleConduitLinks();
        RehydrateMetalCasks();
        RefreshMetalCaskTelemetry();
        RefreshElectricSmelters();
        RefreshIndustrialRecyclers();
        RefreshPoweredDehydrators();

        PowerMgr.ResetRuntimeState();
        lastTimeOfDay = Game1.timeOfDay;

        if (importedLegacyState)
        {
            Monitor.Log(
                $"[PowerGrid] Imported compatibility state from persisted object modData for UniqueID migration. Batteries: {batteryData?.Count ?? 0}, conduit links: {conduitData?.Count ?? 0}, fueled generators: {fuelData?.Count ?? 0}.",
                LogLevel.Warn);
        }

        Monitor.Log($"[PowerGrid] Save loaded. Batteries: {GetTotalBatteryChargeInWorld() + BatteryState.TotalStoredEU()} EU stored. Conduit links: {ConduitMgr.GetAllLinks().Count}.", LogLevel.Trace);

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

    private static bool IsUpgradeableMachineItem(string itemId)
    {
        return itemId == PowerConstants.ElectricSmelterId
            || itemId == PowerConstants.IndustrialRecyclerId
            || itemId == PowerConstants.PoweredDehydratorId;
    }

    private void RehydrateBatteryChargeState()
    {
        if (!Context.IsWorldReady)
            return;

        foreach (GameLocation location in EnumerateLoadedLocations())
        {
            foreach ((Vector2 tile, StardewValley.Object obj) in location.objects.Pairs)
            {
                if (!TryBuildBatteryNode(location, tile, obj, out PowerNode batteryNode))
                    continue;

                BatteryState.GetCharge(batteryNode);
            }
        }
    }

    private void ApplyDailyBatteryLeakToPlacedBatteries()
    {
        if (!Context.IsWorldReady)
            return;

        foreach (GameLocation location in EnumerateLoadedLocations())
        {
            foreach ((Vector2 tile, StardewValley.Object obj) in location.objects.Pairs)
            {
                if (!TryBuildBatteryNode(location, tile, obj, out PowerNode batteryNode))
                    continue;

                BatteryState.ApplyDailyLeak(batteryNode);
            }
        }
    }

    private int GetTotalBatteryChargeInWorld()
    {
        if (!Context.IsWorldReady)
            return 0;

        int total = 0;
        foreach (GameLocation location in EnumerateLoadedLocations())
        {
            foreach ((Vector2 tile, StardewValley.Object obj) in location.objects.Pairs)
            {
                if (!TryBuildBatteryNode(location, tile, obj, out PowerNode batteryNode))
                    continue;

                total += BatteryState.GetCharge(batteryNode);
            }
        }

        return total;
    }

    private bool TryBuildBatteryNode(GameLocation location, Vector2 tile, StardewValley.Object obj, out PowerNode batteryNode)
    {
        batteryNode = null!;
        string itemId = obj.ItemId ?? "";
        if (!IsBatteryItem(itemId))
            return false;

        int capacity = GetBatteryCapacity(itemId);
        if (capacity <= 0)
            return false;

        batteryNode = new PowerNode
        {
            NodeType = PowerNodeType.Battery,
            LocationName = location.NameOrUniqueName,
            Tile = tile,
            ItemId = itemId,
            Capacity = capacity,
            SourceObject = obj
        };
        return true;
    }

    private void QueuePendingBatteryCharge(GameLocation location, Vector2 tile, StardewValley.Object obj)
    {
        if (!TryBuildBatteryNode(location, tile, obj, out PowerNode batteryNode))
            return;

        int charge = BatteryState.GetCharge(batteryNode);
        obj.modData[PersistedChargeKey] = charge.ToString(System.Globalization.CultureInfo.InvariantCulture);
        RemovePendingBatteryCharge(location.NameOrUniqueName, tile, obj.ItemId ?? "");
        pendingBatteryCharges.Add(new PendingBatteryCharge(location.NameOrUniqueName, tile, obj.ItemId ?? "", charge));
    }

    private void ApplyHeldBatteryChargeToPlacedObject(StardewValley.Object heldObject, StardewValley.Object placedObject, GameLocation location, Vector2 tile)
    {
        if (!IsBatteryItem(heldObject.ItemId ?? "") || !IsBatteryItem(placedObject.ItemId ?? ""))
            return;

        if (!TryReadNonNegativeInt(heldObject.modData, PersistedChargeKey, out int charge))
            return;

        placedObject.modData[PersistedChargeKey] = charge.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!TryBuildBatteryNode(location, tile, placedObject, out PowerNode batteryNode))
            return;

        BatteryState.SetCharge(batteryNode, charge);
        PowerMgr.MarkDirty(location.NameOrUniqueName);
    }

    private void QueuePendingMachineUpgrades(GameLocation location, Vector2 tile, StardewValley.Object obj)
    {
        string itemId = obj.ItemId ?? "";
        if (!IsUpgradeableMachineItem(itemId)
            || !MachineUpgradeState.TryGetSerialized(obj, out string serializedUpgradeIds))
        {
            return;
        }

        RemovePendingMachineUpgrades(location.NameOrUniqueName, tile, itemId);
        pendingMachineUpgrades.Add(new PendingMachineUpgradeData(location.NameOrUniqueName, tile, itemId, serializedUpgradeIds));
    }

    private void ApplyHeldMachineUpgradesToPlacedObject(StardewValley.Object heldObject, StardewValley.Object placedObject)
    {
        if (!IsUpgradeableMachineItem(heldObject.ItemId ?? "")
            || !string.Equals(heldObject.ItemId, placedObject.ItemId, StringComparison.Ordinal)
            || !MachineUpgradeState.TryGetSerialized(heldObject, out string serializedUpgradeIds))
        {
            return;
        }

        MachineUpgradeState.SetSerialized(placedObject, serializedUpgradeIds);
    }

    private void ApplyPendingBatteryChargeToItem(Item item)
    {
        if (item is not StardewValley.Object obj || !IsBatteryItem(obj.ItemId ?? ""))
            return;

        if (TryReadNonNegativeInt(obj.modData, PersistedChargeKey, out int existingCharge) && existingCharge > 0)
        {
            TryConsumePendingBatteryCharge(obj.ItemId ?? "", out _);
            return;
        }

        if (!TryConsumePendingBatteryCharge(obj.ItemId ?? "", out int charge))
            return;

        obj.modData[PersistedChargeKey] = charge.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void ApplyPendingMachineUpgradesToItem(Item item)
    {
        if (item is not StardewValley.Object obj || !IsUpgradeableMachineItem(obj.ItemId ?? ""))
            return;

        if (MachineUpgradeState.TryGetSerialized(obj, out _))
        {
            TryConsumePendingMachineUpgrades(obj.ItemId ?? "", out _);
            return;
        }

        if (!TryConsumePendingMachineUpgrades(obj.ItemId ?? "", out string serializedUpgradeIds))
            return;

        MachineUpgradeState.SetSerialized(obj, serializedUpgradeIds);
    }

    private void CopyPendingBatteryChargeToItem(Item item)
    {
        if (item is not StardewValley.Object obj || !IsBatteryItem(obj.ItemId ?? ""))
            return;

        if (TryReadNonNegativeInt(obj.modData, PersistedChargeKey, out int existingCharge) && existingCharge > 0)
            return;

        if (!TryPeekPendingBatteryCharge(obj.ItemId ?? "", out int charge))
            return;

        obj.modData[PersistedChargeKey] = charge.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void CopyPendingMachineUpgradesToItem(Item item)
    {
        if (item is not StardewValley.Object obj || !IsUpgradeableMachineItem(obj.ItemId ?? ""))
            return;

        if (MachineUpgradeState.TryGetSerialized(obj, out _))
            return;

        if (!TryPeekPendingMachineUpgrades(obj.ItemId ?? "", out string serializedUpgradeIds))
            return;

        MachineUpgradeState.SetSerialized(obj, serializedUpgradeIds);
    }

    private bool TryPeekPendingBatteryCharge(string itemId, out int charge)
    {
        charge = 0;
        for (int i = pendingBatteryCharges.Count - 1; i >= 0; i--)
        {
            PendingBatteryCharge pending = pendingBatteryCharges[i];
            if (!string.Equals(pending.ItemId, itemId, StringComparison.Ordinal))
                continue;

            charge = pending.Charge;
            return true;
        }

        return false;
    }

    private bool TryConsumePendingBatteryCharge(string itemId, out int charge)
    {
        charge = 0;
        for (int i = pendingBatteryCharges.Count - 1; i >= 0; i--)
        {
            PendingBatteryCharge pending = pendingBatteryCharges[i];
            if (!string.Equals(pending.ItemId, itemId, StringComparison.Ordinal))
                continue;

            charge = pending.Charge;
            pendingBatteryCharges.RemoveAt(i);
            return true;
        }

        return false;
    }

    private bool TryPeekPendingMachineUpgrades(string itemId, out string serializedUpgradeIds)
    {
        serializedUpgradeIds = "";
        for (int i = pendingMachineUpgrades.Count - 1; i >= 0; i--)
        {
            PendingMachineUpgradeData pending = pendingMachineUpgrades[i];
            if (!string.Equals(pending.ItemId, itemId, StringComparison.Ordinal))
                continue;

            serializedUpgradeIds = pending.SerializedUpgradeIds;
            return true;
        }

        return false;
    }

    private bool TryConsumePendingMachineUpgrades(string itemId, out string serializedUpgradeIds)
    {
        serializedUpgradeIds = "";
        for (int i = pendingMachineUpgrades.Count - 1; i >= 0; i--)
        {
            PendingMachineUpgradeData pending = pendingMachineUpgrades[i];
            if (!string.Equals(pending.ItemId, itemId, StringComparison.Ordinal))
                continue;

            serializedUpgradeIds = pending.SerializedUpgradeIds;
            pendingMachineUpgrades.RemoveAt(i);
            return true;
        }

        return false;
    }

    private void RemovePendingBatteryCharge(string locationName, Vector2 tile, string itemId)
    {
        for (int i = pendingBatteryCharges.Count - 1; i >= 0; i--)
        {
            PendingBatteryCharge pending = pendingBatteryCharges[i];
            if (string.Equals(pending.LocationName, locationName, StringComparison.Ordinal)
                && pending.Tile == tile
                && string.Equals(pending.ItemId, itemId, StringComparison.Ordinal))
            {
                pendingBatteryCharges.RemoveAt(i);
            }
        }
    }

    private void RemovePendingMachineUpgrades(string locationName, Vector2 tile, string itemId)
    {
        for (int i = pendingMachineUpgrades.Count - 1; i >= 0; i--)
        {
            PendingMachineUpgradeData pending = pendingMachineUpgrades[i];
            if (string.Equals(pending.LocationName, locationName, StringComparison.Ordinal)
                && pending.Tile == tile
                && string.Equals(pending.ItemId, itemId, StringComparison.Ordinal))
            {
                pendingMachineUpgrades.RemoveAt(i);
            }
        }
    }

    private static bool IsGeneratorItem(string itemId)
    {
        return itemId == PowerConstants.SteamGeneratorId
            || itemId == PowerConstants.CombustionGeneratorId
            || itemId == PowerConstants.RadioisotopeGeneratorId
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
        ApplyDailyBatteryLeakToPlacedBatteries();
        BatteryState.ApplyDailyLeak();
        RehydrateMetalCasks();
        RefreshMetalCaskTelemetry();
        RefreshElectricSmelters();
        RefreshIndustrialRecyclers();
        RefreshPoweredDehydrators();
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
        pendingBatteryCharges.Clear();
        pendingMachineUpgrades.Clear();
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
                RefreshElectricSmelters(enforcePowerLoss: true);
                RefreshIndustrialRecyclers();
                RefreshPoweredDehydrators();
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
            HandleRemovedBatteryState(e.Location, tile, removedObj);
            HandleRemovedMachineUpgradeState(e.Location, tile, removedObj);
            HandleRemovedGeneratorFuelState(e.Location, tile, removedObj);
            removedConduitState |= HandleRemovedConduitState(e.Location, tile, removedObj);
        }

        RehydrateMetalCasks(e.Location);
        RefreshMetalCaskTelemetry(e.Location);
        RefreshElectricSmelters(e.Location);
        CancelDisconnectedElectricSmelters(e.Location);
        RefreshIndustrialRecyclers(e.Location);
        RefreshPoweredDehydrators(e.Location);

        // Mark location dirty when objects are placed/removed.
        PowerMgr.MarkDirty(e.Location.NameOrUniqueName);
        if (removedConduitState)
            PowerMgr.MarkAllDirty();
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsLocalPlayer)
            return;

        foreach (Item item in e.Added)
        {
            ApplyPendingBatteryChargeToItem(item);
            ApplyPendingMachineUpgradesToItem(item);
        }
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

            if (wantsLegacyMonitor && TryOpenMachinePanel(loc, tile, obj))
            {
                Helper.Input.Suppress(e.Button);
                return;
            }

            if (itemId == PowerConstants.ElectricSmelterId && TryHandleElectricSmelterInteraction(loc, tile, obj))
            {
                Helper.Input.Suppress(e.Button);
                PowerMgr.MarkDirty(loc.NameOrUniqueName);
                return;
            }

            if (itemId == PowerConstants.IndustrialRecyclerId && TryHandleIndustrialRecyclerInteraction(loc, tile, obj))
            {
                Helper.Input.Suppress(e.Button);
                PowerMgr.MarkDirty(loc.NameOrUniqueName);
                return;
            }

            if (itemId == PowerConstants.PoweredDehydratorId && TryHandlePoweredDehydratorInteraction(obj))
            {
                Helper.Input.Suppress(e.Button);
                PowerMgr.MarkDirty(loc.NameOrUniqueName);
                return;
            }

            // Power Conduit interaction: pairing / unlink
            if (itemId == PowerConstants.PowerConduitId)
            {
                if (wantsLegacyMonitor)
                {
                    if (ConduitMgr.RemoveConduitState(loc.NameOrUniqueName, tile))
                    {
                        PowerMgr.MarkAllDirty();
                        Game1.addHUDMessage(new HUDMessage(I18n.Get("message.conduit.link-removed"), HUDMessage.error_type));
                    }
                    else if (ConduitMgr.HasPending)
                    {
                        ConduitMgr.CancelPairing();
                        Game1.addHUDMessage(new HUDMessage(I18n.Get("message.conduit.pairing-cancelled"), HUDMessage.error_type));
                    }
                    else
                    {
                        Game1.addHUDMessage(new HUDMessage(I18n.Get("message.conduit.no-active-link"), HUDMessage.newQuest_type));
                    }

                    Helper.Input.Suppress(e.Button);
                    return;
                }

                if (!ConduitMgr.HasPending)
                {
                    ConduitMgr.StartPairing(loc.NameOrUniqueName, tile);
                    Game1.addHUDMessage(new HUDMessage(I18n.Get("message.conduit.pairing-started"), HUDMessage.newQuest_type));
                }
                else
                {
                    if (ConduitMgr.TryCompletePairing(loc.NameOrUniqueName, tile))
                    {
                        Game1.addHUDMessage(new HUDMessage(I18n.Get("message.conduit.linked"), HUDMessage.achievement_type));
                        PowerMgr.MarkAllDirty();
                    }
                    else
                    {
                        ConduitMgr.CancelPairing();
                        Game1.addHUDMessage(new HUDMessage(I18n.Get("message.conduit.pairing-cancelled"), HUDMessage.error_type));
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

    private bool TryOpenMachinePanel(GameLocation location, Vector2 tile, StardewValley.Object obj)
    {
        MachinePanelSpec? spec = (obj.ItemId ?? "") switch
        {
            PowerConstants.ElectricSmelterId => new MachinePanelSpec(PowerConstants.ElectricSmelterId, ElectricSmelterUpgradeSlots, ElectricSmelterUpgradeIds),
            PowerConstants.IndustrialRecyclerId => new MachinePanelSpec(PowerConstants.IndustrialRecyclerId, IndustrialRecyclerUpgradeSlots, IndustrialRecyclerUpgradeIds),
            PowerConstants.PoweredDehydratorId => new MachinePanelSpec(PowerConstants.PoweredDehydratorId, PoweredDehydratorUpgradeSlots, PoweredDehydratorUpgradeIds),
            _ => null
        };

        if (spec == null)
            return false;

        Game1.activeClickableMenu = new MachinePanelMenu(
            location,
            tile,
            obj,
            spec,
            PowerQuery,
            () => PowerMgr.MarkDirty(location.NameOrUniqueName));
        return true;
    }

    private static bool IsFuelGeneratorItem(string itemId)
    {
        return itemId == PowerConstants.SteamGeneratorId
            || itemId == PowerConstants.CombustionGeneratorId
            || itemId == PowerConstants.RadioisotopeGeneratorId;
    }

    private bool TryHandleElectricSmelterInteraction(GameLocation location, Vector2 tile, StardewValley.Object smelter)
    {
        if (TryCollectElectricSmelterOutput(smelter))
            return true;

        StardewValley.Object? activeObject = Game1.player.ActiveObject;
        if (activeObject == null)
            return false;

        if (TryInstallElectricSmelterUpgrade(smelter, activeObject))
            return true;

        return TryStartElectricSmelter(location, tile, smelter, activeObject, showMessages: true, consumeInput: () => ConsumeActiveObject(ElectricSmelterRecipes[activeObject.QualifiedItemId].InputCount));
    }

    private bool TryCollectElectricSmelterOutput(StardewValley.Object smelter)
    {
        StardewValley.Object? output = smelter.heldObject.Value;
        if (output == null || smelter.MinutesUntilReady > 0)
            return false;

        if (!Game1.player.addItemToInventoryBool(output))
        {
            Game1.addHUDMessage(new HUDMessage(I18n.Get("message.inventory-full"), HUDMessage.error_type));
            return true;
        }

        smelter.heldObject.Value = null;
        smelter.readyForHarvest.Value = false;
        smelter.MinutesUntilReady = 0;
        ClearElectricSmelterInputData(smelter);
        Game1.playSound("coin");
        return true;
    }

    private bool TryInstallElectricSmelterUpgrade(StardewValley.Object smelter, StardewValley.Object activeObject)
    {
        return TryInstallSupportedMachineUpgrade(smelter, activeObject, ElectricSmelterUpgradeSlots, ElectricSmelterUpgradeIds);
    }

    private bool TryStartElectricSmelter(GameLocation location, Vector2 tile, StardewValley.Object smelter, StardewValley.Object input, bool showMessages, Action consumeInput)
    {
        if (!ElectricSmelterRecipes.TryGetValue(input.QualifiedItemId, out ElectricSmelterRecipe? recipe))
            return false;

        if (smelter.MinutesUntilReady > 0 || smelter.heldObject.Value != null)
        {
            if (showMessages)
                Game1.addHUDMessage(new HUDMessage(I18n.Get("message.electric-smelter.busy"), HUDMessage.error_type));
            return true;
        }

        if (input.Stack < recipe.InputCount)
        {
            if (showMessages)
                Game1.addHUDMessage(new HUDMessage(I18n.Get("message.electric-smelter.need-ore", new { count = recipe.InputCount }), HUDMessage.error_type));
            return true;
        }

        if (!HasPowerGridInfrastructureConnection(location, tile, PowerConstants.ElectricSmelterId))
        {
            if (showMessages)
                Game1.addHUDMessage(new HUDMessage(I18n.Get("message.electric-smelter.no-power"), HUDMessage.error_type));
            return true;
        }

        StardewValley.Object output = ItemRegistry.Create<StardewValley.Object>(recipe.OutputQualifiedItemId);
        double bonusChance = MachineUpgradeState.HasUpgrade(smelter, PowerConstants.CatalystChamberId)
            ? ElectricSmelterCatalystBonusOutputChance
            : ElectricSmelterBonusOutputChance;
        output.Stack = Game1.random.NextDouble() < bonusChance ? 2 : 1;

        smelter.heldObject.Value = output;
        smelter.MinutesUntilReady = GetElectricSmelterMinutes(smelter, recipe);
        smelter.readyForHarvest.Value = false;
        smelter.shakeTimer = 100;
        smelter.modData[ElectricSmelterInputItemKey] = recipe.InputQualifiedItemId;
        smelter.modData[ElectricSmelterInputCountKey] = recipe.InputCount.ToString(CultureInfo.InvariantCulture);

        consumeInput();
        Game1.playSound("furnace");
        if (showMessages)
            Game1.addHUDMessage(new HUDMessage(I18n.Get("message.electric-smelter.started", new { item = output.DisplayName }), HUDMessage.newQuest_type));
        return true;
    }

    private bool HasPowerGridInfrastructureConnection(GameLocation location, Vector2 tile, string itemId)
    {
        PowerMgr.MarkDirty(location.NameOrUniqueName);

        PowerConsumerSnapshot? consumerSnapshot = PowerQuery.GetConsumerSnapshots(location.NameOrUniqueName)
            .FirstOrDefault(consumer =>
                consumer.TileX == (int)tile.X
                && consumer.TileY == (int)tile.Y
                && string.Equals(consumer.ItemId, itemId, StringComparison.Ordinal));

        if (consumerSnapshot == null)
            return false;

        foreach (PowerNetworkSnapshot network in PowerQuery.GetNetworkSnapshots())
        {
            if (network.NetworkId != consumerSnapshot.NetworkId)
                continue;

            return network.CableCount > 0
                || network.GeneratorCount > 0
                || network.BatteryCount > 0
                || network.ConduitCount > 0;
        }

        return false;
    }

    private static int GetElectricSmelterMinutes(StardewValley.Object smelter, ElectricSmelterRecipe recipe)
    {
        float multiplier = MachineUpgradeState.HasUpgrade(smelter, PowerConstants.HeatingCoilId)
            ? ElectricSmelterHeatingCoilSpeedMultiplier
            : 1f;

        return Math.Max(PowerConstants.TickIntervalMinutes, (int)MathF.Ceiling(recipe.BaseMinutes * multiplier));
    }

    private static void ConsumeActiveObject(int count)
    {
        for (int i = 0; i < count; i++)
            Game1.player.reduceActiveItemByOne();
    }

    private bool TryHandleIndustrialRecyclerInteraction(GameLocation location, Vector2 tile, StardewValley.Object recycler)
    {
        if (TryCollectIndustrialMachineOutput(recycler))
            return true;

        StardewValley.Object? activeObject = Game1.player.ActiveObject;
        if (activeObject == null)
            return false;

        if (TryInstallIndustrialRecyclerUpgrade(recycler, activeObject))
            return true;

        return TryStartIndustrialRecycler(recycler, activeObject, showMessages: true, consumeInput: () => ConsumeActiveObject(1));
    }

    private bool TryInstallIndustrialRecyclerUpgrade(StardewValley.Object recycler, StardewValley.Object activeObject)
    {
        return TryInstallSupportedMachineUpgrade(recycler, activeObject, IndustrialRecyclerUpgradeSlots, IndustrialRecyclerUpgradeIds);
    }

    private static StardewValley.Object CreateIndustrialRecyclerOutput(StardewValley.Object recycler, IndustrialRecyclerRecipe recipe)
    {
        string outputQualifiedItemId = recipe.OutputQualifiedItemId;
        int outputCount = recipe.OutputCount;

        if (MachineUpgradeState.HasUpgrade(recycler, PowerConstants.SortingMagnetId)
            && Game1.random.NextDouble() < 0.20)
        {
            outputQualifiedItemId = Game1.random.NextDouble() < 0.60 ? "(O)378" : "(O)380";
            outputCount = 1;
        }

        StardewValley.Object output = ItemRegistry.Create<StardewValley.Object>(outputQualifiedItemId);
        output.Stack = outputCount;
        return output;
    }

    private bool TryStartIndustrialRecycler(StardewValley.Object recycler, StardewValley.Object input, bool showMessages, Action consumeInput)
    {
        if (!IndustrialRecyclerRecipes.TryGetValue(input.QualifiedItemId, out IndustrialRecyclerRecipe? recipe))
            return false;

        if (recycler.MinutesUntilReady > 0 || recycler.heldObject.Value != null)
        {
            if (showMessages)
                Game1.addHUDMessage(new HUDMessage(I18n.Get("message.industrial-recycler.busy"), HUDMessage.error_type));
            return true;
        }

        StardewValley.Object output = CreateIndustrialRecyclerOutput(recycler, recipe);
        recycler.heldObject.Value = output;
        recycler.MinutesUntilReady = IndustrialRecyclerBaseMinutes;
        recycler.readyForHarvest.Value = false;
        recycler.shakeTimer = 100;

        consumeInput();
        Game1.playSound("trashcan");
        if (showMessages)
            Game1.addHUDMessage(new HUDMessage(I18n.Get("message.industrial-recycler.started", new { item = output.DisplayName }), HUDMessage.newQuest_type));
        return true;
    }

    private bool TryHandlePoweredDehydratorInteraction(StardewValley.Object dehydrator)
    {
        StardewValley.Object? activeObject = Game1.player.ActiveObject;
        if (activeObject == null)
            return false;

        return TryInstallSupportedMachineUpgrade(dehydrator, activeObject, PoweredDehydratorUpgradeSlots, PoweredDehydratorUpgradeIds);
    }

    private bool TryInstallSupportedMachineUpgrade(StardewValley.Object machine, StardewValley.Object activeObject, int maxSlots, IReadOnlyList<string> supportedUpgradeIds)
    {
        if (!supportedUpgradeIds.Contains(activeObject.ItemId, StringComparer.OrdinalIgnoreCase))
            return false;

        if (MachineUpgradeState.TryInstall(machine, activeObject.ItemId, maxSlots, out string reason))
        {
            Game1.player.reduceActiveItemByOne();
            Game1.playSound("Ship");
            Game1.addHUDMessage(new HUDMessage(I18n.Get("message.upgrade.installed", new { upgrade = activeObject.DisplayName }), HUDMessage.achievement_type));
            return true;
        }

        string messageKey = reason switch
        {
            "already-installed" => "message.upgrade.already-installed",
            "slots-full" => "message.upgrade.slots-full",
            _ => "message.upgrade.cannot-install"
        };

        Game1.addHUDMessage(new HUDMessage(I18n.Get(messageKey), HUDMessage.error_type));
        return true;
    }

    private bool TryCollectIndustrialMachineOutput(StardewValley.Object machine)
    {
        StardewValley.Object? output = machine.heldObject.Value;
        if (output == null || machine.MinutesUntilReady > 0)
            return false;

        if (!Game1.player.addItemToInventoryBool(output))
        {
            Game1.addHUDMessage(new HUDMessage(I18n.Get("message.inventory-full"), HUDMessage.error_type));
            return true;
        }

        machine.heldObject.Value = null;
        machine.readyForHarvest.Value = false;
        machine.MinutesUntilReady = 0;
        Game1.playSound("coin");
        return true;
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
                    CableTier.EnergizedIridium => EnergizedIridiumCableTexture,
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
        TryLoadTexture(e, EnergizedIridiumCableTexture, "EnergizedIridiumCable", new Color(100, 220, 180));
        TryLoadObjectTexture(e, BiofuelTexture, "Biofuel", new Color(84, 196, 118));
        TryLoadObjectTexture(e, RadioisotopeFuelTexture, "RadioisotopeFuel", new Color(160, 220, 80));
        TryLoadObjectTexture(e, HeatingCoilTexture, HeatingCoilSpriteName, new Color(230, 150, 80));
        TryLoadObjectTexture(e, EfficiencyCoreTexture, EfficiencyCoreSpriteName, new Color(120, 180, 220));
        TryLoadObjectTexture(e, CatalystChamberTexture, CatalystChamberSpriteName, new Color(150, 100, 210));
        TryLoadObjectTexture(e, SortingMagnetTexture, SortingMagnetSpriteName, new Color(160, 170, 190));
        TryLoadObjectTexture(e, DryingRackArrayTexture, DryingRackArraySpriteName, new Color(160, 150, 140));
        TryLoadObjectTexture(e, HeatRegulatorTexture, HeatRegulatorSpriteName, new Color(180, 170, 190));
        TryLoadTexture(e, SteamGeneratorTexture, "SteamGenerator", new Color(140, 140, 160));
        TryLoadTexture(e, CombustionGeneratorTexture, "CombustionGenerator", new Color(120, 128, 144));
        TryLoadTexture(e, RadioisotopeGeneratorTexture, "RadioisotopeGenerator", new Color(110, 184, 132));
        TryLoadTexture(e, WindGeneratorTexture, "WindGenerator", new Color(100, 180, 220));
        TryLoadOptionalTexture(e, WindGeneratorWheelTexture, WindGeneratorWheelSpriteName);
        TryLoadTexture(e, BasicBatteryTexture, "BasicBattery", new Color(60, 180, 60));
        TryLoadTexture(e, IridiumBatteryTexture, "IridiumBattery", new Color(150, 80, 220));
        TryLoadTexture(e, PowerConduitTexture, "PowerConduit", new Color(220, 200, 60));
        TryLoadTexture(e, IndustrialPreservesJarTexture, IndustrialPreservesJarSpriteName, new Color(180, 120, 80));
        TryLoadTexture(e, MetalCaskTexture, MetalCaskSpriteName, new Color(130, 130, 140));
        TryLoadTexture(e, MetalKegTexture, MetalKegSpriteName, new Color(170, 170, 180));
        TryLoadTexture(e, HardIridiumKegTexture, HardIridiumKegSpriteName, new Color(150, 110, 210));
        TryLoadTexture(e, ElectricSmelterTexture, ElectricSmelterSpriteName, new Color(190, 120, 80));
        TryLoadTexture(e, IndustrialRecyclerTexture, IndustrialRecyclerSpriteName, new Color(120, 140, 150));
        TryLoadTexture(e, PoweredDehydratorTexture, PoweredDehydratorSpriteName, new Color(150, 170, 190));

        TryLoadStateTexture(e, "SteamGenerator", "off", new Color(140, 140, 160));
        TryLoadStateTexture(e, "SteamGenerator", "on", new Color(140, 140, 160));
        TryLoadStateTexture(e, "CombustionGenerator", "off", new Color(120, 128, 144));
        TryLoadStateTexture(e, "CombustionGenerator", "on", new Color(120, 128, 144));
        TryLoadStateTexture(e, "RadioisotopeGenerator", "off", new Color(110, 184, 132));
        TryLoadStateTexture(e, "RadioisotopeGenerator", "on", new Color(110, 184, 132));
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
        TryLoadStateTexture(e, ElectricSmelterSpriteName, ElectricSmelterOfflineState, new Color(190, 120, 80));
        TryLoadStateTexture(e, ElectricSmelterSpriteName, ElectricSmelterStandbyState, new Color(190, 120, 80));
        TryLoadStateTexture(e, ElectricSmelterSpriteName, ElectricSmelterProcessingPoweredState, new Color(190, 120, 80));
        TryLoadStateTexture(e, IndustrialRecyclerSpriteName, IndustrialPreservesJarPoweredState, new Color(120, 140, 150));
        TryLoadStateTexture(e, IndustrialRecyclerSpriteName, IndustrialPreservesJarUnpoweredState, new Color(120, 140, 150));
        TryLoadStateTexture(e, PoweredDehydratorSpriteName, IndustrialPreservesJarPoweredState, new Color(150, 170, 190));
        TryLoadStateTexture(e, PoweredDehydratorSpriteName, IndustrialPreservesJarUnpoweredState, new Color(150, 170, 190));
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
            "RadioisotopeFuel" => 74, // Solar Essence
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
        RegisterBigCraftable(dict, template, PowerConstants.CopperCableId, "Copper Cable", I18n.Get("item.copper-cable.name"),
            I18n.Get("item.copper-cable.description"), CopperCableTexture, passable: true, spriteIndex: 15);
        RegisterBigCraftable(dict, template, PowerConstants.IronCableId, "Iron Cable", I18n.Get("item.iron-cable.name"),
            I18n.Get("item.iron-cable.description"), IronCableTexture, passable: true, spriteIndex: 15);
        RegisterBigCraftable(dict, template, PowerConstants.IridiumCableId, "Iridium Cable", I18n.Get("item.iridium-cable.name"),
            I18n.Get("item.iridium-cable.description"), IridiumCableTexture, passable: true, spriteIndex: 15);
        RegisterBigCraftable(dict, template, PowerConstants.EnergizedIridiumCableId, "Energized Iridium Cable", I18n.Get("item.energized-iridium-cable.name"),
            I18n.Get("item.energized-iridium-cable.description"), EnergizedIridiumCableTexture, passable: true, spriteIndex: 15);

        // Generators
        RegisterBigCraftable(dict, template, PowerConstants.SteamGeneratorId, "Steam Generator", I18n.Get("item.steam-generator.name"),
            I18n.Get("item.steam-generator.description"), SteamGeneratorTexture);
        RegisterBigCraftable(dict, template, PowerConstants.CombustionGeneratorId, CombustionGeneratorRecipeKey, I18n.Get("item.combustion-generator.name"),
            I18n.Get("item.combustion-generator.description"), CombustionGeneratorTexture);
        RegisterBigCraftable(dict, template, PowerConstants.RadioisotopeGeneratorId, RadioisotopeGeneratorRecipeKey, I18n.Get("item.radioisotope-generator.name"),
            I18n.Get("item.radioisotope-generator.description"), RadioisotopeGeneratorTexture);
        RegisterBigCraftable(dict, template, PowerConstants.WindGeneratorId, "Wind Generator", I18n.Get("item.wind-generator.name"),
            I18n.Get("item.wind-generator.description"), WindGeneratorTexture);

        // Batteries
        RegisterBigCraftable(dict, template, PowerConstants.BasicBatteryId, "Basic Power Battery", I18n.Get("item.basic-battery.name"),
            I18n.Get("item.basic-battery.description"), BasicBatteryTexture);
        RegisterBigCraftable(dict, template, PowerConstants.IridiumBatteryId, "Iridium Power Battery", I18n.Get("item.iridium-battery.name"),
            I18n.Get("item.iridium-battery.description"), IridiumBatteryTexture);

        // Power Conduit
        RegisterBigCraftable(dict, template, PowerConstants.PowerConduitId, "Power Conduit", I18n.Get("item.power-conduit.name"),
            I18n.Get("item.power-conduit.description"), PowerConduitTexture);

        RegisterIndustrialPreservesJar(dict);
        RegisterMetalCask(dict);
        RegisterPoweredArtisanMachines(dict);
        RegisterElectricSmelter(dict);
        RegisterIndustrialRecycler(dict);
        RegisterPoweredDehydrator(dict);
    }

    private void EditObjects(IAssetData asset)
    {
        var dict = asset.AsDictionary<string, ObjectData>().Data;
        dict[PowerConstants.BiofuelId] = CreateBiofuelObjectData();
        dict[PowerConstants.RadioisotopeFuelId] = CreateRadioisotopeFuelObjectData();
        dict[PowerConstants.HeatingCoilId] = CreateUpgradeObjectData(
            HeatingCoilRecipeKey,
            I18n.Get("item.heating-coil.name"),
            I18n.Get("item.heating-coil.description"),
            HeatingCoilTexture,
            price: 350,
            contextTag: "powergrid_heating_coil");
        dict[PowerConstants.EfficiencyCoreId] = CreateUpgradeObjectData(
            EfficiencyCoreRecipeKey,
            I18n.Get("item.efficiency-core.name"),
            I18n.Get("item.efficiency-core.description"),
            EfficiencyCoreTexture,
            price: 450,
            contextTag: "powergrid_efficiency_core");
        dict[PowerConstants.CatalystChamberId] = CreateUpgradeObjectData(
            CatalystChamberRecipeKey,
            I18n.Get("item.catalyst-chamber.name"),
            I18n.Get("item.catalyst-chamber.description"),
            CatalystChamberTexture,
            price: 500,
            contextTag: "powergrid_catalyst_chamber");
        dict[PowerConstants.SortingMagnetId] = CreateUpgradeObjectData(
            SortingMagnetRecipeKey,
            I18n.Get("item.sorting-magnet.name"),
            I18n.Get("item.sorting-magnet.description"),
            SortingMagnetTexture,
            price: 300,
            contextTag: "powergrid_sorting_magnet");
        dict[PowerConstants.DryingRackArrayId] = CreateUpgradeObjectData(
            DryingRackArrayRecipeKey,
            I18n.Get("item.drying-rack-array.name"),
            I18n.Get("item.drying-rack-array.description"),
            DryingRackArrayTexture,
            price: 450,
            contextTag: "powergrid_drying_rack_array");
        dict[PowerConstants.HeatRegulatorId] = CreateUpgradeObjectData(
            HeatRegulatorRecipeKey,
            I18n.Get("item.heat-regulator.name"),
            I18n.Get("item.heat-regulator.description"),
            HeatRegulatorTexture,
            price: 350,
            contextTag: "powergrid_heat_regulator");
    }

    private ObjectData CreateBiofuelObjectData()
    {
        string displayName = I18n.Get("item.biofuel.name");
        string description = I18n.Get("item.biofuel.description");

        ObjectData? data = JsonSerializer.Deserialize<ObjectData>(
            $$"""
            {
              "Name": "{{BiofuelRecipeKey}}",
              "DisplayName": "{{displayName}}",
              "Description": "{{description}}",
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

    private ObjectData CreateRadioisotopeFuelObjectData()
    {
        string displayName = I18n.Get("item.radioisotope-fuel.name");
        string description = I18n.Get("item.radioisotope-fuel.description");

        ObjectData? data = JsonSerializer.Deserialize<ObjectData>(
            $$"""
            {
              "Name": "{{RadioisotopeFuelRecipeKey}}",
              "DisplayName": "{{displayName}}",
              "Description": "{{description}}",
              "Type": "Basic",
              "Category": 0,
              "Price": 300,
              "Texture": "{{RadioisotopeFuelTexture}}",
              "SpriteIndex": 0,
              "Edibility": -300,
              "CanBeGivenAsGift": false,
              "CanBeTrashed": true,
              "ExcludeFromShippingCollection": true,
              "ExcludeFromRandomSale": true,
              "ContextTags": [ "powergrid_radioisotope_fuel", "powergrid_fuel" ]
            }
            """,
            GameDataCloneJsonOptions);

        return data ?? throw new InvalidOperationException("Failed to build Radioisotope Fuel object data.");
    }

    private static ObjectData CreateUpgradeObjectData(string internalName, string displayName, string description, string texture, int price, string contextTag)
    {
        ObjectData? data = JsonSerializer.Deserialize<ObjectData>(
            $$"""
            {
              "Name": "{{internalName}}",
              "DisplayName": "{{displayName}}",
              "Description": "{{description}}",
              "Type": "Basic",
              "Category": 0,
              "Price": {{price}},
              "Texture": "{{texture}}",
              "SpriteIndex": 0,
              "Edibility": -300,
              "CanBeGivenAsGift": false,
              "CanBeTrashed": true,
              "ExcludeFromShippingCollection": true,
              "ExcludeFromRandomSale": true,
              "ContextTags": [ "{{contextTag}}", "powergrid_upgrade" ]
            }
            """,
            GameDataCloneJsonOptions);

        return data ?? throw new InvalidOperationException($"Failed to build {internalName} object data.");
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
        industrialData.DisplayName = I18n.Get("item.industrial-preserves-jar.name");
        industrialData.Description = I18n.Get("item.industrial-preserves-jar.description");
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
        metalCaskData.DisplayName = I18n.Get("item.metal-cask.name");
        metalCaskData.Description = I18n.Get("item.metal-cask.description");
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
        metalKegData.DisplayName = I18n.Get("item.metal-keg.name");
        metalKegData.Description = I18n.Get("item.metal-keg.description");
        metalKegData.Texture = MetalKegTexture;
        metalKegData.SpriteIndex = 0;
        dict[PowerConstants.MetalKegId] = metalKegData;

        KeyValuePair<string, BigCraftableData>? hardwoodTemplateOpt = FindHardwoodKegBigCraftableTemplate(dict);
        if (hardwoodTemplateOpt != null)
            hardwoodKegBigCraftableId = hardwoodTemplateOpt.Value.Key;
        else if (!loggedHardIridiumFallbackToVanillaKeg)
        {
            Monitor.Log("[PowerGrid] Hardwood Keg template was not found in Data/BigCraftables yet; Hard Iridium Keg will use vanilla Keg item data until a compatible Hardwood Keg template is available.", LogLevel.Trace);
            loggedHardIridiumFallbackToVanillaKeg = true;
        }

        BigCraftableData hardIridiumData = CloneJson(hardwoodTemplateOpt?.Value ?? kegTemplate.Value);
        hardIridiumData.Name = HardIridiumKegRecipeKey;
        hardIridiumData.DisplayName = I18n.Get("item.hard-iridium-keg.name");
        hardIridiumData.Description = I18n.Get("item.hard-iridium-keg.description");
        hardIridiumData.Texture = HardIridiumKegTexture;
        hardIridiumData.SpriteIndex = 0;
        dict[PowerConstants.HardIridiumKegId] = hardIridiumData;
    }

    private void RegisterElectricSmelter(IDictionary<string, BigCraftableData> dict)
    {
        KeyValuePair<string, BigCraftableData>? templateOpt =
            FindBigCraftableTemplate(dict, targetDisplayName: "Furnace", preferredKey: "13");

        if (templateOpt == null)
        {
            if (!loggedMissingFurnaceTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Furnace template in Data/BigCraftables; Electric Smelter was not added.", LogLevel.Error);
                loggedMissingFurnaceTemplate = true;
            }

            return;
        }

        BigCraftableData smelterData = CloneJson(templateOpt.Value.Value);
        smelterData.Name = ElectricSmelterRecipeKey;
        smelterData.DisplayName = I18n.Get("item.electric-smelter.name");
        smelterData.Description = I18n.Get("item.electric-smelter.description");
        smelterData.Texture = ElectricSmelterTexture;
        smelterData.SpriteIndex = 0;

        dict[PowerConstants.ElectricSmelterId] = smelterData;
    }

    private void RegisterIndustrialRecycler(IDictionary<string, BigCraftableData> dict)
    {
        KeyValuePair<string, BigCraftableData>? templateOpt =
            FindBigCraftableTemplate(dict, targetDisplayName: "Recycling Machine", preferredKey: "20");

        if (templateOpt == null)
        {
            if (!loggedMissingRecyclerTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Recycling Machine template in Data/BigCraftables; Industrial Recycler was not added.", LogLevel.Error);
                loggedMissingRecyclerTemplate = true;
            }

            return;
        }

        BigCraftableData recyclerData = CloneJson(templateOpt.Value.Value);
        recyclerData.Name = IndustrialRecyclerRecipeKey;
        recyclerData.DisplayName = I18n.Get("item.industrial-recycler.name");
        recyclerData.Description = I18n.Get("item.industrial-recycler.description");
        recyclerData.Texture = IndustrialRecyclerTexture;
        recyclerData.SpriteIndex = 0;

        dict[PowerConstants.IndustrialRecyclerId] = recyclerData;
    }

    private void RegisterPoweredDehydrator(IDictionary<string, BigCraftableData> dict)
    {
        KeyValuePair<string, BigCraftableData>? templateOpt =
            FindBigCraftableTemplate(dict, targetDisplayName: "Dehydrator", preferredKey: null);

        if (templateOpt == null)
        {
            if (!loggedMissingDehydratorTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Dehydrator template in Data/BigCraftables; Powered Dehydrator was not added.", LogLevel.Error);
                loggedMissingDehydratorTemplate = true;
            }

            return;
        }

        vanillaDehydratorBigCraftableId = templateOpt.Value.Key;

        BigCraftableData dehydratorData = CloneJson(templateOpt.Value.Value);
        dehydratorData.Name = PoweredDehydratorRecipeKey;
        dehydratorData.DisplayName = I18n.Get("item.powered-dehydrator.name");
        dehydratorData.Description = I18n.Get("item.powered-dehydrator.description");
        dehydratorData.Texture = PoweredDehydratorTexture;
        dehydratorData.SpriteIndex = 0;

        dict[PowerConstants.PoweredDehydratorId] = dehydratorData;
    }


    private void RegisterBigCraftable(IDictionary<string, BigCraftableData> dict, BigCraftableData template,
        string itemId, string internalName, string displayName, string description, string textureAsset, bool passable = false, int spriteIndex = 0)
    {
        BigCraftableData data = CloneJson(template);
        data.DisplayName = displayName;
        data.Name = internalName;
        data.Description = description;
        data.Texture = textureAsset;
        data.SpriteIndex = spriteIndex;

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
        // Energized Iridium Cable: 337 (Iridium Bar) x4, 910 (Radioactive Bar) x1, 787 (Battery Pack) x1, 338 (Refined Quartz) x2 => 10 cables
        dict["Energized Iridium Cable"] = $"337 4 910 1 787 1 338 2/Field/{PowerConstants.EnergizedIridiumCableId} 10/true/null/";

        // Biofuel: 771 (Fiber) x10, 388 (Wood) x5, 382 (Coal) x1 => 8 Biofuel
        dict[BiofuelRecipeKey] = $"771 10 388 5 382 1/Field/{PowerConstants.BiofuelId} 8/false/null/";
        // Radioisotope Fuel: 910 (Radioactive Bar) x1, 338 (Refined Quartz) x1 => 7 Radioisotope Fuel
        dict[RadioisotopeFuelRecipeKey] = $"910 1 338 1/Field/{PowerConstants.RadioisotopeFuelId} 7/false/null/";
        // Heating Coil: 336 (Gold Bar) x3, 338 (Refined Quartz) x2, 382 (Coal) x6
        dict[HeatingCoilRecipeKey] = $"336 3 338 2 382 6/Field/{PowerConstants.HeatingCoilId}/false/null/";
        // Efficiency Core: 338 (Refined Quartz) x4, 787 (Battery Pack) x1, 336 (Gold Bar) x2
        dict[EfficiencyCoreRecipeKey] = $"338 4 787 1 336 2/Field/{PowerConstants.EfficiencyCoreId}/false/null/";
        // Catalyst Chamber: 337 (Iridium Bar) x2, 336 (Gold Bar) x4, 338 (Refined Quartz) x4
        dict[CatalystChamberRecipeKey] = $"337 2 336 4 338 4/Field/{PowerConstants.CatalystChamberId}/false/null/";
        // Sorting Magnet: 335 (Iron Bar) x4, 338 (Refined Quartz) x2
        dict[SortingMagnetRecipeKey] = $"335 4 338 2/Field/{PowerConstants.SortingMagnetId}/false/null/";
        // Drying Rack Array: 388 (Wood) x20, 709 (Hardwood) x4, 336 (Gold Bar) x1
        dict[DryingRackArrayRecipeKey] = $"388 20 709 4 336 1/Field/{PowerConstants.DryingRackArrayId}/false/null/";
        // Heat Regulator: 336 (Gold Bar) x2, 338 (Refined Quartz) x2, 382 (Coal) x4
        dict[HeatRegulatorRecipeKey] = $"336 2 338 2 382 4/Field/{PowerConstants.HeatRegulatorId}/false/null/";

        // Steam Generator: 335 (Iron Bar) x5, 334 (Copper Bar) x2, 382 (Coal) x6, 338 (Refined Quartz) x1
        dict["Steam Generator"] = $"335 5 334 2 382 6 338 1/Field/{PowerConstants.SteamGeneratorId}/true/null/";
        // Combustion Generator: Steam Generator x1, Iron Bar x8, Gold Bar x5, Refined Quartz x3
        dict[CombustionGeneratorRecipeKey] = $"{PowerConstants.SteamGeneratorId} 1 335 8 336 5 338 3/Field/{PowerConstants.CombustionGeneratorId}/true/null/";
        // Radioisotope Generator: Combustion Generator x1, Radioactive Bar x3, Iridium Bar x6, Iridium Power Battery x1, Refined Quartz x4
        dict[RadioisotopeGeneratorRecipeKey] = $"{PowerConstants.CombustionGeneratorId} 1 910 3 337 6 {PowerConstants.IridiumBatteryId} 1 338 4/Field/{PowerConstants.RadioisotopeGeneratorId}/true/null/";
        // Wind Generator: 335 (Iron Bar) x6, 338 (Refined Quartz) x4, 787 (Battery Pack) x1, 382 (Coal) x4
        dict["Wind Generator"] = $"335 6 338 4 787 1 382 4/Home/{PowerConstants.WindGeneratorId}/true/null/";

        // Basic Power Battery: 787 (Battery Pack) x1, 334 (Copper Bar) x4, 338 (Refined Quartz) x1
        dict["Basic Power Battery"] = $"787 1 334 4 338 1/Home/{PowerConstants.BasicBatteryId}/true/null/";
        // Iridium Power Battery: 787 (Battery Pack) x2, 337 (Iridium Bar) x2, 338 (Refined Quartz) x3
        dict["Iridium Power Battery"] = $"787 2 337 2 338 3/Home/{PowerConstants.IridiumBatteryId}/true/null/";

        // Power Conduit: 337 (Iridium Bar) x1, 787 (Battery Pack) x1, 338 (Refined Quartz) x1
        dict["Power Conduit"] = $"337 1 787 1 338 1/Home/{PowerConstants.PowerConduitId}/true/null/";

        string? preservesTemplate = dict.TryGetValue("Preserves Jar", out string? value) ? value : null;
        dict[IndustrialPreservesJarRecipeKey] = BuildRecipeFromTemplate(
            preservesTemplate,
            ingredients: "388 30 382 4 335 4 338 1",
            resultItemId: PowerConstants.IndustrialPreservesJarId);

        string? caskTemplate = dict.TryGetValue("Cask", out string? caskValue) ? caskValue : null;
        dict[MetalCaskRecipeKey] = BuildRecipeFromTemplate(
            caskTemplate,
            ingredients: "709 8 335 6 337 2 338 1",
            resultItemId: PowerConstants.MetalCaskId);

        string? kegTemplate = dict.TryGetValue("Keg", out string? kegValue) ? kegValue : null;
        dict[MetalKegRecipeKey] = BuildRecipeFromTemplate(
            kegTemplate,
            ingredients: "335 6 334 4 338 1",
            resultItemId: PowerConstants.MetalKegId);

        string? hardwoodKegTemplate = dict.TryGetValue("Hardwood Keg", out string? hardwoodValue) ? hardwoodValue : null;
        dict[HardIridiumKegRecipeKey] = BuildRecipeFromTemplate(
            hardwoodKegTemplate ?? kegTemplate,
            ingredients: "337 4 335 2 338 1",
            resultItemId: PowerConstants.HardIridiumKegId);

        dict[ElectricSmelterRecipeKey] = $"335 8 336 4 338 3 787 1/Field/{PowerConstants.ElectricSmelterId}/true/null/";
        dict[IndustrialRecyclerRecipeKey] = $"335 6 338 4 334 4 382 4/Field/{PowerConstants.IndustrialRecyclerId}/true/null/";
        dict[PoweredDehydratorRecipeKey] = $"335 6 709 10 338 3 787 1/Field/{PowerConstants.PoweredDehydratorId}/true/null/";
    }

    private void EditMachines(IAssetData asset)
    {
        IDictionary<string, MachineData> machines = asset.AsDictionary<string, MachineData>().Data;
        RegisterSteamGeneratorMachine(machines);
        RegisterCombustionGeneratorMachine(machines);
        RegisterRadioisotopeGeneratorMachine(machines);

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
        RegisterElectricSmelterMachine(machines);
        RegisterIndustrialRecyclerMachine(machines);
        RegisterPoweredDehydratorMachine(machines);
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

    private void RegisterRadioisotopeGeneratorMachine(IDictionary<string, MachineData> machines)
    {
        MachineData machine = CreateInputOnlyMachineData();
        machines[PowerConstants.Q(PowerConstants.RadioisotopeGeneratorId)] = machine;
        machines[PowerConstants.RadioisotopeGeneratorId] = machine;
    }

    private void RegisterElectricSmelterMachine(IDictionary<string, MachineData> machines)
    {
        MachineData machine = CreateInputOnlyMachineData();
        machines[PowerConstants.Q(PowerConstants.ElectricSmelterId)] = machine;
        machines[PowerConstants.ElectricSmelterId] = machine;
    }

    private void RegisterIndustrialRecyclerMachine(IDictionary<string, MachineData> machines)
    {
        MachineData machine = CreateInputOnlyMachineData();
        machines[PowerConstants.Q(PowerConstants.IndustrialRecyclerId)] = machine;
        machines[PowerConstants.IndustrialRecyclerId] = machine;
    }

    private void RegisterPoweredDehydratorMachine(IDictionary<string, MachineData> machines)
    {
        string? dehydratorKey = GetDehydratorMachineKey(machines, vanillaDehydratorBigCraftableId);
        if (dehydratorKey == null || !machines.TryGetValue(dehydratorKey, out MachineData? dehydratorMachine))
        {
            if (!loggedMissingDehydratorMachineTemplate)
            {
                Monitor.Log("[PowerGrid] Failed to find vanilla Dehydrator machine entry in Data/Machines; Powered Dehydrator machine rules were not added.", LogLevel.Error);
                loggedMissingDehydratorMachineTemplate = true;
            }

            return;
        }

        MachineData machine = CloneMachineByJson(dehydratorMachine);
        machines[PowerConstants.Q(PowerConstants.PoweredDehydratorId)] = machine;
        machines[PowerConstants.PoweredDehydratorId] = machine;
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
            Monitor.Log("[PowerGrid] Hardwood Keg machine template was not found in Data/Machines yet; Hard Iridium Keg will use vanilla Keg machine behavior until a compatible Hardwood Keg template is available.", LogLevel.Trace);
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

    private static string? GetDehydratorMachineKey(IDictionary<string, MachineData> machines, string? vanillaBigCraftableId)
    {
        if (!string.IsNullOrWhiteSpace(vanillaBigCraftableId))
        {
            string qualified = "(BC)" + vanillaBigCraftableId;
            if (machines.ContainsKey(qualified))
                return qualified;

            if (machines.ContainsKey(vanillaBigCraftableId))
                return vanillaBigCraftableId;
        }

        foreach (string key in machines.Keys)
        {
            if (key.Contains("Dehydrator", StringComparison.OrdinalIgnoreCase))
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
                string state = FormatConsumerPowerState(PowerConsumerPowerStatus.Classify(consumer, net));
                Monitor.Log($"    Consumer [{consumer.LocationName} @ {consumer.TileX},{consumer.TileY}] {consumer.ItemId}: state={state}, {consumer.EUAllocated}/{consumer.DemandPerTick} EU, speedup={consumer.SpeedupFraction:P0}, accel={consumer.MinutesAccelerated}min, {FormatConsumerProgress(consumer)}", LogLevel.Info);
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
            PowerNetworkSnapshot? network = networks.FirstOrDefault(candidate =>
                candidate.NetworkId == consumer.NetworkId
                && candidate.LocationNames.Any(name => string.Equals(name, consumer.LocationName, StringComparison.Ordinal)));
            string state = FormatConsumerPowerState(PowerConsumerPowerStatus.Classify(consumer, network));
            Monitor.Log($"  Consumer [{consumer.LocationName} @ {consumer.TileX},{consumer.TileY}] {consumer.DisplayName}: processing={consumer.IsProcessing}, state={state}, alloc={consumer.EUAllocated}/{consumer.DemandPerTick}, speedup={consumer.SpeedupFraction:P0}, {FormatConsumerProgress(consumer)}", LogLevel.Info);
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

    private static string FormatConsumerPowerState(PowerConsumerPowerState state)
    {
        return state switch
        {
            PowerConsumerPowerState.GridOffline => "grid-offline",
            PowerConsumerPowerState.Standby => "standby",
            PowerConsumerPowerState.Powered => "powered",
            PowerConsumerPowerState.LowPower => "low-power",
            PowerConsumerPowerState.ProcessingUnpowered => "processing-unpowered",
            _ => "not-connected"
        };
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
        CombustionGeneratorRecipeKey,
        ElectricSmelterRecipeKey,
        HeatingCoilRecipeKey,
        EfficiencyCoreRecipeKey,
        CatalystChamberRecipeKey,
        IndustrialRecyclerRecipeKey,
        SortingMagnetRecipeKey,
        PoweredDehydratorRecipeKey,
        DryingRackArrayRecipeKey,
        HeatRegulatorRecipeKey
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

    private static readonly string[] HighDensityGridRecipeKeys =
    {
        "Energized Iridium Cable",
        RadioisotopeFuelRecipeKey,
        RadioisotopeGeneratorRecipeKey
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
                | GrantRecipeSet(player, AdvancedGridRecipeKeys)
                | GrantRecipeSet(player, HighDensityGridRecipeKeys);

        bool any = false;
        bool knowsLightningRod = player.craftingRecipes.ContainsKey("Lightning Rod");
        bool knowsKeg = player.craftingRecipes.ContainsKey("Keg");
        bool knowsPreservesJar = player.craftingRecipes.ContainsKey("Preserves Jar");
        bool knowsSolarPanel = player.craftingRecipes.ContainsKey("Solar Panel");
        bool knowsIridiumBattery = player.craftingRecipes.ContainsKey("Iridium Power Battery");

        if (player.MiningLevel >= 5 || knowsLightningRod)
            any |= GrantRecipeSet(player, GridStarterRecipeKeys);

        if (knowsPreservesJar || knowsKeg)
            any |= GrantRecipeSet(player, PoweredArtisanRecipeKeys);

        if (player.MiningLevel >= 7 && knowsLightningRod)
            any |= GrantRecipeSet(player, FuelTechRecipeKeys);

        if (player.MiningLevel >= 9 && knowsLightningRod)
            any |= GrantRecipeSet(player, AdvancedGridRecipeKeys);

        if (player.MiningLevel >= 10 && knowsLightningRod && knowsSolarPanel && knowsIridiumBattery)
            any |= GrantRecipeSet(player, HighDensityGridRecipeKeys);

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
            PowerConstants.RadioisotopeGeneratorId => fuel.QualifiedItemId == "(O)910"
                || fuel.QualifiedItemId == PowerConstants.QObject(PowerConstants.RadioisotopeFuelId),
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
                harmony.Patch(drawWithLayerTarget, prefix: new HarmonyMethod(typeof(ModEntry), nameof(StatefulObjectScreenDrawPrefix)));
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
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(SteamGeneratorDropInActionPrefix)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(PoweredDehydratorDropInActionPostfix)));
            }

            if (PerformToolActionMethod != null)
                harmony.Patch(PerformToolActionMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(BatteryToolActionPrefix)));

            if (PerformRemoveActionMethod != null)
                harmony.Patch(PerformRemoveActionMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(BatteryRemoveActionPrefix)));

            if (DropObjectMethod != null)
                harmony.Patch(DropObjectMethod, prefix: new HarmonyMethod(typeof(ModEntry), nameof(BatteryDropObjectPrefix)));

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

        Game1.showRedMessage(I18n.Get("message.metal-cask.placement-error"));
        __result = false;
        return false;
    }

    private static void MetalCaskPlacementPostfix(StardewValley.Object __instance, object[] __args, ref bool __result)
    {
        if (Instance == null || !__result || __instance == null)
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

        Instance.ApplyHeldBatteryChargeToPlacedObject(__instance, placedObject, location, tile);
        Instance.ApplyHeldMachineUpgradesToPlacedObject(__instance, placedObject);

        if (__instance.QualifiedItemId != PowerConstants.Q(PowerConstants.MetalCaskId))
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

    private static void BatteryToolActionPrefix(StardewValley.Object __instance)
    {
        if (Instance == null || __instance == null)
            return;

        GameLocation? location = __instance.Location ?? Game1.currentLocation;
        if (location == null)
            return;

        if (IsBatteryItem(__instance.ItemId ?? ""))
            Instance.QueuePendingBatteryCharge(location, __instance.TileLocation, __instance);

        if (IsUpgradeableMachineItem(__instance.ItemId ?? ""))
            Instance.QueuePendingMachineUpgrades(location, __instance.TileLocation, __instance);
    }

    private static void BatteryRemoveActionPrefix(StardewValley.Object __instance)
    {
        if (Instance == null || __instance == null)
            return;

        GameLocation? location = __instance.Location ?? Game1.currentLocation;
        if (location == null)
            return;

        if (IsBatteryItem(__instance.ItemId ?? ""))
            Instance.QueuePendingBatteryCharge(location, __instance.TileLocation, __instance);

        if (IsUpgradeableMachineItem(__instance.ItemId ?? ""))
            Instance.QueuePendingMachineUpgrades(location, __instance.TileLocation, __instance);
    }

    private static void BatteryDropObjectPrefix(GameLocation __instance, StardewValley.Object obj)
    {
        if (Instance == null || __instance == null || obj == null)
            return;

        if (IsBatteryItem(obj.ItemId ?? ""))
            Instance.CopyPendingBatteryChargeToItem(obj);

        if (IsUpgradeableMachineItem(obj.ItemId ?? ""))
            Instance.CopyPendingMachineUpgradesToItem(obj);
    }

    private static bool SteamGeneratorDropInActionPrefix(StardewValley.Object __instance, Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed, ref bool __result)
    {
        if (Instance != null
            && __instance != null
            && dropInItem is StardewValley.Object machineInput
            && TryHandleIndustrialMachineDropIn(__instance, machineInput, probe, ref __result))
        {
            return false;
        }

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
            || itemId == PowerConstants.IridiumCableId
            || itemId == PowerConstants.EnergizedIridiumCableId)
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

    private static bool StatefulObjectScreenDrawPrefix(StardewValley.Object __instance, object[] __args)
    {
        if (Instance == null || __args.Length < 5 || __args[0] is not SpriteBatch spriteBatch)
            return true;

        if (IsCableItem(__instance.ItemId))
            return false;

        if (!IsStatefulPowerGridItem(__instance.ItemId))
            return true;

        if (__args[1] is not int xNonTile || __args[2] is not int yNonTile)
            return true;

        float? layerDepth = __args[3] is float explicitLayerDepth ? explicitLayerDepth : null;
        float alpha = __args[4] is float explicitAlpha ? explicitAlpha : 1f;
        Instance.LogConduitRenderDiagnostic(__instance, "screen-prefix", __args);
        return !Instance.TryDrawStatefulScreenReplacement(__instance, spriteBatch, xNonTile, yNonTile, alpha, layerDepth);
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

    private bool TryDrawStatefulScreenReplacement(StardewValley.Object obj, SpriteBatch spriteBatch, int xNonTile, int yNonTile, float alpha, float? layerDepthOverride)
    {
        if (!Context.IsWorldReady || Game1.currentLocation == null || obj == null || !obj.bigCraftable.Value)
            return false;

        Vector2 tile = obj.TileLocation;
        int tileX = (int)tile.X;
        int tileY = (int)tile.Y;

        if (Game1.currentLocation.getObjectAtTile(tileX, tileY) != obj)
            return false;

        if (!TryGetStatefulSpriteName(obj, tile, out string? stateSpriteName))
            return false;

        if (!TryLoadStateTextureForDraw(stateSpriteName!, out Texture2D? texture))
            return false;

        Vector2 screenPos = new(xNonTile, yNonTile);
        float layerDepth = GetStatefulObjectLayerDepth(obj, tileX, tileY, layerDepthOverride);

        DrawStatefulTexture(spriteBatch, texture!, screenPos, alpha, layerDepth, obj, stateSpriteName!);
        return true;
    }

    private void DrawStatefulTexture(SpriteBatch spriteBatch, Texture2D texture, Vector2 screenPos, float alpha, float layerDepth, StardewValley.Object obj, string stateSpriteName)
    {
        Rectangle? animatedDestination = null;
        if (IsAnimatedStatefulBigCraftable(obj.ItemId))
        {
            animatedDestination = GetAnimatedBigCraftableDestination(screenPos, obj);
            DrawAnimatedBigCraftableTexture(spriteBatch, texture, animatedDestination.Value, alpha, layerDepth);
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
        DrawElectricSmelterProcessingOverlay(spriteBatch, screenPos, animatedDestination, alpha, layerDepth, obj, stateSpriteName);
        DrawWindGeneratorWheelOverlay(spriteBatch, screenPos, alpha, layerDepth, obj, stateSpriteName);
        DrawStatefulReadyIndicator(spriteBatch, alpha, layerDepth, obj);
    }

    private static bool IsAnimatedStatefulBigCraftable(string? itemId)
    {
        return itemId == PowerConstants.IndustrialPreservesJarId
            || itemId == PowerConstants.MetalCaskId
            || itemId == PowerConstants.MetalKegId
            || itemId == PowerConstants.HardIridiumKegId
            || itemId == PowerConstants.ElectricSmelterId
            || itemId == PowerConstants.IndustrialRecyclerId
            || itemId == PowerConstants.PoweredDehydratorId;
    }

    private static bool UsesStatefulReadyIndicator(string? itemId)
    {
        return itemId == PowerConstants.IndustrialPreservesJarId
            || itemId == PowerConstants.MetalCaskId
            || itemId == PowerConstants.MetalKegId
            || itemId == PowerConstants.HardIridiumKegId
            || itemId == PowerConstants.ElectricSmelterId
            || itemId == PowerConstants.IndustrialRecyclerId
            || itemId == PowerConstants.PoweredDehydratorId;
    }

    private static Rectangle GetAnimatedBigCraftableDestination(Vector2 screenPos, StardewValley.Object obj)
    {
        Vector2 drawScale = obj.getScale() * 4f;
        int shakeX = obj.shakeTimer > 0 ? Game1.random.Next(-1, 2) : 0;
        int shakeY = obj.shakeTimer > 0 ? Game1.random.Next(-1, 2) : 0;

        return new Rectangle(
            (int)(screenPos.X - drawScale.X / 2f) + shakeX,
            (int)(screenPos.Y - drawScale.Y / 2f) + shakeY,
            Math.Max(1, (int)(64f + drawScale.X)),
            Math.Max(1, (int)(128f + drawScale.Y / 2f)));
    }

    private static void DrawAnimatedBigCraftableTexture(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination, float alpha, float layerDepth)
    {
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
        int motionFrame = ((int)(totalSeconds * 4f)) % 3;
        float overlayDepth = Math.Min(1f, layerDepth + SteamActiveOverlayLayerOffset);

        Rectangle unitSrc = new(0, 0, 1, 1);
        bool isCombustion = obj.ItemId == PowerConstants.CombustionGeneratorId;
        bool isRadioisotope = obj.ItemId == PowerConstants.RadioisotopeGeneratorId;
        if (isCombustion)
            DrawCombustionFluidOverlay(spriteBatch, screenPos, alpha, motionFrame, overlayDepth);
        else if (isRadioisotope)
            DrawRadioisotopeCoreOverlay(spriteBatch, screenPos, alpha, motionFrame, overlayDepth);
        else
        {
            Vector2 fireboxCenter = new(screenPos.X + 22f + flicker, screenPos.Y + 90f);
            Color outerGlow = new Color(255, 160, 72) * Math.Min(1f, alpha * (0.32f + pulse * 0.28f));
            Color midGlow = new Color(255, 198, 96) * Math.Min(1f, alpha * (0.5f + pulse * 0.25f));
            Color coreGlow = new Color(255, 236, 156) * Math.Min(1f, alpha * (0.75f + pulse * 0.2f));

            spriteBatch.Draw(Game1.staminaRect, new Vector2(fireboxCenter.X - 7f, fireboxCenter.Y - 4f), unitSrc, outerGlow, 0f, Vector2.Zero, new Vector2(14f, 8f), SpriteEffects.None, overlayDepth);
            spriteBatch.Draw(Game1.staminaRect, new Vector2(fireboxCenter.X - 5f, fireboxCenter.Y - 3f), unitSrc, midGlow, 0f, Vector2.Zero, new Vector2(10f, 6f), SpriteEffects.None, overlayDepth);
            spriteBatch.Draw(Game1.staminaRect, new Vector2(fireboxCenter.X - 2f, fireboxCenter.Y - 1f), unitSrc, coreGlow, 0f, Vector2.Zero, new Vector2(5f, 3f), SpriteEffects.None, overlayDepth);
        }

        // Steam puffs near the stack to make active state readable at a glance.
        Color steamColor = new Color(224, 230, 236) * Math.Min(1f, alpha * (0.62f + pulse * 0.58f));
        float stackCenterX = screenPos.X + 25f;

        spriteBatch.Draw(
            Game1.staminaRect,
            new Vector2(stackCenterX - 4f, screenPos.Y + 10f - drift * 2f),
            unitSrc,
            steamColor,
            0f,
            Vector2.Zero,
            new Vector2(8f, 4f),
            SpriteEffects.None,
            overlayDepth);

        spriteBatch.Draw(
            Game1.staminaRect,
            new Vector2(stackCenterX - 3f, screenPos.Y + 4f - drift * 3f),
            unitSrc,
            steamColor * 0.85f,
            0f,
            Vector2.Zero,
            new Vector2(6f, 3f),
            SpriteEffects.None,
            overlayDepth);

        spriteBatch.Draw(
            Game1.staminaRect,
            new Vector2(stackCenterX - 1f, screenPos.Y - 1f - drift * 2.2f),
            unitSrc,
            steamColor * 0.95f,
            0f,
            Vector2.Zero,
            new Vector2(3f, 2f),
            SpriteEffects.None,
            overlayDepth);
    }

    private static void DrawCombustionFluidOverlay(SpriteBatch spriteBatch, Vector2 screenPos, float alpha, int motionFrame, float overlayDepth)
    {
        Color deepFuel = new Color(86, 55, 26) * alpha;
        Color midFuel = new Color(141, 91, 36) * alpha;
        Color warmFuel = new Color(186, 144, 39) * alpha;
        Color hotFuel = new Color(211, 148, 61) * alpha;

        switch (motionFrame)
        {
            case 0:
                DrawOverlayPixel(spriteBatch, screenPos, 4, 20, 1, 1, hotFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 20, 1, 1, midFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 4, 21, 1, 1, midFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 21, 1, 1, warmFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 4, 22, 1, 1, hotFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 22, 1, 1, midFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 4, 23, 1, 1, midFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 23, 1, 1, deepFuel, overlayDepth);
                break;
            case 1:
                DrawOverlayPixel(spriteBatch, screenPos, 4, 20, 1, 1, warmFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 20, 1, 1, deepFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 4, 21, 1, 1, hotFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 21, 1, 1, midFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 4, 22, 1, 1, warmFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 22, 1, 1, hotFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 4, 23, 1, 1, deepFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 23, 1, 1, midFuel, overlayDepth);
                break;
            default:
                DrawOverlayPixel(spriteBatch, screenPos, 4, 20, 1, 1, midFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 20, 1, 1, warmFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 4, 21, 1, 1, warmFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 21, 1, 1, hotFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 4, 22, 1, 1, midFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 22, 1, 1, warmFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 4, 23, 1, 1, hotFuel, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 5, 23, 1, 1, deepFuel, overlayDepth);
                break;
        }
    }

    private static void DrawRadioisotopeCoreOverlay(SpriteBatch spriteBatch, Vector2 screenPos, float alpha, int motionFrame, float overlayDepth)
    {
        Color deepGreen = new Color(0, 198, 70) * alpha;
        Color vividGreen = new Color(0, 255, 3) * alpha;
        Color brightGreen = new Color(136, 255, 3) * alpha;
        Color hotGreen = new Color(209, 255, 3) * alpha;

        switch (motionFrame)
        {
            case 0:
                DrawOverlayPixel(spriteBatch, screenPos, 7, 20, 1, 1, hotGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 20, 1, 1, hotGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 6, 21, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 7, 21, 1, 1, brightGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 21, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 9, 21, 1, 1, hotGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 6, 22, 1, 1, deepGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 7, 22, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 22, 1, 1, brightGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 9, 22, 1, 1, brightGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 7, 23, 1, 1, deepGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 23, 1, 1, vividGreen, overlayDepth);
                break;
            case 1:
                DrawOverlayPixel(spriteBatch, screenPos, 7, 20, 1, 1, brightGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 20, 1, 1, hotGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 6, 21, 1, 1, deepGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 7, 21, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 21, 1, 1, brightGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 9, 21, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 6, 22, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 7, 22, 1, 1, brightGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 22, 1, 1, hotGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 9, 22, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 7, 23, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 23, 1, 1, deepGreen, overlayDepth);
                break;
            default:
                DrawOverlayPixel(spriteBatch, screenPos, 7, 20, 1, 1, hotGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 20, 1, 1, brightGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 6, 21, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 7, 21, 1, 1, hotGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 21, 1, 1, brightGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 9, 21, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 6, 22, 1, 1, deepGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 7, 22, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 22, 1, 1, vividGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 9, 22, 1, 1, hotGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 7, 23, 1, 1, brightGreen, overlayDepth);
                DrawOverlayPixel(spriteBatch, screenPos, 8, 23, 1, 1, vividGreen, overlayDepth);
                break;
        }
    }

    private static void DrawElectricSmelterProcessingOverlay(SpriteBatch spriteBatch, Vector2 screenPos, Rectangle? animatedDestination, float alpha, float layerDepth, StardewValley.Object obj, string stateSpriteName)
    {
        if (obj.ItemId != PowerConstants.ElectricSmelterId || !stateSpriteName.EndsWith("__processing_powered", StringComparison.Ordinal))
            return;

        Rectangle destination = animatedDestination ?? new Rectangle((int)screenPos.X, (int)screenPos.Y, 64, 128);
        double totalSeconds = Game1.currentGameTime?.TotalGameTime.TotalSeconds ?? 0d;
        bool startup = obj.shakeTimer > 0;
        float pulse = 0.5f + ((float)Math.Sin(totalSeconds * 5.5f) + 1f) * 0.25f;
        float glowStrength = startup
            ? 0.48f + pulse * 0.24f
            : 0.28f + pulse * 0.16f;
        int moltenFrame = ((int)(totalSeconds * 6f)) % 4;
        int rodFrame = ((int)(totalSeconds * 2.5f)) % 4;
        float overlayDepth = Math.Min(1f, layerDepth + SteamActiveOverlayLayerOffset);

        DrawElectricSmelterGlowMask(spriteBatch, destination, alpha, glowStrength, overlayDepth);
        DrawElectricSmelterTopOverlay(spriteBatch, destination, alpha, rodFrame, overlayDepth);
        DrawElectricSmelterRodOverlay(spriteBatch, destination, alpha, rodFrame, overlayDepth);
        DrawElectricSmelterMoltenOverlay(spriteBatch, destination, alpha, moltenFrame, overlayDepth);
    }

    private static void DrawElectricSmelterGlowMask(SpriteBatch spriteBatch, Rectangle destination, float alpha, float glowStrength, float overlayDepth)
    {
        Color outerGlow = new Color(255, 104, 24) * Math.Min(1f, alpha * glowStrength);
        Color innerGlow = new Color(255, 178, 58) * Math.Min(1f, alpha * glowStrength * 0.78f);

        DrawOverlayPixel(spriteBatch, destination, 5, 17, 6, 1, outerGlow, overlayDepth);
        for (int y = 18; y <= 25; y++)
            DrawOverlayPixel(spriteBatch, destination, 4, y, 8, 1, y is >= 19 and <= 23 ? innerGlow : outerGlow, overlayDepth);
    }

    private static void DrawElectricSmelterTopOverlay(SpriteBatch spriteBatch, Rectangle destination, float alpha, int rodFrame, float overlayDepth)
    {
        Color topA = new Color(255, 162, 44) * Math.Min(1f, alpha * (0.56f + rodFrame * 0.035f));
        Color topB = new Color(255, 214, 90) * Math.Min(1f, alpha * (0.48f + (3 - rodFrame) * 0.03f));
        Color topSide = new Color(255, 126, 32) * Math.Min(1f, alpha * (0.42f + rodFrame * 0.025f));

        DrawOverlayPixel(spriteBatch, destination, 6, 6, 1, 1, rodFrame % 2 == 0 ? topSide : topA, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 7, 6, 1, 1, topA, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 8, 6, 1, 1, topB, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 9, 6, 1, 1, rodFrame % 2 == 0 ? topB : topSide, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 6, 8, 1, 1, rodFrame % 2 == 0 ? topA : topSide, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 7, 8, 1, 1, rodFrame % 2 == 0 ? topB : topA, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 8, 8, 1, 1, rodFrame % 2 == 0 ? topA : topB, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 9, 8, 1, 1, rodFrame % 2 == 0 ? topSide : topB, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 6, 10, 1, 1, rodFrame % 2 == 0 ? topSide : topB, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 7, 10, 1, 1, topA, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 8, 10, 1, 1, topB, overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 9, 10, 1, 1, rodFrame % 2 == 0 ? topA : topSide, overlayDepth);
    }

    private static void DrawElectricSmelterRodOverlay(SpriteBatch spriteBatch, Rectangle destination, float alpha, int rodFrame, float overlayDepth)
    {
        Color rodA = new Color(255, 150, 36) * Math.Min(1f, alpha * (0.66f + rodFrame * 0.025f));
        Color rodB = new Color(255, 216, 84) * Math.Min(1f, alpha * (0.58f + (3 - rodFrame) * 0.02f));

        for (int y = 17; y <= 21; y++)
        {
            bool alternate = (y + rodFrame) % 2 == 0;
            DrawOverlayPixel(spriteBatch, destination, 6, y, 1, 1, alternate ? rodA : rodB, overlayDepth);
            DrawOverlayPixel(spriteBatch, destination, 9, y, 1, 1, alternate ? rodB : rodA, overlayDepth);
        }
    }

    private static void DrawElectricSmelterMoltenOverlay(SpriteBatch spriteBatch, Rectangle destination, float alpha, int moltenFrame, float overlayDepth)
    {
        Color redMolten = new Color(196, 64, 18) * alpha;
        Color orangeMolten = new Color(248, 116, 24) * alpha;
        Color hotMolten = new Color(255, 192, 72) * alpha;
        Color[] palette = moltenFrame switch
        {
            0 => new[] { hotMolten, orangeMolten, redMolten, orangeMolten },
            1 => new[] { orangeMolten, hotMolten, orangeMolten, redMolten },
            2 => new[] { redMolten, orangeMolten, hotMolten, orangeMolten },
            _ => new[] { orangeMolten, redMolten, orangeMolten, hotMolten }
        };

        DrawOverlayPixel(spriteBatch, destination, 4, 24, 1, 1, palette[1], overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 6, 24, 1, 1, palette[2], overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 7, 24, 1, 1, palette[3], overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 9, 24, 1, 1, palette[0], overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 11, 24, 1, 1, palette[2], overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 4, 25, 1, 1, palette[2], overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 6, 25, 1, 1, palette[3], overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 8, 25, 1, 1, palette[0], overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 9, 25, 1, 1, palette[1], overlayDepth);
        DrawOverlayPixel(spriteBatch, destination, 11, 25, 1, 1, palette[3], overlayDepth);
    }

    private static void DrawOverlayPixel(SpriteBatch spriteBatch, Vector2 screenPos, int spriteX, int spriteY, int widthPixels, int heightPixels, Color color, float layerDepth)
    {
        Vector2 pos = new(screenPos.X + spriteX * 4f, screenPos.Y + spriteY * 4f);
        Vector2 scale = new(widthPixels * 4f, heightPixels * 4f);
        spriteBatch.Draw(Game1.staminaRect, pos, new Rectangle(0, 0, 1, 1), color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
    }

    private static void DrawOverlayPixel(SpriteBatch spriteBatch, Rectangle destination, int spriteX, int spriteY, int widthPixels, int heightPixels, Color color, float layerDepth)
    {
        Vector2 pos = SpritePixelToScreen(destination, spriteX, spriteY);
        Vector2 scale = new(destination.Width * (widthPixels / 16f), destination.Height * (heightPixels / 32f));
        spriteBatch.Draw(Game1.staminaRect, pos, new Rectangle(0, 0, 1, 1), color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
    }

    private static Vector2 SpritePixelToScreen(Rectangle destination, float spriteX, float spriteY)
    {
        return new Vector2(
            destination.X + destination.Width * (spriteX / 16f),
            destination.Y + destination.Height * (spriteY / 32f));
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
            || itemId == PowerConstants.RadioisotopeGeneratorId
            || itemId == PowerConstants.WindGeneratorId
            || itemId == PowerConstants.BasicBatteryId
            || itemId == PowerConstants.IridiumBatteryId
            || itemId == PowerConstants.PowerConduitId
            || itemId == PowerConstants.IndustrialPreservesJarId
            || itemId == PowerConstants.MetalCaskId
            || itemId == PowerConstants.MetalKegId
            || itemId == PowerConstants.HardIridiumKegId
            || itemId == PowerConstants.ElectricSmelterId
            || itemId == PowerConstants.IndustrialRecyclerId
            || itemId == PowerConstants.PoweredDehydratorId;
    }

    private static bool IsCableItem(string? itemId)
    {
        return itemId == PowerConstants.CopperCableId
            || itemId == PowerConstants.IronCableId
            || itemId == PowerConstants.IridiumCableId
            || itemId == PowerConstants.EnergizedIridiumCableId;
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
            PowerConstants.RadioisotopeGeneratorId => GetRadioisotopeGeneratorState(locationName, tile),
            PowerConstants.WindGeneratorId => GetWindGeneratorState(obj),
            PowerConstants.BasicBatteryId => GetBatteryState(locationName, tile, itemId, obj),
            PowerConstants.IridiumBatteryId => GetBatteryState(locationName, tile, itemId, obj),
            PowerConstants.PowerConduitId => GetConduitState(locationName, tile),
            PowerConstants.IndustrialPreservesJarId => GetIndustrialPreservesJarState(obj),
            PowerConstants.MetalCaskId => GetPoweredMachineState(obj),
            PowerConstants.MetalKegId => GetPoweredMachineState(obj),
            PowerConstants.HardIridiumKegId => GetPoweredMachineState(obj),
            PowerConstants.ElectricSmelterId => GetElectricSmelterState(obj),
            PowerConstants.IndustrialRecyclerId => GetPoweredMachineState(obj),
            PowerConstants.PoweredDehydratorId => GetPoweredMachineState(obj),
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

    private string? GetRadioisotopeGeneratorState(string locationName, Vector2 tile)
    {
        if (string.IsNullOrWhiteSpace(locationName))
            return null;

        return GetFueledGeneratorState(locationName, tile, PowerConstants.RadioisotopeGeneratorId);
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

    private void RefreshElectricSmelters(bool enforcePowerLoss = false)
    {
        foreach (GameLocation location in EnumerateLoadedLocations())
            RefreshElectricSmelters(location, enforcePowerLoss);
    }

    private void RefreshElectricSmelters(GameLocation location, bool enforcePowerLoss = false)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.objects.Pairs)
        {
            StardewValley.Object obj = pair.Value;
            if (obj.ItemId != PowerConstants.ElectricSmelterId)
                continue;

            if (enforcePowerLoss && obj.MinutesUntilReady > 0 && !IsElectricSmelterActivelyPowered(location, pair.Key, obj))
            {
                CancelElectricSmelterProcessing(location, pair.Key, obj);
                continue;
            }

            obj.readyForHarvest.Value = obj.heldObject.Value != null && obj.MinutesUntilReady <= 0;
        }
    }

    private bool IsElectricSmelterActivelyPowered(GameLocation location, Vector2 tile, StardewValley.Object smelter)
    {
        PowerConsumerSnapshot? consumer = PowerQuery.GetConsumerSnapshots(location.NameOrUniqueName)
            .FirstOrDefault(candidate =>
                candidate.TileX == (int)tile.X
                && candidate.TileY == (int)tile.Y
                && string.Equals(candidate.ItemId, PowerConstants.ElectricSmelterId, StringComparison.Ordinal));

        if (consumer == null)
            return false;

        PowerNetworkSnapshot? network = PowerQuery.GetNetworkSnapshots()
            .FirstOrDefault(candidate =>
                candidate.NetworkId == consumer.NetworkId
                && candidate.LocationNames.Any(name => string.Equals(name, consumer.LocationName, StringComparison.Ordinal)));

        PowerConsumerPowerState state = PowerConsumerPowerStatus.Classify(consumer, network);
        if (state is PowerConsumerPowerState.Powered or PowerConsumerPowerState.LowPower)
            return true;

        return smelter.modData.TryGetValue("meiameiameia.PowerGrid/powered", out string? powered)
            && powered == "1";
    }

    private void CancelDisconnectedElectricSmelters(GameLocation location)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.objects.Pairs)
        {
            StardewValley.Object obj = pair.Value;
            if (obj.ItemId != PowerConstants.ElectricSmelterId || obj.MinutesUntilReady <= 0)
                continue;

            if (HasPowerGridInfrastructureConnection(location, pair.Key, PowerConstants.ElectricSmelterId))
                continue;

            CancelElectricSmelterProcessing(location, pair.Key, obj);
        }
    }

    private void CancelElectricSmelterProcessing(GameLocation location, Vector2 tile, StardewValley.Object smelter)
    {
        if (TryGetElectricSmelterInputReturn(smelter, out string inputQualifiedItemId, out int inputCount))
        {
            StardewValley.Object returnedInput = ItemRegistry.Create<StardewValley.Object>(inputQualifiedItemId);
            returnedInput.Stack = Math.Max(1, inputCount);
            Vector2 dropPosition = new(tile.X * Game1.tileSize + Game1.tileSize / 2f, tile.Y * Game1.tileSize + Game1.tileSize / 2f);
            Game1.createItemDebris(returnedInput, dropPosition, -1, location);
        }

        smelter.heldObject.Value = null;
        smelter.MinutesUntilReady = 0;
        smelter.readyForHarvest.Value = false;
        ClearElectricSmelterInputData(smelter);
    }

    private static bool TryGetElectricSmelterInputReturn(StardewValley.Object smelter, out string inputQualifiedItemId, out int inputCount)
    {
        inputQualifiedItemId = "";
        inputCount = 0;

        if (smelter.modData.TryGetValue(ElectricSmelterInputItemKey, out string? rawInput)
            && !string.IsNullOrWhiteSpace(rawInput)
            && smelter.modData.TryGetValue(ElectricSmelterInputCountKey, out string? rawCount)
            && int.TryParse(rawCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedCount)
            && parsedCount > 0)
        {
            inputQualifiedItemId = rawInput;
            inputCount = parsedCount;
            return true;
        }

        string? outputQualifiedItemId = smelter.heldObject.Value?.QualifiedItemId;
        if (string.IsNullOrWhiteSpace(outputQualifiedItemId))
            return false;

        ElectricSmelterRecipe? recipe = ElectricSmelterRecipes.Values
            .FirstOrDefault(candidate => string.Equals(candidate.OutputQualifiedItemId, outputQualifiedItemId, StringComparison.OrdinalIgnoreCase));
        if (recipe == null)
            return false;

        inputQualifiedItemId = recipe.InputQualifiedItemId;
        inputCount = recipe.InputCount;
        return true;
    }

    private static void ClearElectricSmelterInputData(StardewValley.Object smelter)
    {
        smelter.modData.Remove(ElectricSmelterInputItemKey);
        smelter.modData.Remove(ElectricSmelterInputCountKey);
    }

    private void RefreshIndustrialRecyclers()
    {
        foreach (GameLocation location in EnumerateLoadedLocations())
            RefreshIndustrialRecyclers(location);
    }

    private static void RefreshIndustrialRecyclers(GameLocation location)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.objects.Pairs)
        {
            StardewValley.Object obj = pair.Value;
            if (obj.ItemId != PowerConstants.IndustrialRecyclerId)
                continue;

            obj.readyForHarvest.Value = obj.heldObject.Value != null && obj.MinutesUntilReady <= 0;
        }
    }

    private void RefreshPoweredDehydrators()
    {
        foreach (GameLocation location in EnumerateLoadedLocations())
            RefreshPoweredDehydrators(location);
    }

    private static void RefreshPoweredDehydrators(GameLocation location)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.objects.Pairs)
        {
            StardewValley.Object obj = pair.Value;
            if (obj.ItemId != PowerConstants.PoweredDehydratorId)
                continue;

            obj.readyForHarvest.Value = obj.heldObject.Value != null && obj.MinutesUntilReady <= 0;
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

                if (obj.ItemId == PowerConstants.CombustionGeneratorId || obj.ItemId == PowerConstants.RadioisotopeGeneratorId)
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

    private void HandleRemovedBatteryState(GameLocation location, Vector2 tile, StardewValley.Object removedObj)
    {
        if (!TryBuildBatteryNode(location, tile, removedObj, out PowerNode batteryNode))
            return;

        QueuePendingBatteryCharge(location, tile, removedObj);
        BatteryState.RemoveTileState(batteryNode);
    }

    private void HandleRemovedMachineUpgradeState(GameLocation location, Vector2 tile, StardewValley.Object removedObj)
    {
        QueuePendingMachineUpgrades(location, tile, removedObj);
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
            case PowerConstants.RadioisotopeGeneratorId:
                qualifiedFuelId = "(O)910";
                ticksPerUnit = Math.Max(1, Config.RadioactiveBarFuelTicks);
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

    private string? GetBatteryState(string locationName, Vector2 tile, string itemId, StardewValley.Object obj)
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
            Capacity = capacity,
            SourceObject = obj
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

    private static string GetElectricSmelterState(StardewValley.Object obj)
    {
        bool energized = TryReadPowerModDataString(obj.modData, "energized", out string? energizedText)
            && energizedText == "1";
        bool powered = TryReadPowerModDataString(obj.modData, "powered", out string? poweredText)
            && poweredText == "1";

        if (obj.MinutesUntilReady > 0 && (powered || obj.shakeTimer > 0))
            return ElectricSmelterProcessingPoweredState;

        if (energized || powered)
            return ElectricSmelterStandbyState;

        return ElectricSmelterOfflineState;
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
            PowerConstants.RadioisotopeGeneratorId => "RadioisotopeGenerator",
            PowerConstants.WindGeneratorId => "WindGenerator",
            PowerConstants.BasicBatteryId => "BasicBattery",
            PowerConstants.IridiumBatteryId => "IridiumBattery",
            PowerConstants.PowerConduitId => "PowerConduit",
            PowerConstants.IndustrialPreservesJarId => IndustrialPreservesJarSpriteName,
            PowerConstants.MetalCaskId => MetalCaskSpriteName,
            PowerConstants.MetalKegId => MetalKegSpriteName,
            PowerConstants.HardIridiumKegId => HardIridiumKegSpriteName,
            PowerConstants.ElectricSmelterId => ElectricSmelterSpriteName,
            PowerConstants.IndustrialRecyclerId => IndustrialRecyclerSpriteName,
            PowerConstants.PoweredDehydratorId => PoweredDehydratorSpriteName,
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
        if (!Config.DebugOverlayEnabled)
            return;

        if (conduitRenderDiagnosticsLogged.Add(key))
            Monitor.Log(message, LogLevel.Trace);
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
        Game1.addHUDMessage(new HUDMessage(I18n.Get("message.generator.fuel-added", new { minutes = minutesAdded }), HUDMessage.newQuest_type));
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
        Game1.addHUDMessage(new HUDMessage(I18n.Get("message.generator.legacy-fuel-migrated", new { minutes = minutesAdded }), HUDMessage.newQuest_type));
        return true;
    }

    private static bool TryHandleIndustrialMachineDropIn(StardewValley.Object machine, StardewValley.Object input, bool probe, ref bool result)
    {
        if (Instance == null)
            return false;

        string itemId = machine.ItemId ?? "";
        GameLocation? location = machine.Location ?? Game1.currentLocation;
        Vector2 tile = machine.TileLocation;

        if (itemId == PowerConstants.ElectricSmelterId)
        {
            if (!ElectricSmelterRecipes.TryGetValue(input.QualifiedItemId, out ElectricSmelterRecipe? recipe))
                return false;

            result = machine.MinutesUntilReady <= 0
                && machine.heldObject.Value == null
                && input.Stack >= recipe.InputCount
                && location != null
                && Instance.HasPowerGridInfrastructureConnection(location, tile, PowerConstants.ElectricSmelterId);

            if (probe || !result || location == null)
                return true;

            result = Instance.TryStartElectricSmelter(location, tile, machine, input, showMessages: false, consumeInput: () => ReduceInputStack(input, recipe.InputCount));
            return true;
        }

        if (itemId == PowerConstants.IndustrialRecyclerId)
        {
            if (!IndustrialRecyclerRecipes.ContainsKey(input.QualifiedItemId))
                return false;

            result = machine.MinutesUntilReady <= 0
                && machine.heldObject.Value == null
                && input.Stack > 0;

            if (probe || !result)
                return true;

            result = Instance.TryStartIndustrialRecycler(machine, input, showMessages: false, consumeInput: () => ReduceInputStack(input, 1));
            return true;
        }

        return false;
    }

    private static void ReduceInputStack(StardewValley.Object input, int count)
    {
        input.Stack = Math.Max(0, input.Stack - Math.Max(0, count));
    }

    private static void PoweredDehydratorDropInActionPostfix(StardewValley.Object __instance, Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed, bool __result)
    {
        if (!__result
            || probe
            || __instance?.ItemId != PowerConstants.PoweredDehydratorId)
        {
            return;
        }

        if (MachineUpgradeState.HasUpgrade(__instance, PowerConstants.HeatRegulatorId)
            && __instance.MinutesUntilReady > PowerConstants.TickIntervalMinutes)
        {
            __instance.MinutesUntilReady = Math.Max(
                PowerConstants.TickIntervalMinutes,
                (int)MathF.Ceiling(__instance.MinutesUntilReady * PoweredDehydratorHeatRegulatorSpeedMultiplier));
        }

        if (MachineUpgradeState.HasUpgrade(__instance, PowerConstants.DryingRackArrayId)
            && __instance.heldObject.Value != null
            && Game1.random.NextDouble() < PoweredDehydratorDryingRackExtraOutputChance)
        {
            __instance.heldObject.Value.Stack++;
        }
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

    private sealed record PendingBatteryCharge(string LocationName, Vector2 Tile, string ItemId, int Charge);
    private sealed record PendingMachineUpgradeData(string LocationName, Vector2 Tile, string ItemId, string SerializedUpgradeIds);
}
