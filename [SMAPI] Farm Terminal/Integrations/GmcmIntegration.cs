using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Darth.FarmTerminal.Integrations;

public interface IGmcmApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
}

internal static class GmcmIntegration
{
    public static void Register(IModHelper helper, IManifest manifest, ModConfig config, Action resetCallback)
    {
        IGmcmApi? gmcmApi = helper.ModRegistry.GetApi<IGmcmApi>("spacechase0.GenericModConfigMenu");
        if (gmcmApi == null)
            return;

        gmcmApi.Register(manifest, resetCallback, () => helper.WriteConfig(config));

        gmcmApi.AddSectionTitle(manifest, () => "Terminal Access");

        gmcmApi.AddTextOption(manifest,
            () => config.OpenTerminalKeybind.ToString(),
            value =>
            {
                if (KeybindList.TryParse(value, out KeybindList? parsed, out string[]? _) && parsed != null)
                    config.OpenTerminalKeybind = parsed;
            },
            () => "Open Terminal Keybind",
            () => "Keybind list used to open Farm Terminal (default: F7).");
    }
}
