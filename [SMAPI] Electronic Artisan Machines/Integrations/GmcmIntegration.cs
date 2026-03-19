using StardewModdingAPI;

namespace Darth.ElectronicArtisanMachines.Integrations;

public interface IGmcmApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
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

        gmcmApi.AddSectionTitle(manifest, () => "PowerGrid Integration");

        gmcmApi.AddBoolOption(manifest,
            () => config.EnablePowerGridIntegration,
            value => config.EnablePowerGridIntegration = value,
            () => "Enable PowerGrid",
            () => "If enabled, Industrial Preserves Jar registers power-consumer traits when PowerGrid is installed.");

        gmcmApi.AddNumberOption(manifest,
            () => config.IndustrialPreservesJarEUPerMinute,
            value => config.IndustrialPreservesJarEUPerMinute = value,
            () => "Industrial Preserves Jar EU/min",
            () => "EU consumed per in-game minute while Industrial Preserves Jar is processing under PowerGrid.",
            min: 0, max: 40, interval: 1);

        gmcmApi.AddNumberOption(manifest,
            () => (int)(config.IndustrialPreservesJarMaxSpeedup * 100),
            value => config.IndustrialPreservesJarMaxSpeedup = value / 100f,
            () => "Industrial Preserves Jar Speedup %",
            () => "Maximum PowerGrid speedup percentage for Industrial Preserves Jar.",
            min: 0, max: 50, interval: 5);

        gmcmApi.AddNumberOption(manifest,
            () => config.IndustrialPreservesJarPriority,
            value => config.IndustrialPreservesJarPriority = value,
            () => "Industrial Preserves Jar Priority",
            () => "Lower values are allocated power first.",
            min: 0, max: 100, interval: 1);
    }
}
