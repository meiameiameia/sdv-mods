using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Darth.PowerGrid.Integrations;

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
        gmcmApi.AddSectionTitle(manifest, () => "Unlocking");

        gmcmApi.AddTextOption(manifest,
            () => config.UnlockMode,
            (string val) => config.UnlockMode = val,
            () => "Unlock Mode",
            () => "Controls when PowerGrid crafting recipes are granted.\n- existingProgress: unlock once you can craft Keg or Lightning Rod (or Mining 6)\n- always: always grant on load/day start\n- disabled: never auto-grant (use console command powergrid_unlock)",
            allowedValues: new[] { "existingProgress", "always", "disabled" });

        gmcmApi.AddBoolOption(manifest,
            () => config.AutoGrantRecipes,
            (bool val) => config.AutoGrantRecipes = val,
            () => "Auto-Grant Recipes",
            () => "If enabled, recipes are granted automatically based on Unlock Mode. If disabled, use the console command powergrid_unlock.");

        // --- Power Tab ---
        gmcmApi.AddSectionTitle(manifest, () => "Power Tab UI");

        gmcmApi.AddBoolOption(manifest,
            () => config.EnablePowerTab,
            (bool val) => config.EnablePowerTab = val,
            () => "Enable Power Tab",
            () => "Allow opening the global Power Tab menu via keybind and console command.");

        gmcmApi.AddTextOption(manifest,
            () => config.PowerTabKeybind.ToString(),
            (string val) =>
            {
                if (KeybindList.TryParse(val, out KeybindList parsed, out string[] _))
                    config.PowerTabKeybind = parsed;
            },
            () => "Power Tab Keybind",
            () => "Keybind list to open the global Power Tab menu (default: P, K). Examples: 'P', 'LeftShift + P', or 'P, K'.");

        // --- Cable Throughput ---
        gmcmApi.AddSectionTitle(manifest, () => "Cable Throughput (EU per tick)");

        gmcmApi.AddNumberOption(manifest,
            () => config.CopperCableThroughput,
            (int val) => config.CopperCableThroughput = val,
            () => "Copper Cable",
            () => "Max EU transferred per 10-minute tick through copper cables.",
            min: 10, max: 1000, interval: 10);

        gmcmApi.AddNumberOption(manifest,
            () => config.IronCableThroughput,
            (int val) => config.IronCableThroughput = val,
            () => "Iron Cable",
            () => "Max EU transferred per 10-minute tick through iron cables.",
            min: 10, max: 2000, interval: 10);

        gmcmApi.AddNumberOption(manifest,
            () => config.IridiumCableThroughput,
            (int val) => config.IridiumCableThroughput = val,
            () => "Iridium Cable",
            () => "Max EU transferred per 10-minute tick through iridium cables.",
            min: 50, max: 5000, interval: 50);

        // --- Generators ---
        gmcmApi.AddSectionTitle(manifest, () => "Generators (EU per tick)");

        gmcmApi.AddNumberOption(manifest,
            () => config.SteamGeneratorEUPerTick,
            (int val) => config.SteamGeneratorEUPerTick = val,
            () => "Steam Generator",
            () => "EU produced per 10-minute tick when fueled.",
            min: 5, max: 500, interval: 5);

        gmcmApi.AddNumberOption(manifest,
            () => config.WindGeneratorEUPerTick,
            (int val) => config.WindGeneratorEUPerTick = val,
            () => "Wind Generator",
            () => "Base EU produced per 10-minute tick (weather modifies this).",
            min: 5, max: 300, interval: 5);

        // --- Batteries ---
        gmcmApi.AddSectionTitle(manifest, () => "Batteries");

        gmcmApi.AddNumberOption(manifest,
            () => config.BasicBatteryCapacity,
            (int val) => config.BasicBatteryCapacity = val,
            () => "Basic Battery Capacity",
            () => "Max EU stored in a Basic Power Battery.",
            min: 100, max: 5000, interval: 100);

        gmcmApi.AddNumberOption(manifest,
            () => config.IridiumBatteryCapacity,
            (int val) => config.IridiumBatteryCapacity = val,
            () => "Iridium Battery Capacity",
            () => "Max EU stored in an Iridium Power Battery.",
            min: 500, max: 20000, interval: 500);

        gmcmApi.AddNumberOption(manifest,
            () => (int)(config.BatteryDailyLeakPercent * 10),
            (int val) => config.BatteryDailyLeakPercent = val / 10f,
            () => "Daily Leak (x0.1%)",
            () => "Percent of stored EU lost each morning. Value is divided by 10 (e.g. 20 = 2.0%).",
            min: 0, max: 100, interval: 1);

        // --- Fuel ---
        gmcmApi.AddSectionTitle(manifest, () => "Fuel (ticks per unit)");

        gmcmApi.AddNumberOption(manifest,
            () => config.CoalFuelTicks,
            (int val) => config.CoalFuelTicks = val,
            () => "Coal",
            () => "Number of 10-minute ticks per Coal.",
            min: 1, max: 30, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.WoodFuelTicks,
            (int val) => config.WoodFuelTicks = val,
            () => "Wood",
            () => "Number of 10-minute ticks per Wood.",
            min: 1, max: 20, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.HardwoodFuelTicks,
            (int val) => config.HardwoodFuelTicks = val,
            () => "Hardwood",
            () => "Number of 10-minute ticks per Hardwood.",
            min: 1, max: 20, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.BatteryPackFuelTicks,
            (int val) => config.BatteryPackFuelTicks = val,
            () => "Battery Pack",
            () => "Number of 10-minute ticks per Battery Pack.",
            min: 1, max: 50, interval: 1);

        // --- Debug ---
        gmcmApi.AddSectionTitle(manifest, () => "Debug");

        gmcmApi.AddBoolOption(manifest,
            () => config.DebugOverlayEnabled,
            (bool val) => config.DebugOverlayEnabled = val,
            () => "Enable Debug Overlay",
            () => "Allow toggling the debug overlay with a keybind.");

        gmcmApi.AddTextOption(manifest,
            () => config.DebugOverlayKeybind,
            (string val) => config.DebugOverlayKeybind = val,
            () => "Debug Overlay Keybind",
            () => "Key to toggle the power grid debug overlay.");
    }
}
