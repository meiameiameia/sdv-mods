namespace Darth.ElectronicArtisanMachines;

internal sealed class ModConfig
{
    public const int DefaultIndustrialPreservesJarEUPerMinute = 3;
    public const float DefaultIndustrialPreservesJarMaxSpeedup = 0.20f;
    public const int DefaultIndustrialPreservesJarPriority = 10;

    public bool EnablePowerGridIntegration { get; set; } = true;
    public int IndustrialPreservesJarEUPerMinute { get; set; } = DefaultIndustrialPreservesJarEUPerMinute;
    public float IndustrialPreservesJarMaxSpeedup { get; set; } = DefaultIndustrialPreservesJarMaxSpeedup;
    public int IndustrialPreservesJarPriority { get; set; } = DefaultIndustrialPreservesJarPriority;
}
