using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using Meiameiameia.PowerGrid;

namespace Meiameiameia.PowerGrid.Integrations;

public interface IGmcmApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
}

internal static class GmcmIntegration
{
    public static void Register(IModHelper helper, IManifest manifest, ModConfig config, Action resetCallback)
    {
        var gmcmApi = helper.ModRegistry.GetApi<IGmcmApi>("spacechase0.GenericModConfigMenu");
        if (gmcmApi == null)
            return;

        gmcmApi.Register(manifest, () =>
        {
            resetCallback();
        }, () =>
        {
            helper.WriteConfig(config);
        });

        // --- Unlocking ---
        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.unlocking"));

        gmcmApi.AddTextOption(manifest,
            () => config.UnlockMode,
            (string val) => config.UnlockMode = val,
            () => I18n.Get("gmcm.unlock-mode.name"),
            () => I18n.Get("gmcm.unlock-mode.tooltip"),
            allowedValues: new[] { "existingProgress", "always", "disabled" });

        gmcmApi.AddBoolOption(manifest,
            () => config.AutoGrantRecipes,
            (bool val) => config.AutoGrantRecipes = val,
            () => I18n.Get("gmcm.auto-grant.name"),
            () => I18n.Get("gmcm.auto-grant.tooltip"));

        // --- Power Tab ---
        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.power-tab"));

        gmcmApi.AddBoolOption(manifest,
            () => config.EnablePowerTab,
            (bool val) => config.EnablePowerTab = val,
            () => I18n.Get("gmcm.enable-power-tab.name"),
            () => I18n.Get("gmcm.enable-power-tab.tooltip"));

        gmcmApi.AddTextOption(manifest,
            () => config.PowerTabKeybind.ToString(),
            (string val) =>
            {
                if (KeybindList.TryParse(val, out KeybindList? parsed, out string[] _) && parsed != null)
                    config.PowerTabKeybind = parsed;
            },
            () => I18n.Get("gmcm.power-tab-keybind.name"),
            () => I18n.Get("gmcm.power-tab-keybind.tooltip"));

        // --- Cable Throughput ---
        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.cable-throughput"));

        gmcmApi.AddNumberOption(manifest,
            () => config.CopperCableThroughput,
            (int val) => config.CopperCableThroughput = val,
            () => I18n.Get("item.copper-cable.name"),
            () => I18n.Get("gmcm.copper-cable-throughput.tooltip"),
            min: 10, max: 1000, interval: 10);

        gmcmApi.AddNumberOption(manifest,
            () => config.IronCableThroughput,
            (int val) => config.IronCableThroughput = val,
            () => I18n.Get("item.iron-cable.name"),
            () => I18n.Get("gmcm.iron-cable-throughput.tooltip"),
            min: 10, max: 2000, interval: 10);

        gmcmApi.AddNumberOption(manifest,
            () => config.IridiumCableThroughput,
            (int val) => config.IridiumCableThroughput = val,
            () => I18n.Get("item.iridium-cable.name"),
            () => I18n.Get("gmcm.iridium-cable-throughput.tooltip"),
            min: 50, max: 5000, interval: 50);

        gmcmApi.AddNumberOption(manifest,
            () => config.EnergizedIridiumCableThroughput,
            (int val) => config.EnergizedIridiumCableThroughput = val,
            () => I18n.Get("item.energized-iridium-cable.name"),
            () => I18n.Get("gmcm.energized-iridium-cable-throughput.tooltip"),
            min: 250, max: 10000, interval: 50);

        // --- Generators ---
        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.generators"));

        gmcmApi.AddNumberOption(manifest,
            () => config.SteamGeneratorEUPerTick,
            (int val) => config.SteamGeneratorEUPerTick = val,
            () => I18n.Get("item.steam-generator.name"),
            () => I18n.Get("gmcm.steam-generator.tooltip"),
            min: 5, max: 500, interval: 5);

        gmcmApi.AddNumberOption(manifest,
            () => config.CombustionGeneratorEUPerTick,
            (int val) => config.CombustionGeneratorEUPerTick = val,
            () => I18n.Get("item.combustion-generator.name"),
            () => I18n.Get("gmcm.combustion-generator.tooltip"),
            min: 10, max: 1000, interval: 10);

        gmcmApi.AddNumberOption(manifest,
            () => config.WindGeneratorEUPerTick,
            (int val) => config.WindGeneratorEUPerTick = val,
            () => I18n.Get("item.wind-generator.name"),
            () => I18n.Get("gmcm.wind-generator.tooltip"),
            min: 5, max: 300, interval: 5);

        // --- Batteries ---
        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.batteries"));

        gmcmApi.AddNumberOption(manifest,
            () => config.BasicBatteryCapacity,
            (int val) => config.BasicBatteryCapacity = val,
            () => I18n.Get("gmcm.basic-battery-capacity.name"),
            () => I18n.Get("gmcm.basic-battery-capacity.tooltip"),
            min: 100, max: 5000, interval: 100);

        gmcmApi.AddNumberOption(manifest,
            () => config.IridiumBatteryCapacity,
            (int val) => config.IridiumBatteryCapacity = val,
            () => I18n.Get("gmcm.iridium-battery-capacity.name"),
            () => I18n.Get("gmcm.iridium-battery-capacity.tooltip"),
            min: 500, max: 20000, interval: 500);

        gmcmApi.AddNumberOption(manifest,
            () => (int)(config.BatteryDailyLeakPercent * 10),
            (int val) => config.BatteryDailyLeakPercent = val / 10f,
            () => I18n.Get("gmcm.daily-leak.name"),
            () => I18n.Get("gmcm.daily-leak.tooltip"),
            min: 0, max: 100, interval: 1);

        // --- Fuel ---
        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.fuel"));

        gmcmApi.AddNumberOption(manifest,
            () => config.CoalFuelTicks,
            (int val) => config.CoalFuelTicks = val,
            () => I18n.Get("gmcm.coal.name"),
            () => I18n.Get("gmcm.coal.tooltip"),
            min: 1, max: 30, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.WoodFuelTicks,
            (int val) => config.WoodFuelTicks = val,
            () => I18n.Get("gmcm.wood.name"),
            () => I18n.Get("gmcm.wood.tooltip"),
            min: 1, max: 20, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.HardwoodFuelTicks,
            (int val) => config.HardwoodFuelTicks = val,
            () => I18n.Get("gmcm.hardwood.name"),
            () => I18n.Get("gmcm.hardwood.tooltip"),
            min: 1, max: 20, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.BiofuelFuelTicks,
            (int val) => config.BiofuelFuelTicks = val,
            () => I18n.Get("item.biofuel.name"),
            () => I18n.Get("gmcm.biofuel.tooltip"),
            min: 1, max: 60, interval: 1);

        // --- PowerGrid-owned machines ---
        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("item.industrial-preserves-jar.name"));

