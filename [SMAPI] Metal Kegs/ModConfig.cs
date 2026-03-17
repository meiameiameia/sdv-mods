namespace Darth.MetalKegs;

internal sealed class ModConfig
{
    public const int DefaultMetalCaskEUPerMinute = 6;
    public const float DefaultMetalCaskMaxSpeedup = 0.50f;
    public const int DefaultMetalCaskPriority = 8;

    public const int DefaultMetalKegEUPerMinute = 2;
    public const float DefaultMetalKegMaxSpeedup = 0.20f;
    public const int DefaultMetalKegPriority = 10;

    public const int DefaultHardIridiumKegEUPerMinute = 4;
    public const float DefaultHardIridiumKegMaxSpeedup = 0.20f;
    public const int DefaultHardIridiumKegPriority = 10;

    public string UnlockMode { get; set; } = "existingProgress";
    public string MissingGofMode { get; set; } = "disable";

    // Optional PowerGrid integration. These settings are owned by Metal Kegs and
    // used when the PowerGrid API is present at runtime.
    public bool EnablePowerGridIntegration { get; set; } = true;
    public int MetalCaskEUPerMinute { get; set; } = DefaultMetalCaskEUPerMinute;
    public float MetalCaskMaxSpeedup { get; set; } = DefaultMetalCaskMaxSpeedup;
    public int MetalCaskPriority { get; set; } = DefaultMetalCaskPriority;

    public int MetalKegEUPerMinute { get; set; } = DefaultMetalKegEUPerMinute;
    public float MetalKegMaxSpeedup { get; set; } = DefaultMetalKegMaxSpeedup;
    public int MetalKegPriority { get; set; } = DefaultMetalKegPriority;

    public int HardIridiumKegEUPerMinute { get; set; } = DefaultHardIridiumKegEUPerMinute;
    public float HardIridiumKegMaxSpeedup { get; set; } = DefaultHardIridiumKegMaxSpeedup;
    public int HardIridiumKegPriority { get; set; } = DefaultHardIridiumKegPriority;

    // Set after Metal Kegs evaluates legacy PowerGrid-owned settings once.
    public bool LegacyPowerGridSettingsMigrated { get; set; } = false;
}
