using StardewModdingAPI.Utilities;

namespace Darth.PerfectionAdvisor;

internal sealed class ModConfig
{
    public bool EnableAdvisor { get; set; } = false;
    public bool EnableDetailedSpoilers { get; set; } = false;
    public bool ShowOnlyCategorySummary { get; set; } = true;
    public KeybindList OpenAdvisorKeybind { get; set; } = KeybindList.Parse("F8");
}