        gmcmApi.AddNumberOption(manifest,
            () => config.IndustrialPreservesJarEUPerMinute,
            (int val) => config.IndustrialPreservesJarEUPerMinute = val,
            () => I18n.Get("gmcm.eu-per-minute.name"),
            () => I18n.Get("gmcm.industrial-preserves-jar-eu.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => (int)MathF.Round(config.IndustrialPreservesJarMaxSpeedup * 100f),
            (int val) => config.IndustrialPreservesJarMaxSpeedup = Math.Clamp(val / 100f, 0f, 1f),
            () => I18n.Get("gmcm.max-speedup-percent.name"),
            () => I18n.Get("gmcm.processing-speedup.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.IndustrialPreservesJarPriority,
            (int val) => config.IndustrialPreservesJarPriority = val,
            () => I18n.Get("gmcm.priority.name"),
            () => I18n.Get("gmcm.priority.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("item.metal-cask.name"));

        gmcmApi.AddNumberOption(manifest,
            () => config.MetalCaskEUPerMinute,
            (int val) => config.MetalCaskEUPerMinute = val,
            () => I18n.Get("gmcm.eu-per-minute.name"),
            () => I18n.Get("gmcm.metal-cask-eu.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => (int)MathF.Round(config.MetalCaskMaxSpeedup * 100f),
            (int val) => config.MetalCaskMaxSpeedup = Math.Clamp(val / 100f, 0f, 1f),
            () => I18n.Get("gmcm.max-speedup-percent.name"),
            () => I18n.Get("gmcm.metal-cask-speedup.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.MetalCaskPriority,
            (int val) => config.MetalCaskPriority = val,
            () => I18n.Get("gmcm.priority.name"),
            () => I18n.Get("gmcm.priority.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("item.metal-keg.name"));

        gmcmApi.AddNumberOption(manifest,
            () => config.MetalKegEUPerMinute,
            (int val) => config.MetalKegEUPerMinute = val,
            () => I18n.Get("gmcm.eu-per-minute.name"),
            () => I18n.Get("gmcm.metal-keg-eu.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => (int)MathF.Round(config.MetalKegMaxSpeedup * 100f),
            (int val) => config.MetalKegMaxSpeedup = Math.Clamp(val / 100f, 0f, 1f),
            () => I18n.Get("gmcm.max-speedup-percent.name"),
            () => I18n.Get("gmcm.metal-keg-speedup.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.MetalKegPriority,
            (int val) => config.MetalKegPriority = val,
            () => I18n.Get("gmcm.priority.name"),
            () => I18n.Get("gmcm.priority.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("item.hard-iridium-keg.name"));

        gmcmApi.AddNumberOption(manifest,
            () => config.HardIridiumKegEUPerMinute,
            (int val) => config.HardIridiumKegEUPerMinute = val,
            () => I18n.Get("gmcm.eu-per-minute.name"),
            () => I18n.Get("gmcm.hard-iridium-keg-eu.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => (int)MathF.Round(config.HardIridiumKegMaxSpeedup * 100f),
            (int val) => config.HardIridiumKegMaxSpeedup = Math.Clamp(val / 100f, 0f, 1f),
            () => I18n.Get("gmcm.max-speedup-percent.name"),
            () => I18n.Get("gmcm.hard-iridium-keg-speedup.tooltip"),
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.HardIridiumKegPriority,
            (int val) => config.HardIridiumKegPriority = val,
            () => I18n.Get("gmcm.priority.name"),
            () => I18n.Get("gmcm.priority.tooltip"),
            min: 0, max: 100, interval: 1);

        // --- Debug ---
        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.debug"));

        gmcmApi.AddBoolOption(manifest,
            () => config.DebugOverlayEnabled,
            (bool val) => config.DebugOverlayEnabled = val,
            () => I18n.Get("gmcm.enable-debug-overlay.name"),
            () => I18n.Get("gmcm.enable-debug-overlay.tooltip"));

        gmcmApi.AddTextOption(manifest,
            () => config.DebugOverlayKeybind,
            (string val) => config.DebugOverlayKeybind = val,
            () => I18n.Get("gmcm.debug-overlay-keybind.name"),
            () => I18n.Get("gmcm.debug-overlay-keybind.tooltip"));
    }
}
