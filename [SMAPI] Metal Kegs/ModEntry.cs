using System.Collections;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using DarthMods.API.Power;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using Darth.MetalKegs.Integrations;

namespace Darth.MetalKegs;

internal sealed class ModEntry : Mod
{
    private const string MetalCaskItemId = "darth.MetalKegs_MetalCask";
    private const string MetalKegItemId = "darth.MetalKegs_MetalKeg";
    private const string HardIridiumKegItemId = "darth.MetalKegs_HardIridiumKeg";

    private const string MetalCaskRecipeKey = "Metal Cask";
    private const string MetalKegRecipeKey = "Metal Keg";
    private const string HardIridiumKegRecipeKey = "Hard Iridium Keg";
    private const string DefaultBigCraftableTexture = "TileSheets/Craftables";
    private const string MetalCaskCustomSpritePath = "assets/MetalCask.png";
    private const string MetalKegCustomSpritePath = "assets/MetalKeg.png";
    private const string HardIridiumKegCustomSpritePath = "assets/HardIridiumKeg.png";
    private const string VanillaCaskTemplateExportPath = "assets/templates/VanillaCask.png";
    private const string VanillaKegTemplateExportPath = "assets/templates/VanillaKeg.png";
    private const string HardwoodKegTemplateExportPath = "assets/templates/HardwoodKeg.png";
    private const string PowerGridUniqueId = "meiameiameia.PowerGrid";
    private const string LegacyPowerGridUniqueId = "darth.PowerGrid";
    private const string MetalCaskQualifiedItemId = "(BC)" + MetalCaskItemId;
    private const string MetalKegQualifiedItemId = "(BC)" + MetalKegItemId;
    private const string HardIridiumKegQualifiedItemId = "(BC)" + HardIridiumKegItemId;
    private const int PowerGridTickIntervalMinutes = 10;
    private const float MetalCaskBaseAgingMultiplier = 0.50f;
    private const float MetalCaskPoweredBonusDaysAtFullPower = 1.50f;
    private const string MetalCaskPlacementError = "Metal Cask can only be placed in player-owned indoor spaces.";
    private const string MetalCaskMarkerKey = "meiameiameia.MetalKegs/MetalCask";
    private const string MetalCaskPowerModeKey = "meiameiameia.MetalKegs/PowerMode";
    private const string MetalCaskObservedPowerStateKey = "meiameiameia.MetalKegs/ObservedPowerState";
    private const string MetalCaskObservedSpeedupKey = "meiameiameia.MetalKegs/ObservedSpeedupFraction";
    private const string MetalCaskLastAppliedBonusDaysKey = "meiameiameia.MetalKegs/LastAppliedBonusDays";
    private const string MetalCaskLastAppliedDateKey = "meiameiameia.MetalKegs/LastAppliedDate";
    private static readonly FieldInfo? CaskDaysToMatureField = AccessTools.Field(typeof(Cask), "daysToMature");
    private static readonly MethodInfo? CaskCheckForMaturityMethod = AccessTools.Method(typeof(Cask), "checkForMaturity");
    private static readonly MethodInfo? ResetParentSheetIndexMethod = AccessTools.Method(typeof(StardewValley.Object), "ResetParentSheetIndex");

    private string MetalCaskTextureAsset => $"Mods/{this.ModManifest.UniqueID}/MetalCask";
    private string MetalKegTextureAsset => $"Mods/{this.ModManifest.UniqueID}/MetalKeg";
    private string HardIridiumKegTextureAsset => $"Mods/{this.ModManifest.UniqueID}/HardIridiumKeg";
    private static ModEntry? Instance;

    private ModConfig Config = new();
    private Harmony? harmony;
    private IPowerGridApi? PowerGridApi;

    private bool GofDetectedByRegistry;
    private string? GofUniqueId;
    private bool HardwoodKegDetectedByData;

