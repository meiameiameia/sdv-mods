using StardewModdingAPI.Utilities;

namespace Meiameiameia.PowerGrid;

internal sealed class ModConfig
{
    // --- Unlocking ---
    // "existingProgress" = auto-grant recipe bundles as the player reaches vanilla-aligned milestones.
    // "always" = always grant recipes on load/day start (best for testing).
    // "disabled" = never auto-grant recipes (use console commands instead).
    public string UnlockMode { get; set; } = "existingProgress";
    public bool AutoGrantRecipes { get; set; } = true;

    // --- Power Tab UI ---
    public bool EnablePowerTab { get; set; } = true;
    public KeybindList PowerTabKeybind { get; set; } = KeybindList.Parse("P, K");

    // --- Cable Throughput (EU per tick) ---
    public int CopperCableThroughput { get; set; } = 50;
    public int IronCableThroughput { get; set; } = 150;
    public int IridiumCableThroughput { get; set; } = 500;

    // --- Generators (EU per 10-minute tick) ---
    public int SteamGeneratorEUPerTick { get; set; } = 50;
    public int CombustionGeneratorEUPerTick { get; set; } = 120;
    public int WindGeneratorEUPerTick { get; set; } = 25;

    // --- Batteries ---
    public int BasicBatteryCapacity { get; set; } = 500;
    public int IridiumBatteryCapacity { get; set; } = 2000;
    public float BatteryDailyLeakPercent { get; set; } = 2f;

    // --- Fuel (ticks per unit; 1 tick = 10 in-game minutes) ---
    public int CoalFuelTicks { get; set; } = 12;
    public int WoodFuelTicks { get; set; } = 4;
    public int HardwoodFuelTicks { get; set; } = 8;
    public int BiofuelFuelTicks { get; set; } = 18;

    // --- PowerGrid-owned machines ---
    public int IndustrialPreservesJarEUPerMinute { get; set; } = 2;
    public float IndustrialPreservesJarMaxSpeedup { get; set; } = 0.20f;
    public int IndustrialPreservesJarPriority { get; set; } = 10;
    public int MetalCaskEUPerMinute { get; set; } = 4;
    public float MetalCaskMaxSpeedup { get; set; } = 0.50f;
    public int MetalCaskPriority { get; set; } = 8;
    public int MetalKegEUPerMinute { get; set; } = 1;
    public float MetalKegMaxSpeedup { get; set; } = 0.20f;
    public int MetalKegPriority { get; set; } = 10;
    public int HardIridiumKegEUPerMinute { get; set; } = 3;
    public float HardIridiumKegMaxSpeedup { get; set; } = 0.30f;
    public int HardIridiumKegPriority { get; set; } = 10;

    // --- Debug ---
    public bool DebugOverlayEnabled { get; set; } = false;
    public string DebugOverlayKeybind { get; set; } = "F8";
}
