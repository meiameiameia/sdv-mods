using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Darth.PerfectionAdvisor.Integrations;

public interface IGmcmApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
}

internal static class GmcmIntegration
{
    public static void Register(
        IModHelper helper,
        IManifest manifest,
        Func<ModConfig> getConfig,
        Action resetCallback,
        Action configSavedCallback)
    {
        IGmcmApi? gmcmApi = helper.ModRegistry.GetApi<IGmcmApi>("spacechase0.GenericModConfigMenu");
        if (gmcmApi == null)
            return;

        gmcmApi.Register(manifest, resetCallback, configSavedCallback);

        gmcmApi.AddSectionTitle(manifest, () => "Advisor Access");
        gmcmApi.AddBoolOption(manifest,
            () => getConfig().EnableAdvisor,
            value => getConfig().EnableAdvisor = value,
            () => "Enable Advisor",
            () => "Default OFF. Enables Perfection Advisor summary access.");
        gmcmApi.AddKeybindList(manifest,
            () => getConfig().OpenAdvisorKeybind,
            value => getConfig().OpenAdvisorKeybind = value,
            () => "Open Advisor Key",
            () => "Open the standalone Perfection Advisor panel.");

        gmcmApi.AddSectionTitle(manifest, () => "Spoiler Controls");
        gmcmApi.AddBoolOption(manifest,
            () => getConfig().EnableDetailedSpoilers,
            value => getConfig().EnableDetailedSpoilers = value,
            () => "Enable Detailed Spoilers",
            () => "Default OFF. Allows sample missing-item hints in the advisor panel.");
        gmcmApi.AddBoolOption(manifest,
            () => getConfig().ShowOnlyCategorySummary,
            value => getConfig().ShowOnlyCategorySummary = value,
            () => "Summary Only",
            () => "If enabled, only category-level progress is shown.");
    }
}
