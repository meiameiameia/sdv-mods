using StardewModdingAPI;

namespace Darth.MetalKegs.Integrations;

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
    public static void Register(IModHelper helper, IManifest manifest, ModConfig config, Action resetCallback, Action configSavedCallback)
    {
        IGmcmApi? gmcmApi = helper.ModRegistry.GetApi<IGmcmApi>("spacechase0.GenericModConfigMenu");
        if (gmcmApi == null)
            return;

        gmcmApi.Register(manifest, resetCallback, () =>
        {
            helper.WriteConfig(config);
            configSavedCallback();
        });

        gmcmApi.AddSectionTitle(manifest, () => "Unlocking");

        gmcmApi.AddTextOption(manifest,
            () => config.UnlockMode,
            value => config.UnlockMode = value,
            () => "Unlock Mode",
            () => "Controls when Metal Keg recipes are granted.",
            allowedValues: new[] { "existingProgress", "always" });

        gmcmApi.AddTextOption(manifest,
            () => config.MissingGofMode,
            value => config.MissingGofMode = value,
            () => "Missing GOF Mode",
            () => "How Hard Iridium Keg behaves if Grapes of Ferngill isn't installed.",
            allowedValues: new[] { "disable", "fallbackVanillaKeg", "fallback" });

        gmcmApi.AddSectionTitle(manifest, () => "PowerGrid Integration");

        gmcmApi.AddBoolOption(manifest,
            () => config.EnablePowerGridIntegration,
            value => config.EnablePowerGridIntegration = value,
            () => "Enable PowerGrid",
            () => "If enabled, Metal Kegs registers power-consumer traits when PowerGrid is installed.");

        gmcmApi.AddNumberOption(manifest,
            () => config.MetalCaskEUPerMinute,
            value => config.MetalCaskEUPerMinute = value,
            () => "Metal Cask EU/min",
            () => "EU consumed per in-game minute while Metal Cask receives PowerGrid acceleration.",
            min: 0, max: 40, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => (int)(config.MetalCaskMaxSpeedup * 100),
            value => config.MetalCaskMaxSpeedup = value / 100f,
            () => "Metal Cask Speedup %",
            () => "Maximum PowerGrid speedup percentage for Metal Cask.",
            min: 0, max: 100, interval: 5);

        gmcmApi.AddNumberOption(manifest,
            () => config.MetalCaskPriority,
            value => config.MetalCaskPriority = value,
            () => "Metal Cask Priority",
            () => "Lower values are allocated power first.",
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.MetalKegEUPerMinute,
            value => config.MetalKegEUPerMinute = value,
            () => "Metal Keg EU/min",
            () => "EU consumed per in-game minute while a Metal Keg is processing under PowerGrid.",
            min: 0, max: 20, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => (int)(config.MetalKegMaxSpeedup * 100),
            value => config.MetalKegMaxSpeedup = value / 100f,
            () => "Metal Keg Speedup %",
            () => "Maximum PowerGrid speedup percentage for Metal Kegs.",
            min: 0, max: 50, interval: 5);

        gmcmApi.AddNumberOption(manifest,
            () => config.MetalKegPriority,
            value => config.MetalKegPriority = value,
            () => "Metal Keg Priority",
            () => "Lower values are allocated power first.",
            min: 0, max: 100, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => config.HardIridiumKegEUPerMinute,
            value => config.HardIridiumKegEUPerMinute = value,
            () => "Hard Iridium EU/min",
            () => "EU consumed per in-game minute while a Hard Iridium Keg is processing under PowerGrid.",
            min: 0, max: 40, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => (int)(config.HardIridiumKegMaxSpeedup * 100),
            value => config.HardIridiumKegMaxSpeedup = value / 100f,
            () => "Hard Iridium Speedup %",
            () => "Maximum PowerGrid speedup percentage for Hard Iridium Kegs.",
            min: 0, max: 50, interval: 5);

        gmcmApi.AddNumberOption(manifest,
            () => config.HardIridiumKegPriority,
            value => config.HardIridiumKegPriority = value,
            () => "Hard Iridium Priority",
            () => "Lower values are allocated power first.",
            min: 0, max: 100, interval: 1);
    }
}
