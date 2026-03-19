using System.Text.Json;
using Darth.ElectronicArtisanMachines.Integrations;
using DarthMods.API.Power;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;

namespace Darth.ElectronicArtisanMachines;

internal sealed class ModEntry : Mod
{
    private const string IndustrialPreservesJarItemId = "darth.ElectronicArtisanMachines_IndustrialPreservesJar";
    private const string IndustrialPreservesJarRecipeKey = "Industrial Preserves Jar";
    private const string IndustrialPreservesJarQualifiedItemId = "(BC)" + IndustrialPreservesJarItemId;
    private const string IndustrialPreservesJarCustomSpritePath = "assets/IndustrialPreservesJar.png";
    private const int PowerGridTickIntervalMinutes = 10;
    private const string PowerGridUniqueId = "meiameiameia.PowerGrid";
    private const string LegacyPowerGridUniqueId = "darth.PowerGrid";
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

    private ModConfig Config = new();
    private IPowerGridApi? PowerGridApi;
    private string? vanillaPreservesJarBigCraftableId;
    private bool loggedMissingTemplate;
    private bool loggedMissingMachineTemplate;

    private string IndustrialPreservesJarTextureAsset => $"Mods/{this.ModManifest.UniqueID}/IndustrialPreservesJar";
    private bool HasCustomIndustrialPreservesJarTexture => File.Exists(
        Path.Combine(this.Helper.DirectoryPath, IndustrialPreservesJarCustomSpritePath.Replace('/', Path.DirectorySeparatorChar)));

    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>() ?? new ModConfig();

        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GmcmIntegration.Register(this.Helper, this.ModManifest, this.Config, () =>
        {
            this.Config = this.Helper.ReadConfig<ModConfig>() ?? new ModConfig();
        }, this.SyncPowerGridConsumer);

        this.SyncPowerGridConsumer();
    }

    private void SyncPowerGridConsumer()
    {
        this.PowerGridApi = this.Helper.ModRegistry.GetApi<IPowerGridApi>(PowerGridUniqueId)
            ?? this.Helper.ModRegistry.GetApi<IPowerGridApi>(LegacyPowerGridUniqueId);
        if (this.PowerGridApi == null)
            return;

        if (!this.Config.EnablePowerGridIntegration)
        {
            this.PowerGridApi.UnregisterConsumer(IndustrialPreservesJarQualifiedItemId);
            return;
        }

        this.PowerGridApi.RegisterConsumer(
            IndustrialPreservesJarQualifiedItemId,
            GetDemandPerTick(this.Config.IndustrialPreservesJarEUPerMinute),
            ClampSpeedup(this.Config.IndustrialPreservesJarMaxSpeedup),
            Math.Max(0, this.Config.IndustrialPreservesJarPriority),
            IndustrialPreservesJarRecipeKey);
    }

    private static int GetDemandPerTick(int euPerMinute)
    {
        return Math.Max(0, euPerMinute) * PowerGridTickIntervalMinutes;
    }

    private static float ClampSpeedup(float speedup)
    {
        return Math.Clamp(speedup, 0f, 1f);
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

        if (e.NameWithoutLocale.IsEquivalentTo(this.IndustrialPreservesJarTextureAsset) && this.HasCustomIndustrialPreservesJarTexture)
        {
            e.LoadFromModFile<Texture2D>(IndustrialPreservesJarCustomSpritePath, AssetLoadPriority.Exclusive);
        }
    }

    private void EditBigCraftables(IAssetData asset)
    {
        IDictionary<string, BigCraftableData> dict = asset.AsDictionary<string, BigCraftableData>().Data;
        KeyValuePair<string, BigCraftableData>? templateOpt =
            FindBigCraftableTemplate(dict, targetDisplayName: "Preserves Jar", preferredKey: "15");

        if (templateOpt == null)
        {
            if (!this.loggedMissingTemplate)
            {
                this.Monitor.Log("Failed to find vanilla Preserves Jar template in Data/BigCraftables; Industrial Preserves Jar was not added.", LogLevel.Error);
                this.loggedMissingTemplate = true;
            }

            return;
        }

        KeyValuePair<string, BigCraftableData> template = templateOpt.Value;
        this.vanillaPreservesJarBigCraftableId = template.Key;

        BigCraftableData industrialData = CloneByJson(template.Value);
        industrialData.Name = IndustrialPreservesJarRecipeKey;
        industrialData.DisplayName = IndustrialPreservesJarRecipeKey;
        industrialData.Description = "An industrial preserves jar built for powered throughput.";
        if (this.HasCustomIndustrialPreservesJarTexture)
        {
            industrialData.Texture = this.IndustrialPreservesJarTextureAsset;
            industrialData.SpriteIndex = 0;
        }

        dict[IndustrialPreservesJarItemId] = industrialData;
    }

    private void EditCraftingRecipes(IAssetData asset)
    {
        IDictionary<string, string> recipes = asset.AsDictionary<string, string>().Data;
        string? template = recipes.TryGetValue("Preserves Jar", out string? value) ? value : null;
        recipes[IndustrialPreservesJarRecipeKey] = BuildRecipeFromTemplate(template, IndustrialPreservesJarItemId);
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

    private void EditMachines(IAssetData asset)
    {
        IDictionary<string, MachineData> machines = asset.AsDictionary<string, MachineData>().Data;
        string? templateKey = GetPreservesJarMachineKey(machines, this.vanillaPreservesJarBigCraftableId);
        if (templateKey == null || !machines.TryGetValue(templateKey, out MachineData? template))
        {
            if (!this.loggedMissingMachineTemplate)
            {
                this.Monitor.Log("Failed to find vanilla Preserves Jar machine entry in Data/Machines; Industrial Preserves Jar machine rules were not added.", LogLevel.Error);
                this.loggedMissingMachineTemplate = true;
            }

            return;
        }

        MachineData machine = CloneMachineByJson(template);
        machines[IndustrialPreservesJarQualifiedItemId] = machine;
        machines[IndustrialPreservesJarItemId] = machine;
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

    private static KeyValuePair<string, BigCraftableData>? FindBigCraftableTemplate(
        IDictionary<string, BigCraftableData> dict,
        string targetDisplayName,
        string? preferredKey)
    {
        if (!string.IsNullOrWhiteSpace(preferredKey) && dict.TryGetValue(preferredKey, out BigCraftableData? byKey) && HasDisplayName(byKey, targetDisplayName))
            return new KeyValuePair<string, BigCraftableData>(preferredKey, byKey);

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

    private static T CloneByJson<T>(T template)
    {
        string json = JsonSerializer.Serialize(template, JsonOptions);
        T? clone = JsonSerializer.Deserialize<T>(json, JsonOptions);
        if (clone == null)
            throw new InvalidOperationException($"Failed to clone {typeof(T).FullName} via JSON.");
        return clone;
    }

    private static MachineData CloneMachineByJson(MachineData template)
    {
        string json = JsonSerializer.Serialize(template, GameDataCloneJsonOptions);
        MachineData? clone = JsonSerializer.Deserialize<MachineData>(json, GameDataCloneJsonOptions);
        if (clone == null)
            throw new InvalidOperationException("Failed to clone MachineData via JSON.");
        return clone;
    }
}