    private TemplateSpriteInfo? VanillaCaskSpriteInfo;
    private TemplateSpriteInfo? VanillaKegSpriteInfo;
    private TemplateSpriteInfo? HardwoodKegSpriteInfo;
    private string? VanillaCaskBigCraftableId;
    private string? VanillaKegBigCraftableId;
    private string? HardwoodKegBigCraftableId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions GameDataCloneJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        IncludeFields = true
    };

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        this.Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
        this.TryMigrateLegacyPowerGridSettings();

        this.DetectGof(helper);
        this.ApplyHarmonyPatches();

        helper.ConsoleCommands.Add("metal_kegs_unlock", "Grant Metal Cask / Metal Keg / Hard Iridium Keg crafting recipes if eligible.\nUsage: metal_kegs_unlock", this.CmdUnlock);
        helper.ConsoleCommands.Add("metal_kegs_status", "Print Metal Cask / Metal Keg / Hard Iridium Keg unlock status.\nUsage: metal_kegs_status", this.CmdStatus);
        helper.ConsoleCommands.Add("metal_kegs_machine_status", "Print Data/Machines registration status for Metal Cask, Metal Keg, and Hard Iridium Keg.\nUsage: metal_kegs_machine_status", this.CmdMachineStatus);
        helper.ConsoleCommands.Add("metal_kegs_power_status", "Print PowerGrid acceleration status for Metal Cask, Metal Keg, and Hard Iridium Keg in the current location.\nUsage: metal_kegs_power_status", this.CmdPowerStatus);
        helper.ConsoleCommands.Add("metal_kegs_export_sprites", "Export vanilla Cask, Keg, and GOF Hardwood Keg source sprites as PNG files.\nUsage: metal_kegs_export_sprites", this.CmdExportSprites);

        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Player.Warped += this.OnWarped;
    }

    private void DetectGof(IModHelper helper)
    {
        // Required by spec: detect GOF via SMAPI registry (without assuming the exact UniqueID).
        foreach (IModInfo mod in helper.ModRegistry.GetAll())
        {
            string name = mod.Manifest.Name ?? "";
            string uniqueId = mod.Manifest.UniqueID ?? "";
            if (name.Contains("Grapes of Ferngill", StringComparison.OrdinalIgnoreCase)
                || uniqueId.Contains("GrapesOfFerngill", StringComparison.OrdinalIgnoreCase)
                || uniqueId.Contains(".gof", StringComparison.OrdinalIgnoreCase)
                || name.Equals("GOF", StringComparison.OrdinalIgnoreCase))
            {
                this.GofDetectedByRegistry = true;
                this.GofUniqueId = uniqueId;
                return;
            }
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GmcmIntegration.Register(this.Helper, this.ModManifest, this.Config, () =>
        {
            this.Config = this.Helper.ReadConfig<ModConfig>() ?? new ModConfig();
        }, this.SyncPowerGridConsumers);

        this.SyncPowerGridConsumers();

        if (this.GofDetectedByRegistry)
            this.Monitor.Log($"Detected GOF via registry: {this.GofUniqueId ?? "(unknown UniqueID)"}", LogLevel.Info);
        else
            this.Monitor.Log("GOF not detected via registry. Hard Iridium Keg will be disabled unless MissingGofMode is set to fallback.", LogLevel.Info);
    }

    private void SyncPowerGridConsumers()
    {
        this.PowerGridApi = this.Helper.ModRegistry.GetApi<IPowerGridApi>(PowerGridUniqueId)
            ?? this.Helper.ModRegistry.GetApi<IPowerGridApi>(LegacyPowerGridUniqueId);
        if (this.PowerGridApi == null)
            return;

        if (!this.Config.EnablePowerGridIntegration)
        {
            this.PowerGridApi.UnregisterConsumer(MetalCaskQualifiedItemId);
            this.PowerGridApi.UnregisterConsumer(MetalKegQualifiedItemId);
            this.PowerGridApi.UnregisterConsumer(HardIridiumKegQualifiedItemId);
            this.Monitor.Log("PowerGrid integration disabled in Metal Kegs config; consumer traits were unregistered.", LogLevel.Debug);
            return;
        }

        this.PowerGridApi.RegisterConsumer(
            MetalCaskQualifiedItemId,
            GetDemandPerTick(this.Config.MetalCaskEUPerMinute),
            ClampSpeedup(this.Config.MetalCaskMaxSpeedup),
            Math.Max(0, this.Config.MetalCaskPriority),
            MetalCaskRecipeKey);

        this.PowerGridApi.RegisterConsumer(
            MetalKegQualifiedItemId,
            GetDemandPerTick(this.Config.MetalKegEUPerMinute),
            ClampSpeedup(this.Config.MetalKegMaxSpeedup),
            Math.Max(0, this.Config.MetalKegPriority),
            MetalKegRecipeKey);

        this.PowerGridApi.RegisterConsumer(
            HardIridiumKegQualifiedItemId,
            GetDemandPerTick(this.Config.HardIridiumKegEUPerMinute),
            ClampSpeedup(this.Config.HardIridiumKegMaxSpeedup),
            Math.Max(0, this.Config.HardIridiumKegPriority),
            HardIridiumKegRecipeKey);

        this.Monitor.Log("Registered Metal Kegs consumer traits with PowerGrid using Metal Kegs config-owned values.", LogLevel.Debug);
    }

    private void TryMigrateLegacyPowerGridSettings()
    {
        if (this.Config.LegacyPowerGridSettingsMigrated)
            return;

        if (!TryFindPowerGridConfigPath(out string? configPath) || string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            return;

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(configPath)) as JsonObject;
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Couldn't inspect PowerGrid config for legacy Metal Kegs settings: {ex.Message}", LogLevel.Trace);
            return;
        }

        if (root == null)
            return;

        bool foundLegacy = false;
        bool importedAny = false;

        if (!HasCustomEnergySettings())
        {
            importedAny |= TryImportLegacyInt(root, "MetalKegEUPerMinute", value => this.Config.MetalKegEUPerMinute = value, min: 0);
            importedAny |= TryImportLegacyFloat(root, "MetalKegMaxSpeedup", value => this.Config.MetalKegMaxSpeedup = value, min: 0f, max: 1f);
            importedAny |= TryImportLegacyInt(root, "MetalKegPriority", value => this.Config.MetalKegPriority = value, min: 0);
            importedAny |= TryImportLegacyInt(root, "HardIridiumKegEUPerMinute", value => this.Config.HardIridiumKegEUPerMinute = value, min: 0);
            importedAny |= TryImportLegacyFloat(root, "HardIridiumKegMaxSpeedup", value => this.Config.HardIridiumKegMaxSpeedup = value, min: 0f, max: 1f);
            importedAny |= TryImportLegacyInt(root, "HardIridiumKegPriority", value => this.Config.HardIridiumKegPriority = value, min: 0);
            foundLegacy = importedAny;
        }
        else
        {
            foundLegacy =
                root.ContainsKey("MetalKegEUPerMinute") ||
                root.ContainsKey("MetalKegMaxSpeedup") ||
                root.ContainsKey("MetalKegPriority") ||
                root.ContainsKey("HardIridiumKegEUPerMinute") ||
                root.ContainsKey("HardIridiumKegMaxSpeedup") ||
                root.ContainsKey("HardIridiumKegPriority");
        }

        if (!foundLegacy)
            return;

        this.Config.LegacyPowerGridSettingsMigrated = true;
        this.Helper.WriteConfig(this.Config);

        if (importedAny)
        {
            this.Monitor.Log("Imported deprecated Metal Kegs energy settings from PowerGrid config into Metal Kegs config. Future changes should be made in Metal Kegs.", LogLevel.Warn);
        }
        else
        {
            this.Monitor.Log("Detected deprecated PowerGrid-owned Metal Kegs settings, but kept Metal Kegs' current config values. Future changes should be made in Metal Kegs.", LogLevel.Warn);
        }
    }

    private bool HasCustomEnergySettings()
    {
        return this.Config.EnablePowerGridIntegration != true
            || this.Config.MetalCaskEUPerMinute != ModConfig.DefaultMetalCaskEUPerMinute
            || Math.Abs(this.Config.MetalCaskMaxSpeedup - ModConfig.DefaultMetalCaskMaxSpeedup) > 0.0001f
            || this.Config.MetalCaskPriority != ModConfig.DefaultMetalCaskPriority
            || this.Config.MetalKegEUPerMinute != ModConfig.DefaultMetalKegEUPerMinute
            || Math.Abs(this.Config.MetalKegMaxSpeedup - ModConfig.DefaultMetalKegMaxSpeedup) > 0.0001f
            || this.Config.MetalKegPriority != ModConfig.DefaultMetalKegPriority
            || this.Config.HardIridiumKegEUPerMinute != ModConfig.DefaultHardIridiumKegEUPerMinute
            || Math.Abs(this.Config.HardIridiumKegMaxSpeedup - ModConfig.DefaultHardIridiumKegMaxSpeedup) > 0.0001f
            || this.Config.HardIridiumKegPriority != ModConfig.DefaultHardIridiumKegPriority;
    }

    private bool TryFindPowerGridConfigPath(out string? configPath)
    {
        configPath = null;

        string? modsRoot = Directory.GetParent(this.Helper.DirectoryPath)?.FullName;
        if (string.IsNullOrWhiteSpace(modsRoot) || !Directory.Exists(modsRoot))
            return false;

        foreach (string dir in Directory.EnumerateDirectories(modsRoot))
        {
            string manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                JsonObject? manifest = JsonNode.Parse(File.ReadAllText(manifestPath)) as JsonObject;
                string? uniqueId = manifest?["UniqueID"]?.GetValue<string>();
                if (!string.Equals(uniqueId, PowerGridUniqueId, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(uniqueId, LegacyPowerGridUniqueId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                configPath = Path.Combine(dir, "config.json");
                return true;
            }
            catch
            {
                // Ignore malformed manifests when scanning sibling mod folders.
            }
        }

        return false;
    }

    private static bool TryImportLegacyInt(JsonObject root, string key, Action<int> setter, int min)
    {
        int? value = root[key]?.GetValue<int?>();
        if (!value.HasValue)
            return false;

        setter(Math.Max(min, value.Value));
        return true;
    }

    private static bool TryImportLegacyFloat(JsonObject root, string key, Action<float> setter, float min, float max)
    {
        float? value = root[key]?.GetValue<float?>();
        if (!value.HasValue)
            return false;

        setter(Math.Clamp(value.Value, min, max));
        return true;
    }

    private static int GetDemandPerTick(int euPerMinute)
    {
        return Math.Max(0, euPerMinute) * PowerGridTickIntervalMinutes;
    }

    private static float ClampSpeedup(float speedup)
    {
        return Math.Clamp(speedup, 0f, 1f);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.RehydrateMetalCasks();
        this.RefreshMetalCaskTelemetry();
        this.TryGrantRecipes(Game1.player, reason: "DayStarted");
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        this.RehydrateMetalCasks();
        this.RefreshMetalCaskTelemetry();
        this.TryGrantRecipes(Game1.player, reason: "SaveLoaded");
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsLocalPlayer)
            return;

        this.RehydrateMetalCasks(e.NewLocation);
        this.RefreshMetalCaskTelemetry(e.NewLocation);
    }

    private void TryGrantRecipes(Farmer player, string reason)
    {
        string unlockMode = (this.Config.UnlockMode ?? "existingProgress").Trim();
        string missingGofMode = (this.Config.MissingGofMode ?? "disable").Trim();

        bool metalCaskUnlocked =
            unlockMode.Equals("always", StringComparison.OrdinalIgnoreCase)
            || this.PlayerHasCellar(player);

        bool metalKegUnlocked =
            unlockMode.Equals("always", StringComparison.OrdinalIgnoreCase)
            || player.FarmingLevel >= 8
            || (player.craftingRecipes.TryGetValue("Keg", out int kegCraftCount) && kegCraftCount > 0);

        bool hardIridiumRecipeAllowed = this.IsHardIridiumRecipeAllowed(missingGofMode);

        bool hardIridiumUnlocked;
        if (unlockMode.Equals("always", StringComparison.OrdinalIgnoreCase))
        {
            hardIridiumUnlocked = hardIridiumRecipeAllowed;
        }
        else
        {
            hardIridiumUnlocked =
                hardIridiumRecipeAllowed
                && (this.PlayerHasCellar(player) || this.GetWineShippedCount(player) >= 200);
        }

        bool any = false;
        if (metalCaskUnlocked)
            any |= this.GrantCraftingRecipe(player, MetalCaskRecipeKey);

        if (metalKegUnlocked)
            any |= this.GrantCraftingRecipe(player, MetalKegRecipeKey);

        if (hardIridiumUnlocked)
            any |= this.GrantCraftingRecipe(player, HardIridiumKegRecipeKey);

        if (any)
            this.Monitor.Log($"Granted recipes after {reason}.", LogLevel.Info);
        else
            this.Monitor.Log($"No recipes granted after {reason} (may be locked by progress/config). Run `metal_kegs_status` to see why.", LogLevel.Trace);
    }

    private bool IsHardIridiumRecipeAllowed(string missingGofMode)
    {
        if (this.GofDetectedByRegistry || this.HardwoodKegDetectedByData)
            return true;

        return missingGofMode.Equals("fallbackVanillaKeg", StringComparison.OrdinalIgnoreCase)
            || missingGofMode.Equals("fallback", StringComparison.OrdinalIgnoreCase);
    }

    private bool GrantCraftingRecipe(Farmer player, string recipeKey)
    {
        if (!player.craftingRecipes.ContainsKey(recipeKey))
        {
            player.craftingRecipes.Add(recipeKey, 0);
            this.Monitor.Log($"Granted crafting recipe: {recipeKey}", LogLevel.Info);
            return true;
        }

        return false;
    }

    private bool PlayerHasCellar(Farmer player)
    {
        FarmHouse? home = Utility.getHomeOfFarmer(player);
        return home != null && home.upgradeLevel >= 3;
    }

    private int GetWineShippedCount(Farmer player)
    {
        // Wine is vanilla object ID 348; shipping data is per item ID, so all wines aggregate there.
        const int wineId = 348;

        object? shipped = player.basicShipped;

        if (shipped is IDictionary<int, int> shippedInt)
            return shippedInt.TryGetValue(wineId, out int count) ? count : 0;

        if (shipped is IDictionary<string, int> shippedString)
            return shippedString.TryGetValue(wineId.ToString(), out int count) ? count : 0;

        if (shipped is IDictionary shippedUntyped)
        {
            foreach (DictionaryEntry entry in shippedUntyped)
            {
                if (entry.Key is int kInt && kInt == wineId)
                    return entry.Value is int v ? v : 0;
                if (entry.Key is string kStr && kStr == wineId.ToString())
                    return entry.Value is int v ? v : 0;
            }
        }

        return 0;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
        {
            e.Edit(this.EditBigCraftables, AssetEditPriority.Late);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(this.EditCraftingRecipes, AssetEditPriority.Late);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
        {
            e.Edit(this.EditMachines, AssetEditPriority.Late);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo(this.MetalCaskTextureAsset))
        {
            e.LoadFrom(this.BuildMetalCaskTexture, AssetLoadPriority.Medium);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo(this.MetalKegTextureAsset))
        {
            e.LoadFrom(this.BuildMetalKegTexture, AssetLoadPriority.Medium);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo(this.HardIridiumKegTextureAsset))
        {
            e.LoadFrom(this.BuildHardIridiumKegTexture, AssetLoadPriority.Medium);
            return;
        }
    }

    private void EditCraftingRecipes(IAssetData asset)
    {
        IDictionary<string, string> dict = asset.AsDictionary<string, string>().Data;

        string? caskTemplate = dict.TryGetValue("Cask", out string? caskStr) ? caskStr : null;
        string metalCaskRecipe = BuildRecipeFromTemplate(
            template: caskTemplate,
            ingredients: "709 20 337 3 335 10 787 1 382 25",
            resultItemId: MetalCaskItemId);
        dict[MetalCaskRecipeKey] = metalCaskRecipe;

        string? kegTemplate = dict.TryGetValue("Keg", out string? kegStr) ? kegStr : null;
        string metalRecipe = BuildRecipeFromTemplate(
            template: kegTemplate,
            ingredients: "335 10 334 5 338 2 725 1 382 15",
            resultItemId: MetalKegItemId);
        dict[MetalKegRecipeKey] = metalRecipe;

        string? hardwoodTemplate = dict.TryGetValue("Hardwood Keg", out string? hwStr) ? hwStr : null;
        string missingGofMode = (this.Config.MissingGofMode ?? "disable").Trim();
        bool allowHardIridiumRecipe = this.IsHardIridiumRecipeAllowed(missingGofMode);
        if (allowHardIridiumRecipe)
        {
            string hardIridiumRecipe = BuildRecipeFromTemplate(
                template: hardwoodTemplate ?? kegTemplate,
                ingredients: "337 5 787 1 338 5 725 1 382 30",
                resultItemId: HardIridiumKegItemId);
            dict[HardIridiumKegRecipeKey] = hardIridiumRecipe;
        }
    }

    private static string BuildRecipeFromTemplate(string? template, string ingredients, string resultItemId)
    {
        // Backcompat-friendly: keep the template's route/unlock fields, only swapping ingredients + output.
        // Example template: "709 10 335 5 787 1 382 10/Home/FishSmoker/true/null/"
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

    private void EditBigCraftables(IAssetData asset)
    {
        IDictionary<string, BigCraftableData> dict = asset.AsDictionary<string, BigCraftableData>().Data;

        KeyValuePair<string, BigCraftableData>? caskTemplateOpt = FindBigCraftableTemplate(dict, targetDisplayName: "Cask", preferredKey: "163");
        if (caskTemplateOpt != null)
        {
            KeyValuePair<string, BigCraftableData> caskTemplate = caskTemplateOpt.Value;
            this.VanillaCaskBigCraftableId = caskTemplate.Key;
            this.VanillaCaskSpriteInfo ??= new TemplateSpriteInfo(NormalizeTextureAssetName(caskTemplate.Value.Texture), caskTemplate.Value.SpriteIndex);

            BigCraftableData metalCaskData = CloneByJson(caskTemplate.Value);
            metalCaskData.DisplayName = MetalCaskRecipeKey;
            metalCaskData.Name = MetalCaskRecipeKey;
            metalCaskData.Description = "An industrial cask that can age artisan goods in player-owned indoor spaces. Slower than a cellar cask unless powered.";
            metalCaskData.Texture = this.MetalCaskTextureAsset;
            metalCaskData.SpriteIndex = 0;
            dict[MetalCaskItemId] = metalCaskData;
        }
        else
        {
            this.Monitor.Log("Failed to find vanilla Cask template in Data/BigCraftables; Metal Cask will not be added.", LogLevel.Error);
        }

        KeyValuePair<string, BigCraftableData>? kegTemplateOpt = FindBigCraftableTemplate(dict, targetDisplayName: "Keg", preferredKey: "12");
        if (kegTemplateOpt == null)
        {
            this.Monitor.Log("Failed to find vanilla Keg template in Data/BigCraftables; Metal Keg will not be added.", LogLevel.Error);
            return;
        }

        KeyValuePair<string, BigCraftableData> kegTemplate = kegTemplateOpt.Value;
        this.VanillaKegBigCraftableId = kegTemplate.Key;
        this.VanillaKegSpriteInfo ??= new TemplateSpriteInfo(NormalizeTextureAssetName(kegTemplate.Value.Texture), kegTemplate.Value.SpriteIndex);

        BigCraftableData metalKegData = CloneByJson(kegTemplate.Value);
        metalKegData.DisplayName = MetalKegRecipeKey;
        metalKegData.Name = MetalKegRecipeKey;
        metalKegData.Description = "A sturdy metal keg. Functions like a Keg.";
        metalKegData.Texture = this.MetalKegTextureAsset;
        metalKegData.SpriteIndex = 0;
        dict[MetalKegItemId] = metalKegData;

        KeyValuePair<string, BigCraftableData>? hardwoodTemplateOpt = FindBigCraftableTemplate(dict, targetDisplayName: "Hardwood Keg", preferredKey: null);
        bool hardwoodFound = hardwoodTemplateOpt != null;
        this.HardwoodKegDetectedByData |= hardwoodFound;
        if (hardwoodFound)
        {
            this.HardwoodKegBigCraftableId = hardwoodTemplateOpt!.Value.Key;
            this.HardwoodKegSpriteInfo ??= new TemplateSpriteInfo(NormalizeTextureAssetName(hardwoodTemplateOpt.Value.Value.Texture), hardwoodTemplateOpt.Value.Value.SpriteIndex);
        }

        BigCraftableData baseForHardIridium = hardwoodFound ? hardwoodTemplateOpt!.Value.Value : kegTemplate.Value;

        BigCraftableData hardIridiumData = CloneByJson(baseForHardIridium);
        hardIridiumData.DisplayName = HardIridiumKegRecipeKey;
        hardIridiumData.Name = HardIridiumKegRecipeKey;
        hardIridiumData.Description = "An iridium-reinforced keg. Matches GOF Hardwood Keg behavior when available.";
        hardIridiumData.Texture = this.HardIridiumKegTextureAsset;
        hardIridiumData.SpriteIndex = 0;
        dict[HardIridiumKegItemId] = hardIridiumData;
    }

    private void EditMachines(IAssetData asset)
    {
        IDictionary<string, MachineData> machines = asset.AsDictionary<string, MachineData>().Data;

        string? caskBaseKey = GetCaskMachineKey(machines, this.VanillaCaskBigCraftableId);
        if (caskBaseKey != null && machines.TryGetValue(caskBaseKey, out MachineData? caskMachine))
        {
            MachineData metalCaskMachine = CreateMetalCaskMachine(caskMachine);
            machines[MetalCaskQualifiedItemId] = metalCaskMachine;
            machines[MetalCaskItemId] = metalCaskMachine;
        }
        else
        {
            this.Monitor.Log("Failed to find vanilla Cask machine entry in Data/Machines; Metal Cask won't behave like a Cask.", LogLevel.Error);
        }

        string? kegBaseKey = GetKegMachineKey(machines, this.VanillaKegBigCraftableId);
        if (kegBaseKey == null || !machines.TryGetValue(kegBaseKey, out MachineData? kegMachine))
        {
            this.Monitor.Log("Failed to find vanilla Keg machine entry in Data/Machines; Metal Keg won't behave like a Keg.", LogLevel.Error);
            return;
        }

        // Reuse the exact machine model instance from the source machine so behavior stays 1:1.
        string metalQualified = "(BC)" + MetalKegItemId;
        machines[metalQualified] = kegMachine;
        machines[MetalKegItemId] = kegMachine;

        string missingGofMode = (this.Config.MissingGofMode ?? "disable").Trim();
        bool allowHardIridium = this.IsHardIridiumRecipeAllowed(missingGofMode);
        if (!allowHardIridium)
            return;

        MachineData? baseHardwoodMachine = null;
        string? hardwoodKey = GetHardwoodKegMachineKey(machines, this.HardwoodKegBigCraftableId);
        if (hardwoodKey != null && machines.TryGetValue(hardwoodKey, out MachineData? hardwoodMachine))
        {
            baseHardwoodMachine = hardwoodMachine;
        }
        else if (this.GofDetectedByRegistry || this.HardwoodKegDetectedByData)
        {
            this.Monitor.Log("GOF/Hardwood Keg appears present, but Hardwood Keg machine entry wasn't found in Data/Machines. Hard Iridium Keg will fall back to vanilla Keg machine behavior.", LogLevel.Warn);
            baseHardwoodMachine = kegMachine;
        }
        else
        {
            // Missing GOF: allow fallback only if config says so.
            if (missingGofMode.Equals("fallbackVanillaKeg", StringComparison.OrdinalIgnoreCase) || missingGofMode.Equals("fallback", StringComparison.OrdinalIgnoreCase))
                baseHardwoodMachine = kegMachine;
            else
                return;
        }

        string hardQualified = "(BC)" + HardIridiumKegItemId;
        machines[hardQualified] = baseHardwoodMachine;
        machines[HardIridiumKegItemId] = baseHardwoodMachine;
    }

    private static string? GetKegMachineKey(IDictionary<string, MachineData> machines, string? kegBigCraftableId)
    {
        if (!string.IsNullOrWhiteSpace(kegBigCraftableId))
        {
            string candidate = "(BC)" + kegBigCraftableId;
            if (machines.ContainsKey(candidate))
                return candidate;
        }

        if (machines.ContainsKey("(BC)12"))
            return "(BC)12";

        foreach (string key in machines.Keys)
        {
            if (key.Equals("12", StringComparison.OrdinalIgnoreCase) || key.EndsWith(")12", StringComparison.OrdinalIgnoreCase))
                return key;
        }

        return null;
    }

    private static string? GetCaskMachineKey(IDictionary<string, MachineData> machines, string? caskBigCraftableId)
    {
        if (!string.IsNullOrWhiteSpace(caskBigCraftableId))
        {
            string candidate = "(BC)" + caskBigCraftableId;
            if (machines.ContainsKey(candidate))
                return candidate;
        }

        if (machines.ContainsKey("(BC)163"))
            return "(BC)163";

        foreach (string key in machines.Keys)
        {
            if (key.Equals("163", StringComparison.OrdinalIgnoreCase) || key.EndsWith(")163", StringComparison.OrdinalIgnoreCase))
                return key;
        }

        return null;
    }

    private static string? GetHardwoodKegMachineKey(IDictionary<string, MachineData> machines, string? hardwoodBigCraftableId)
    {
        if (!string.IsNullOrWhiteSpace(hardwoodBigCraftableId))
        {
            string candidate = "(BC)" + hardwoodBigCraftableId;
            if (machines.ContainsKey(candidate))
                return candidate;
        }

        foreach (string key in machines.Keys)
        {
            if (key.Contains("Hardwood", StringComparison.OrdinalIgnoreCase) && key.Contains("Keg", StringComparison.OrdinalIgnoreCase))
                return key;
        }

        return null;
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
                    && float.TryParse(customData["AgingMultiplier"]!.GetValue<string>(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float existingMultiplier)
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

    private Texture2D BuildMetalKegTexture()
    {
        if (this.TryLoadCustomSprite(MetalKegCustomSpritePath, out Texture2D? customTexture))
            return customTexture;

        TemplateSpriteInfo src = this.VanillaKegSpriteInfo
            ?? new TemplateSpriteInfo("TileSheets/Craftables", 12);
        if (this.VanillaKegSpriteInfo == null)
            this.Monitor.Log("Metal Keg sprite requested before vanilla template was read; using default craftables keg index 12.", LogLevel.Warn);

        return BuildTintedCraftableSprite(src, new Color(180, 180, 180));
    }

    private Texture2D BuildMetalCaskTexture()
    {
        if (this.TryLoadCustomSprite(MetalCaskCustomSpritePath, out Texture2D? customTexture))
            return customTexture;

        TemplateSpriteInfo src = this.VanillaCaskSpriteInfo
            ?? new TemplateSpriteInfo("TileSheets/Craftables", 163);
        if (this.VanillaCaskSpriteInfo == null)
            this.Monitor.Log("Metal Cask sprite requested before vanilla template was read; using default craftables cask index 163.", LogLevel.Warn);

        return BuildTintedCraftableSprite(src, new Color(150, 150, 170));
    }

    private Texture2D BuildHardIridiumKegTexture()
    {
        if (this.TryLoadCustomSprite(HardIridiumKegCustomSpritePath, out Texture2D? customTexture))
            return customTexture;

        TemplateSpriteInfo src = this.HardwoodKegSpriteInfo
            ?? this.VanillaKegSpriteInfo
            ?? new TemplateSpriteInfo("TileSheets/Craftables", 12);
        if (this.HardwoodKegSpriteInfo == null && this.VanillaKegSpriteInfo == null)
            this.Monitor.Log("Hard Iridium Keg sprite requested before templates were read; using default craftables keg index 12.", LogLevel.Warn);

        return BuildTintedCraftableSprite(src, new Color(90, 50, 130));
    }

    private static Texture2D BuildTintedCraftableSprite(TemplateSpriteInfo src, Color tint)
    {
        string textureAssetName = NormalizeTextureAssetName(src.TextureAssetName);
        Texture2D baseTexture = Game1.content.Load<Texture2D>(textureAssetName);
        const int tileWidth = 16;
        const int tileHeight = 32;

        int tilesPerRow = Math.Max(1, baseTexture.Width / tileWidth);
        int col = src.SpriteIndex % tilesPerRow;
        int row = src.SpriteIndex / tilesPerRow;

        var sourceRect = new Rectangle(col * tileWidth, row * tileHeight, tileWidth, tileHeight);
        var pixels = new Color[tileWidth * tileHeight];
        baseTexture.GetData(0, sourceRect, pixels, 0, pixels.Length);

        for (int i = 0; i < pixels.Length; i++)
        {
            Color p = pixels[i];
            if (p.A == 0)
                continue;

            pixels[i] = new Color(
                (byte)(p.R * tint.R / 255),
                (byte)(p.G * tint.G / 255),
                (byte)(p.B * tint.B / 255),
                p.A);
        }

        Texture2D result = new(Game1.graphics.GraphicsDevice, tileWidth, tileHeight);
        result.SetData(pixels);
        return result;
    }

    private static Texture2D BuildUntintedCraftableSprite(TemplateSpriteInfo src)
    {
        string textureAssetName = NormalizeTextureAssetName(src.TextureAssetName);
        Texture2D baseTexture = Game1.content.Load<Texture2D>(textureAssetName);
        const int tileWidth = 16;
        const int tileHeight = 32;

        int tilesPerRow = Math.Max(1, baseTexture.Width / tileWidth);
        int col = src.SpriteIndex % tilesPerRow;
        int row = src.SpriteIndex / tilesPerRow;

        var sourceRect = new Rectangle(col * tileWidth, row * tileHeight, tileWidth, tileHeight);
        var pixels = new Color[tileWidth * tileHeight];
        baseTexture.GetData(0, sourceRect, pixels, 0, pixels.Length);

        Texture2D result = new(Game1.graphics.GraphicsDevice, tileWidth, tileHeight);
        result.SetData(pixels);
        return result;
    }

    private static KeyValuePair<string, BigCraftableData>? FindBigCraftableTemplate(
        IDictionary<string, BigCraftableData> dict,
        string targetDisplayName,
        string? preferredKey)
    {
        if (preferredKey != null && dict.TryGetValue(preferredKey, out BigCraftableData? preferred) && HasDisplayName(preferred, targetDisplayName))
            return new KeyValuePair<string, BigCraftableData>(preferredKey, preferred);

        foreach (KeyValuePair<string, BigCraftableData> kvp in dict)
        {
            if (HasDisplayName(kvp.Value, targetDisplayName))
                return kvp;
        }

        foreach (KeyValuePair<string, BigCraftableData> kvp in dict)
        {
            if (kvp.Key.Contains(targetDisplayName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                return kvp;
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

    private static T CloneByJson<T>(T template)
    {
        string json = JsonSerializer.Serialize(template, JsonOptions);
        T? clone = JsonSerializer.Deserialize<T>(json, JsonOptions);
        if (clone == null)
            throw new InvalidOperationException($"Failed to clone {typeof(T).FullName} via JSON.");
        return clone;
    }

    private static string NormalizeTextureAssetName(string? rawAssetName)
    {
        if (string.IsNullOrWhiteSpace(rawAssetName))
            return DefaultBigCraftableTexture;

        return rawAssetName.Trim();
    }

    private readonly record struct TemplateSpriteInfo(string TextureAssetName, int SpriteIndex);

    private bool TryLoadCustomSprite(string relativePath, out Texture2D texture)
    {
        string fullPath = Path.Combine(this.Helper.DirectoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            texture = this.Helper.ModContent.Load<Texture2D>(relativePath);
            return true;
        }

        texture = null!;
        return false;
    }

    private void EnsureTemplateSpriteInfoLoaded()
    {
        if (this.VanillaCaskSpriteInfo != null
            && this.VanillaKegSpriteInfo != null
            && (this.HardwoodKegSpriteInfo != null || !this.GofDetectedByRegistry))
        return;

        IDictionary<string, BigCraftableData> bigCraftables = this.Helper.GameContent.Load<Dictionary<string, BigCraftableData>>("Data/BigCraftables");

        if (this.VanillaCaskSpriteInfo == null)
        {
            KeyValuePair<string, BigCraftableData>? caskTemplate = FindBigCraftableTemplate(bigCraftables, "Cask", "163");
            if (caskTemplate != null)
            {
                this.VanillaCaskSpriteInfo = new TemplateSpriteInfo(NormalizeTextureAssetName(caskTemplate.Value.Value.Texture), caskTemplate.Value.Value.SpriteIndex);
                this.VanillaCaskBigCraftableId = caskTemplate.Value.Key;
            }
        }

        if (this.VanillaKegSpriteInfo == null)
        {
            KeyValuePair<string, BigCraftableData>? kegTemplate = FindBigCraftableTemplate(bigCraftables, "Keg", "12");
            if (kegTemplate != null)
            {
                this.VanillaKegSpriteInfo = new TemplateSpriteInfo(NormalizeTextureAssetName(kegTemplate.Value.Value.Texture), kegTemplate.Value.Value.SpriteIndex);
                this.VanillaKegBigCraftableId = kegTemplate.Value.Key;
            }
        }

        if (this.HardwoodKegSpriteInfo == null)
        {
            KeyValuePair<string, BigCraftableData>? hardwoodTemplate = FindBigCraftableTemplate(bigCraftables, "Hardwood Keg", null);
            if (hardwoodTemplate != null)
            {
                this.HardwoodKegSpriteInfo = new TemplateSpriteInfo(NormalizeTextureAssetName(hardwoodTemplate.Value.Value.Texture), hardwoodTemplate.Value.Value.SpriteIndex);
                this.HardwoodKegBigCraftableId = hardwoodTemplate.Value.Key;
                this.HardwoodKegDetectedByData = true;
            }
        }
    }

    private void ApplyHarmonyPatches()
    {
        try
        {
            this.harmony = new Harmony(this.ModManifest.UniqueID);

            MethodInfo? placementTarget = typeof(StardewValley.Object).GetMethod(
                "placementAction",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) },
                modifiers: null);
            if (placementTarget != null)
            {
                this.harmony.Patch(
                    placementTarget,
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(MetalCaskPlacementPrefix)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(MetalCaskPlacementPostfix)));
            }

            MethodInfo? isValidCaskLocationTarget = typeof(Cask).GetMethod(
                "IsValidCaskLocation",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (isValidCaskLocationTarget != null)
            {
                this.harmony.Patch(isValidCaskLocationTarget, postfix: new HarmonyMethod(typeof(ModEntry), nameof(MetalCaskLocationPostfix)));
            }

            MethodInfo? dayUpdateTarget = typeof(Cask).GetMethod(
                "DayUpdate",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (dayUpdateTarget != null)
            {
                this.harmony.Patch(dayUpdateTarget, postfix: new HarmonyMethod(typeof(ModEntry), nameof(MetalCaskDayUpdatePostfix)));
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed to apply Metal Cask runtime patches: {ex}", LogLevel.Error);
        }
    }

    private static bool MetalCaskPlacementPrefix(StardewValley.Object __instance, object[] __args, ref bool __result)
    {
        if (Instance == null || __instance?.QualifiedItemId != MetalCaskQualifiedItemId)
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
        if (Instance == null || !__result || __instance?.QualifiedItemId != MetalCaskQualifiedItemId)
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

        location.objects[tile] = Instance.CreatePlacedMetalCask(tile, placedObject);
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

    private StardewValley.Object CreatePlacedMetalCask(Vector2 tile, StardewValley.Object placedObject)
    {
        Cask metalCask = new(tile);
        this.ApplyMetalCaskIdentity(metalCask, placedObject);

        metalCask.modData.Clear();
        foreach (KeyValuePair<string, string> pair in placedObject.modData.Pairs)
            metalCask.modData[pair.Key] = pair.Value;
        metalCask.modData[MetalCaskMarkerKey] = "true";
        this.UpdateMetalCaskPowerTelemetry(metalCask, Game1.currentLocation, observedSpeedup: 0f, lastAppliedBonusDays: null);

        return metalCask;
    }

    private bool IsMetalCask(Cask cask)
    {
        return cask.QualifiedItemId == MetalCaskQualifiedItemId
            || (cask.modData?.ContainsKey(MetalCaskMarkerKey) ?? false);
    }

    private void ApplyMetalCaskIdentity(Cask cask, StardewValley.Object? sourceObject = null)
    {
        StardewValley.Object identityTemplate = sourceObject ?? ItemRegistry.Create<StardewValley.Object>(MetalCaskQualifiedItemId);

        cask.ItemId = MetalCaskItemId;
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
            this.RehydrateMetalCasks(location);
    }

    private void RehydrateMetalCasks(GameLocation location)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.objects.Pairs)
        {
            if (pair.Value is not Cask cask)
                continue;

            if (!this.IsMetalCask(cask))
                continue;

            if (cask.QualifiedItemId == MetalCaskQualifiedItemId)
                continue;

            this.ApplyMetalCaskIdentity(cask);
        }
    }

    private void RefreshMetalCaskTelemetry()
    {
        foreach (GameLocation location in EnumerateLoadedLocations())
            this.RefreshMetalCaskTelemetry(location);
    }

    private void RefreshMetalCaskTelemetry(GameLocation location)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in location.objects.Pairs)
        {
            if (pair.Value is not Cask cask || !this.IsMetalCask(cask))
                continue;

            float observedSpeedup = 0f;
            if (this.PowerGridApi != null)
            {
                Vector2 tile = cask.TileLocation;
                observedSpeedup = this.PowerGridApi.GetSpeedupAtTile(location.NameOrUniqueName, (int)tile.X, (int)tile.Y);
            }

            this.UpdateMetalCaskPowerTelemetry(cask, location, observedSpeedup, lastAppliedBonusDays: null);
        }
    }

    private static IEnumerable<GameLocation> EnumerateLoadedLocations()
    {
        return Game1.locations;
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
            && this.PowerGridApi != null
            && cask.heldObject.Value != null
            && TryGetDaysToMature(cask, out float currentDays)
            && currentDays > 0f)
        {
            float configuredMaxSpeedup = ClampSpeedup(this.Config.MetalCaskMaxSpeedup);
            if (configuredMaxSpeedup > 0f)
            {
                Vector2 tile = cask.TileLocation;
                observedSpeedup = this.PowerGridApi.GetSpeedupAtTile(location.NameOrUniqueName, (int)tile.X, (int)tile.Y);
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

        this.UpdateMetalCaskPowerTelemetry(cask, location, observedSpeedup, appliedBonusDays);
    }

    private void UpdateMetalCaskPowerTelemetry(Cask cask, GameLocation? location, float observedSpeedup, float? lastAppliedBonusDays)
    {
        cask.modData[MetalCaskPowerModeKey] = "days";

        if (lastAppliedBonusDays.HasValue && lastAppliedBonusDays.Value > 0f)
        {
            cask.modData[MetalCaskLastAppliedBonusDaysKey] = lastAppliedBonusDays.Value.ToString("0.##", CultureInfo.InvariantCulture);
            cask.modData[MetalCaskLastAppliedDateKey] = BuildCurrentGameDateLabel();
        }

        cask.modData[MetalCaskObservedSpeedupKey] = observedSpeedup.ToString("0.####", CultureInfo.InvariantCulture);

        string state = cask.heldObject.Value == null
            ? "empty"
            : !TryGetDaysToMature(cask, out float daysRemaining)
                ? "aging"
                : daysRemaining <= 0f
                    ? "ready"
                    : observedSpeedup > 0f
                        ? "powered"
                        : this.PowerGridApi == null
                            ? "powergrid-unavailable"
                            : "unpowered";

        cask.modData[MetalCaskObservedPowerStateKey] = state;
    }

    private static bool TryGetDaysToMature(Cask cask, out float daysRemaining)
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

    private void CmdExportSprites(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first so patched data from other mods is available.", LogLevel.Info);
            return;
        }

        this.EnsureTemplateSpriteInfoLoaded();

        string exportRoot = Path.Combine(this.Helper.DirectoryPath, "assets", "templates");
        Directory.CreateDirectory(exportRoot);

        if (this.VanillaCaskSpriteInfo is TemplateSpriteInfo caskSprite)
        {
            this.ExportSprite(caskSprite, VanillaCaskTemplateExportPath);
        }
        else
        {
            this.Monitor.Log("Could not locate vanilla Cask sprite metadata in Data/BigCraftables.", LogLevel.Warn);
        }

        if (this.VanillaKegSpriteInfo is TemplateSpriteInfo vanillaSprite)
        {
            this.ExportSprite(vanillaSprite, VanillaKegTemplateExportPath);
        }
        else
        {
            this.Monitor.Log("Could not locate vanilla Keg sprite metadata in Data/BigCraftables.", LogLevel.Warn);
        }

        if (this.HardwoodKegSpriteInfo is TemplateSpriteInfo hardwoodSprite)
        {
            this.ExportSprite(hardwoodSprite, HardwoodKegTemplateExportPath);
        }
        else
        {
            this.Monitor.Log("Could not locate GOF Hardwood Keg sprite metadata. Is GOF installed and loaded?", LogLevel.Warn);
        }

        this.Monitor.Log($"Export complete. Edit PNGs in '{Path.Combine(this.Helper.DirectoryPath, "assets", "templates")}' and copy your final files to:", LogLevel.Info);
        this.Monitor.Log($"- {Path.Combine(this.Helper.DirectoryPath, "assets", "MetalCask.png")}", LogLevel.Info);
        this.Monitor.Log($"- {Path.Combine(this.Helper.DirectoryPath, "assets", "MetalKeg.png")}", LogLevel.Info);
        this.Monitor.Log($"- {Path.Combine(this.Helper.DirectoryPath, "assets", "HardIridiumKeg.png")}", LogLevel.Info);
    }

    private void ExportSprite(TemplateSpriteInfo source, string outputRelativePath)
    {
        string outputPath = Path.Combine(this.Helper.DirectoryPath, outputRelativePath.Replace('/', Path.DirectorySeparatorChar));
        string? parentDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(parentDir))
            Directory.CreateDirectory(parentDir);

        using Texture2D sprite = BuildUntintedCraftableSprite(source);
        using FileStream stream = File.Create(outputPath);
        sprite.SaveAsPng(stream, sprite.Width, sprite.Height);

        this.Monitor.Log($"Exported sprite: {outputPath}", LogLevel.Info);
    }

    private void CmdUnlock(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Info);
            return;
        }

        this.TryGrantRecipes(Game1.player, reason: "ConsoleCommand");
        this.CmdStatus(command, args);
    }

    private void CmdStatus(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Info);
            return;
        }

        Farmer player = Game1.player;
        string unlockMode = (this.Config.UnlockMode ?? "existingProgress").Trim();
        string missingGofMode = (this.Config.MissingGofMode ?? "disable").Trim();

        bool hasCraftedKeg = player.craftingRecipes.TryGetValue("Keg", out int kegCraftCount) && kegCraftCount > 0;
        bool metalCaskEligible = unlockMode.Equals("always", StringComparison.OrdinalIgnoreCase) || this.PlayerHasCellar(player);
        bool metalEligible = unlockMode.Equals("always", StringComparison.OrdinalIgnoreCase) || player.FarmingLevel >= 8 || hasCraftedKeg;

        bool allowHard = this.IsHardIridiumRecipeAllowed(missingGofMode);
        bool hardEligible = unlockMode.Equals("always", StringComparison.OrdinalIgnoreCase)
            ? allowHard
            : allowHard && (this.PlayerHasCellar(player) || this.GetWineShippedCount(player) >= 200);

        this.Monitor.Log($"Config UnlockMode={unlockMode}, MissingGofMode={missingGofMode}", LogLevel.Info);
        this.Monitor.Log($"GOF registry detected={this.GofDetectedByRegistry} ({this.GofUniqueId ?? "n/a"}), HardwoodKeg detected by data={this.HardwoodKegDetectedByData}", LogLevel.Info);
        this.Monitor.Log($"Metal Cask eligible={metalCaskEligible} (hasCellar={this.PlayerHasCellar(player)})", LogLevel.Info);
        this.Monitor.Log($"Metal Keg eligible={metalEligible} (FarmingLevel={player.FarmingLevel}, craftedKegCount={kegCraftCount})", LogLevel.Info);
        this.Monitor.Log($"Hard Iridium allowed={allowHard}, eligible={hardEligible} (hasCellar={this.PlayerHasCellar(player)}, wineShipped={this.GetWineShippedCount(player)})", LogLevel.Info);
        this.Monitor.Log($"Player knows recipes: MetalCask={player.craftingRecipes.ContainsKey(MetalCaskRecipeKey)}, MetalKeg={player.craftingRecipes.ContainsKey(MetalKegRecipeKey)}, HardIridiumKeg={player.craftingRecipes.ContainsKey(HardIridiumKegRecipeKey)}", LogLevel.Info);
    }

    private void CmdMachineStatus(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Info);
            return;
        }

        Dictionary<string, MachineData> machines = this.Helper.GameContent.Load<Dictionary<string, MachineData>>("Data/Machines");
        string metalCaskQualified = "(BC)" + MetalCaskItemId;
        string metalQualified = "(BC)" + MetalKegItemId;
        string hardQualified = "(BC)" + HardIridiumKegItemId;

        bool hasMetalCaskQualified = machines.TryGetValue(metalCaskQualified, out MachineData? metalCaskQualifiedData);
        bool hasMetalCaskUnqualified = machines.TryGetValue(MetalCaskItemId, out MachineData? metalCaskUnqualifiedData);
        bool hasMetalQualified = machines.TryGetValue(metalQualified, out MachineData? metalQualifiedData);
        bool hasMetalUnqualified = machines.TryGetValue(MetalKegItemId, out MachineData? metalUnqualifiedData);
        bool hasHardQualified = machines.TryGetValue(hardQualified, out MachineData? hardQualifiedData);
        bool hasHardUnqualified = machines.TryGetValue(HardIridiumKegItemId, out MachineData? hardUnqualifiedData);

        int metalCaskRules = GetRuleCount(metalCaskQualifiedData) + GetRuleCount(metalCaskUnqualifiedData);
        int metalRules = GetRuleCount(metalQualifiedData) + GetRuleCount(metalUnqualifiedData);
        int hardRules = GetRuleCount(hardQualifiedData) + GetRuleCount(hardUnqualifiedData);

        this.Monitor.Log($"Machine keys for Metal Cask: qualified={hasMetalCaskQualified}, unqualified={hasMetalCaskUnqualified}, outputRules(totalAcrossFoundEntries)={metalCaskRules}", LogLevel.Info);
        this.Monitor.Log($"Machine keys for Metal Keg: qualified={hasMetalQualified}, unqualified={hasMetalUnqualified}, outputRules(totalAcrossFoundEntries)={metalRules}", LogLevel.Info);
        this.Monitor.Log($"Machine keys for Hard Iridium Keg: qualified={hasHardQualified}, unqualified={hasHardUnqualified}, outputRules(totalAcrossFoundEntries)={hardRules}", LogLevel.Info);
    }

    private void CmdPowerStatus(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save first.", LogLevel.Info);
            return;
        }

        GameLocation location = Game1.currentLocation;
        this.RefreshMetalCaskTelemetry(location);

        List<StardewValley.Object> machines = location.objects.Pairs
            .Select(pair => pair.Value)
            .Where(this.IsTrackedPowerMachine)
            .OrderBy(obj => obj.TileLocation.Y)
            .ThenBy(obj => obj.TileLocation.X)
            .ToList();

        if (machines.Count == 0)
        {
            this.Monitor.Log($"No Metal Kegs power consumers found in {location.NameOrUniqueName}.", LogLevel.Info);
            return;
        }

        foreach (StardewValley.Object obj in machines)
        {
            Vector2 tile = obj.TileLocation;
            float liveSpeedup = this.PowerGridApi?.GetSpeedupAtTile(location.NameOrUniqueName, (int)tile.X, (int)tile.Y) ?? 0f;

            if (obj is Cask cask && this.IsMetalCask(cask))
            {
                string powerState = cask.modData.TryGetValue(MetalCaskObservedPowerStateKey, out string? stateText) ? stateText : "unknown";
                string lastBonusDays = cask.modData.TryGetValue(MetalCaskLastAppliedBonusDaysKey, out string? bonusText) ? bonusText : "0";
                string lastBonusDate = cask.modData.TryGetValue(MetalCaskLastAppliedDateKey, out string? dateText) ? dateText : "n/a";
                string daysRemaining = TryGetDaysToMature(cask, out float days)
                    ? $"{days:0.##} day(s) to next quality"
                    : "days-to-mature unavailable";

                this.Monitor.Log(
                    $"Metal Cask [{location.NameOrUniqueName} @ {tile.X},{tile.Y}]: state={powerState}, liveSpeedup={liveSpeedup:P0}, {daysRemaining}, lastOvernightBonus={lastBonusDays} day(s) on {lastBonusDate}",
                    LogLevel.Info);
                continue;
            }

            string powered = obj.modData.TryGetValue("darth.PowerGrid/powered", out string? poweredText) ? poweredText : "0";
            string speedup = obj.modData.TryGetValue("darth.PowerGrid/speedup", out string? speedupText) ? speedupText : "0";
            string accelerated = obj.modData.TryGetValue("darth.PowerGrid/minutesAccelerated", out string? accelText) ? accelText : "0";
            string remaining = obj.modData.TryGetValue("darth.PowerGrid/minutesRemaining", out string? remainingText) ? remainingText : obj.MinutesUntilReady.ToString(CultureInfo.InvariantCulture);

            this.Monitor.Log(
                $"{obj.DisplayName} [{location.NameOrUniqueName} @ {tile.X},{tile.Y}]: powered={(powered == "1" ? "yes" : "no")}, liveSpeedup={liveSpeedup:P0}, lastTickSpeedup={speedup}, lastTickAccel={accelerated} min, remaining={remaining} min",
                LogLevel.Info);
        }
    }

    private bool IsTrackedPowerMachine(StardewValley.Object obj)
    {
        string qualifiedId = obj.QualifiedItemId ?? "";
        if (qualifiedId == MetalKegQualifiedItemId || qualifiedId == HardIridiumKegQualifiedItemId)
            return true;

        return obj is Cask cask && this.IsMetalCask(cask);
    }

    private static int GetRuleCount(MachineData? data)
    {
        return data?.OutputRules?.Count ?? 0;
    }
}
