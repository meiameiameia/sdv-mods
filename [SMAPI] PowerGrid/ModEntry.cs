using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.Mods;
using Darth.PowerGrid.Core;
using Darth.PowerGrid.UI;
using Darth.PowerGrid.Integrations;

namespace Darth.PowerGrid;

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
    private const string PersistedModDataPrefix = "darth.PowerGrid/";
    private const string PersistedChargeKey = PersistedModDataPrefix + "charge";
    private const string PersistedFuelTicksRemainingKey = PersistedModDataPrefix + "fuelTicksRemaining";
    private const string PersistedLinkedKey = PersistedModDataPrefix + "linked";
    private const string PersistedPartnerLocationKey = PersistedModDataPrefix + "partnerLocation";
    private const string PersistedPartnerTileKey = PersistedModDataPrefix + "partnerTile";
    private const string RuntimeOnlineKey = PersistedModDataPrefix + "online";
    private const string RuntimeGeneratedThisTickKey = PersistedModDataPrefix + "generatedThisTick";
    private const float ChargedBatteryThreshold = 0.5f;
    private static readonly Rectangle BigCraftableSourceRect = new(0, 0, 16, 32);
    private readonly HashSet<string> invalidStateSpritesLogged = new(StringComparer.Ordinal);
    private readonly HashSet<string> conduitRenderDiagnosticsLogged = new(StringComparer.Ordinal);

    // Texture asset keys (lazy-computed from manifest)
    private string CopperCableTexture => $"Mods/{ModManifest.UniqueID}/CopperCable";
    private string IronCableTexture => $"Mods/{ModManifest.UniqueID}/IronCable";
    private string IridiumCableTexture => $"Mods/{ModManifest.UniqueID}/IridiumCable";
    private string SteamGeneratorTexture => $"Mods/{ModManifest.UniqueID}/SteamGenerator";
    private string WindGeneratorTexture => $"Mods/{ModManifest.UniqueID}/WindGenerator";
    private string BasicBatteryTexture => $"Mods/{ModManifest.UniqueID}/BasicBattery";
    private string IridiumBatteryTexture => $"Mods/{ModManifest.UniqueID}/IridiumBattery";
    private string PowerConduitTexture => $"Mods/{ModManifest.UniqueID}/PowerConduit";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly record struct LegacyConduitEndpoint(string LocationName, Vector2 Tile, StardewValley.Object ConduitObject);

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();

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
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.TimeChanged += OnTimeChanged;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.World.ObjectListChanged += OnObjectListChanged;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Display.RenderingStep += OnRenderingStep;
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
        WarnAboutDeprecatedMetalKegSettings();

        GmcmIntegration.Register(Helper, ModManifest, Config, () =>
        {
            Config = Helper.ReadConfig<ModConfig>() ?? new ModConfig();
        });

        LogConduitStateSpriteAvailability();
        Monitor.Log("[PowerGrid] Loaded. Waiting for save.", LogLevel.Info);
    }

    private void WarnAboutDeprecatedMetalKegSettings()
    {
        string configPath = Path.Combine(Helper.DirectoryPath, "config.json");
        if (!File.Exists(configPath))
            return;

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject;
        }
        catch (Exception ex)
        {
            Monitor.Log($"[PowerGrid] Couldn't inspect config.json for deprecated Metal Kegs settings: {ex.Message}", LogLevel.Trace);
            return;
        }

        if (root == null)
            return;

        string[] deprecatedKeys = new[]
        {
            "MetalKegEUPerMinute",
            "MetalKegMaxSpeedup",
            "MetalKegPriority",
            "HardIridiumKegEUPerMinute",
            "HardIridiumKegMaxSpeedup",
            "HardIridiumKegPriority"
        };

        var found = deprecatedKeys.Where(root.ContainsKey).ToArray();
        if (found.Length == 0)
            return;

        Monitor.Log("[PowerGrid] Deprecated Metal Kegs settings were found in PowerGrid config and are now ignored. Configure those values in Metal Kegs instead.", LogLevel.Warn);
        Monitor.Log($"[PowerGrid] Ignored deprecated keys: {string.Join(", ", found)}", LogLevel.Warn);
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
                    && TryReadNonNegativeInt(obj.modData, PersistedChargeKey, out int charge)
                    && charge > 0)
                {
                    importedBatteries[PowerConstants.MakeNodeKey(locationName, tile, itemId)] = charge;
                }

                if (importFuel
                    && importedFuel != null
                    && IsGeneratorItem(itemId)
                    && TryReadNonNegativeInt(obj.modData, PersistedFuelTicksRemainingKey, out int fuelTicksRemaining)
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
                if (!endpoint.ConduitObject.modData.TryGetValue(PersistedLinkedKey, out string? linkedRaw)
                    || linkedRaw != "1")
                {
                    continue;
                }

                if (!endpoint.ConduitObject.modData.TryGetValue(PersistedPartnerLocationKey, out string? partnerLocation)
                    || string.IsNullOrWhiteSpace(partnerLocation)
                    || !endpoint.ConduitObject.modData.TryGetValue(PersistedPartnerTileKey, out string? partnerTileRaw)
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
        return itemId == PowerConstants.SteamGeneratorId || itemId == PowerConstants.WindGeneratorId;
    }

    private static bool TryReadNonNegativeInt(ModDataDictionary modData, string key, out int value)
    {
        value = 0;
        return modData.TryGetValue(key, out string? raw)
            && int.TryParse(raw, out value)
            && value >= 0;
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
        Helper.Data.WriteSaveData(PowerConstants.SaveDataKey, BatteryState.ExportState());
        Helper.Data.WriteSaveData(PowerConstants.ConduitSaveDataKey, ConduitMgr.ExportState());
        Helper.Data.WriteSaveData(PowerConstants.FuelSaveDataKey, FuelMgr.ExportState());
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        BatteryState.ApplyDailyLeak();
        PowerMgr.ResetRuntimeState();
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
                PowerMgr.SimulateTick();
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
        // Mark location dirty when objects are placed/removed
        PowerMgr.MarkDirty(e.Location.NameOrUniqueName);
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

            // Power Conduit interaction: pairing
            if (itemId == PowerConstants.PowerConduitId)
            {
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

            // Steam Generator: fuel insertion should happen on right-click with supported fuel item.
            if (itemId == PowerConstants.SteamGeneratorId && TryInsertFuelIntoSteamGenerator(loc, obj))
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
            if (itemId == PowerConstants.SteamGeneratorId && !wantsLegacyMonitor)
            {
                Helper.Input.Suppress(e.Button);
                return;
            }

            // Legacy location-scoped monitor is available with Shift+right-click.
            if (wantsLegacyMonitor && (itemId == PowerConstants.BasicBatteryId || itemId == PowerConstants.IridiumBatteryId ||
                itemId == PowerConstants.SteamGeneratorId || itemId == PowerConstants.WindGeneratorId ||
                IsConsumerObject(obj)))
            {
                var menu = new PowerMonitorMenu(loc, PowerMgr, BatteryState, FuelMgr, ConduitMgr, Config);
                Game1.activeClickableMenu = menu;
                Helper.Input.Suppress(e.Button);
                return;
            }
        }
    }

    private void OnRenderingStep(object? sender, RenderingStepEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.currentLocation == null)
            return;

        if (e.Step != RenderSteps.World_Sorted)
            return;

        DrawCables(e.SpriteBatch, Game1.currentLocation);
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

                float layerDepth = Math.Max(0.0f, ((cable.Tile.Y + 1f) * 64f - 24f) / 10000f) + cable.Tile.X * 1E-05f;

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
            e.Edit(EditBigCraftables);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(EditCraftingRecipes);
        }

        // Texture loading for each PowerGrid object
        TryLoadTexture(e, CopperCableTexture, "CopperCable", new Color(200, 120, 60));
        TryLoadTexture(e, IronCableTexture, "IronCable", new Color(180, 180, 180));
        TryLoadTexture(e, IridiumCableTexture, "IridiumCable", new Color(120, 70, 200));
        TryLoadTexture(e, SteamGeneratorTexture, "SteamGenerator", new Color(140, 140, 160));
        TryLoadTexture(e, WindGeneratorTexture, "WindGenerator", new Color(100, 180, 220));
        TryLoadTexture(e, BasicBatteryTexture, "BasicBattery", new Color(60, 180, 60));
        TryLoadTexture(e, IridiumBatteryTexture, "IridiumBattery", new Color(150, 80, 220));
        TryLoadTexture(e, PowerConduitTexture, "PowerConduit", new Color(220, 200, 60));

        TryLoadStateTexture(e, "SteamGenerator", "off", new Color(140, 140, 160));
        TryLoadStateTexture(e, "SteamGenerator", "on", new Color(140, 140, 160));
        TryLoadStateTexture(e, "WindGenerator", "idle", new Color(100, 180, 220));
        TryLoadStateTexture(e, "WindGenerator", "generating", new Color(100, 180, 220));
        TryLoadStateTexture(e, "BasicBattery", "low", new Color(60, 180, 60));
        TryLoadStateTexture(e, "BasicBattery", "charged", new Color(60, 180, 60));
        TryLoadStateTexture(e, "IridiumBattery", "low", new Color(150, 80, 220));
        TryLoadStateTexture(e, "IridiumBattery", "charged", new Color(150, 80, 220));
        TryLoadStateTexture(e, "PowerConduit", "unpaired", new Color(220, 200, 60));
        TryLoadStateTexture(e, "PowerConduit", "linked", new Color(220, 200, 60));
    }

    private void TryLoadTexture(AssetRequestedEventArgs e, string assetKey, string spriteName, Color tint)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(assetKey))
            return;

        e.LoadFrom(() => LoadTextureOrFallback(spriteName, spriteName, tint), AssetLoadPriority.Medium);
    }

    private void TryLoadStateTexture(AssetRequestedEventArgs e, string baseSpriteName, string stateName, Color tint)
    {
        string stateSpriteName = GetStateSpriteName(baseSpriteName, stateName);
        string assetKey = GetTextureAsset(stateSpriteName);
        if (!e.NameWithoutLocale.IsEquivalentTo(assetKey))
            return;

        if (baseSpriteName == "PowerConduit")
        {
            string fullPath = Path.Combine(Helper.DirectoryPath, "assets", $"{stateSpriteName}.png");
            LogConduitDiagnosticOnce(
                $"asset-request|{stateSpriteName}",
                $"[PowerGrid] Conduit state asset requested: assetKey={assetKey}, fileExists={File.Exists(fullPath)}, path={fullPath}");
        }

        e.LoadFrom(() => LoadTextureOrFallback(stateSpriteName, baseSpriteName, tint), AssetLoadPriority.Medium);
    }

    private Texture2D LoadTextureOrFallback(string spriteName, string fallbackSpriteName, Color tint)
    {
        string customPath = $"assets/{spriteName}.png";
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
            "SteamGenerator" => 13,   // Furnace
            "WindGenerator" => 10,    // Bee House
            "BasicBattery" => 36,     // Lightning Rod
            "IridiumBattery" => 36,
            "PowerConduit" => 8,      // Scarecrow
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
            "Burns fuel (Coal, Wood, Hardwood, Battery Pack) to produce EU. Place fuel inside to power it.", SteamGeneratorTexture);
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

        // Copper Cable: 334 (Copper Bar) x3
        dict["Copper Cable"] = $"334 3/Field/{PowerConstants.CopperCableId}/true/null/";
        // Iron Cable: 335 (Iron Bar) x3
        dict["Iron Cable"] = $"335 3/Field/{PowerConstants.IronCableId}/true/null/";
        // Iridium Cable: 337 (Iridium Bar) x2, 338 (Refined Quartz) x1
        dict["Iridium Cable"] = $"337 2 338 1/Field/{PowerConstants.IridiumCableId}/true/null/";

        // Steam Generator: 335 (Iron Bar) x5, 382 (Coal) x5, 334 (Copper Bar) x3
        dict["Steam Generator"] = $"335 5 382 5 334 3/Field/{PowerConstants.SteamGeneratorId}/true/null/";
        // Wind Generator: 335 (Iron Bar) x3, 388 (Wood) x20, 92 (Sap) x5
        dict["Wind Generator"] = $"335 3 388 20 92 5/Field/{PowerConstants.WindGeneratorId}/true/null/";

        // Basic Power Battery: 787 (Battery Pack) x1, 334 (Copper Bar) x5
        dict["Basic Power Battery"] = $"787 1 334 5/Field/{PowerConstants.BasicBatteryId}/true/null/";
        // Iridium Power Battery: 787 (Battery Pack) x3, 337 (Iridium Bar) x2
        dict["Iridium Power Battery"] = $"787 3 337 2/Field/{PowerConstants.IridiumBatteryId}/true/null/";

        // Power Conduit: 337 (Iridium Bar) x1, 787 (Battery Pack) x1, 338 (Refined Quartz) x2
        dict["Power Conduit"] = $"337 1 787 1 338 2/Field/{PowerConstants.PowerConduitId}/true/null/";
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
                Monitor.Log($"    Consumer [{consumer.LocationName} @ {consumer.TileX},{consumer.TileY}] {consumer.ItemId}: {consumer.EUAllocated}/{consumer.DemandPerTick} EU, speedup={consumer.SpeedupFraction:P0}, accel={consumer.MinutesAccelerated}min, remaining={consumer.MinutesRemaining}min", LogLevel.Info);
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
            Monitor.Log($"  Consumer [{consumer.LocationName} @ {consumer.TileX},{consumer.TileY}] {consumer.DisplayName}: processing={consumer.IsProcessing}, powered={consumer.IsPowered}, alloc={consumer.EUAllocated}/{consumer.DemandPerTick}, speedup={consumer.SpeedupFraction:P0}, remaining={consumer.MinutesRemaining}", LogLevel.Info);
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

    private static readonly string[] PowerGridRecipeKeys = new[]
    {
        "Copper Cable",
        "Iron Cable",
        "Iridium Cable",
        "Steam Generator",
        "Wind Generator",
        "Basic Power Battery",
        "Iridium Power Battery",
        "Power Conduit"
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
            if (!force && unlockMode.Equals("existingProgress", StringComparison.OrdinalIgnoreCase))
            {
                // Conservative mid/late-game condition:
                // - Player can already craft a Keg (industry) OR Lightning Rod (electricity),
                //   or is at least Mining 6 (plenty of metal progression).
                bool knowsKeg = farmer.craftingRecipes.ContainsKey("Keg");
                bool knowsLightningRod = farmer.craftingRecipes.ContainsKey("Lightning Rod");
                bool miningEnough = farmer.MiningLevel >= 6;

                if (!(knowsKeg || knowsLightningRod || miningEnough))
                    continue;
            }

            any |= GrantPowerGridRecipes(farmer);
        }

        if (any)
            Monitor.Log($"[PowerGrid] Granted crafting recipes after {reason} (mode='{unlockMode}', force={force}).", LogLevel.Info);
        else
            Monitor.Log($"[PowerGrid] No recipes granted after {reason} (mode='{unlockMode}', force={force}).", LogLevel.Trace);
    }

    private static bool GrantPowerGridRecipes(Farmer player)
    {
        bool any = false;
        foreach (string key in PowerGridRecipeKeys)
        {
            if (!player.craftingRecipes.ContainsKey(key))
            {
                player.craftingRecipes.Add(key, 0);
                any = true;
            }
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

        Game1.activeClickableMenu = new PowerTabMenu(PowerMgr, BatteryState, FuelMgr, ConduitMgr);
    }

    private static bool IsSupportedGeneratorFuel(StardewValley.Object fuel)
    {
        return fuel.QualifiedItemId switch
        {
            "(O)382" => true, // Coal
            "(O)388" => true, // Wood
            "(O)709" => true, // Hardwood
            "(O)787" => true, // Battery Pack
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
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(StatefulConduitTileDrawPrefix)),
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
                harmony.Patch(drawAboveFrontLayerTarget, postfix: new HarmonyMethod(typeof(ModEntry), nameof(StatefulObjectAboveFrontLayerPostfix)));
            }
        }
        catch (Exception ex)
        {
            Monitor.Log($"[PowerGrid] Failed to apply runtime object patches: {ex}", LogLevel.Error);
        }
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

    private static bool StatefulConduitTileDrawPrefix(StardewValley.Object __instance, object[] __args)
    {
        if (Instance == null || __instance?.ItemId != PowerConstants.PowerConduitId || __args.Length < 4 || __args[0] is not SpriteBatch spriteBatch)
            return true;

        if (__args[1] is not int tileX || __args[2] is not int tileY)
            return true;

        float alpha = __args[3] is float drawAlpha ? drawAlpha : 1f;
        return !Instance.TryDrawConduitTileReplacement(__instance, spriteBatch, tileX, tileY, alpha, "tile-prefix");
    }

    private static void StatefulObjectTileDrawPostfix(StardewValley.Object __instance, object[] __args)
    {
        if (Instance == null || __args.Length < 4 || __args[0] is not SpriteBatch spriteBatch)
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
        float layerDepth = Math.Max(0f, ((tileY + 1) * 64f - 24f) / 10000f + tileX / 1000000f);

        DrawStatefulTexture(spriteBatch, texture!, screenPos, alpha, layerDepth);
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
        float layerDepth = layerDepthOverride ?? Math.Max(0f, ((tileY + 1) * 64f - 24f) / 10000f + tileX / 1000000f);

        DrawStatefulTexture(spriteBatch, texture!, screenPos, alpha, layerDepth);
    }

    private static void DrawStatefulTexture(SpriteBatch spriteBatch, Texture2D texture, Vector2 screenPos, float alpha, float layerDepth)
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

    private bool TryDrawConduitTileReplacement(StardewValley.Object obj, SpriteBatch spriteBatch, int tileX, int tileY, float alpha, string source)
    {
        if (!Context.IsWorldReady || Game1.currentLocation == null || Game1.currentLocation.getObjectAtTile(tileX, tileY) != obj)
            return false;

        Vector2 tile = new(tileX, tileY);
        if (!TryGetStatefulSpriteName(obj, tile, out string? stateSpriteName))
            return false;

        if (!TryLoadStateTextureForDraw(stateSpriteName!, out Texture2D? texture))
            return false;

        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX * 64, tileY * 64 - 64));
        float layerDepth = Math.Max(0f, ((tileY + 1) * 64f - 24f) / 10000f + tileX / 1000000f);
        LogConduitRenderDiagnostic(obj, source, new object[] { tileX, tileY, alpha, stateSpriteName! });
        DrawStatefulTexture(spriteBatch, texture!, screenPos, alpha, layerDepth);
        return true;
    }

    private bool TryGetStatefulSpriteName(StardewValley.Object obj, Vector2 tile, out string? stateSpriteName)
    {
        stateSpriteName = null;

        string itemId = obj.ItemId ?? "";
        string locationName = Game1.currentLocation?.NameOrUniqueName ?? "";

        string? stateName = itemId switch
        {
            PowerConstants.SteamGeneratorId => GetSteamGeneratorState(locationName, tile),
            PowerConstants.WindGeneratorId => GetWindGeneratorState(obj),
            PowerConstants.BasicBatteryId => GetBatteryState(locationName, tile, itemId),
            PowerConstants.IridiumBatteryId => GetBatteryState(locationName, tile, itemId),
            PowerConstants.PowerConduitId => GetConduitState(locationName, tile),
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

        string customPath = $"assets/{stateSpriteName}.png";
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

        string generatorKey = PowerConstants.MakeNodeKey(locationName, tile, PowerConstants.SteamGeneratorId);
        return FuelMgr.GetFuelTicksRemaining(generatorKey) > 0 ? "on" : "off";
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

        return null;
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
            PowerConstants.WindGeneratorId => "WindGenerator",
            PowerConstants.BasicBatteryId => "BasicBattery",
            PowerConstants.IridiumBatteryId => "IridiumBattery",
            PowerConstants.PowerConduitId => "PowerConduit",
            _ => null
        };
    }

    private string GetTextureAsset(string spriteName)
    {
        return PowerConstants.TextureAsset(ModManifest.UniqueID, spriteName);
    }

    private static string GetStateSpriteName(string baseSpriteName, string stateName)
    {
        return $"{baseSpriteName}__{stateName}";
    }

    private void LogConduitStateSpriteAvailability()
    {
        string linkedPath = Path.Combine(Helper.DirectoryPath, "assets", "PowerConduit__linked.png");
        string unpairedPath = Path.Combine(Helper.DirectoryPath, "assets", "PowerConduit__unpaired.png");
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

    private bool TryInsertFuelIntoSteamGenerator(GameLocation location, StardewValley.Object generator)
    {
        StardewValley.Object? activeFuel = Game1.player.ActiveObject;
        if (activeFuel == null || !IsSupportedGeneratorFuel(activeFuel))
            return false;

        string generatorKey = PowerConstants.MakeNodeKey(location.NameOrUniqueName, generator.TileLocation, generator.ItemId ?? "");
        if (!FuelMgr.TryAddFuel(generatorKey, activeFuel, 1, out int ticksAdded))
            return false;

        Game1.player.reduceActiveItemByOne();
        Game1.playSound("Ship");

        int minutesAdded = ticksAdded * PowerConstants.TickIntervalMinutes;
        Game1.addHUDMessage(new HUDMessage($"+{minutesAdded}m generator fuel", HUDMessage.newQuest_type));
        return true;
    }

    private bool TryMigrateLegacySteamFuel(GameLocation location, StardewValley.Object generator)
    {
        StardewValley.Object? heldFuel = generator.heldObject.Value;
        if (heldFuel == null || heldFuel.Stack <= 0 || !IsSupportedGeneratorFuel(heldFuel))
            return false;

        string generatorKey = PowerConstants.MakeNodeKey(location.NameOrUniqueName, generator.TileLocation, generator.ItemId ?? "");
        if (!FuelMgr.TryAddFuel(generatorKey, heldFuel, heldFuel.Stack, out int ticksAdded))
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
