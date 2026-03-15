using StardewModdingAPI.Utilities;

namespace Darth.PowerGrid;

internal sealed class ModConfig
{
    // --- Unlocking ---
    // "existingProgress" = auto-grant recipes once the player has reached a reasonable vanilla milestone.
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
    public int SteamGeneratorEUPerTick { get; set; } = 40;
    public int WindGeneratorEUPerTick { get; set; } = 20;

    // --- Batteries ---
    public int BasicBatteryCapacity { get; set; } = 500;
    public int IridiumBatteryCapacity { get; set; } = 2000;
    public float BatteryDailyLeakPercent { get; set; } = 2f;

    // --- Fuel (ticks per unit; 1 tick = 10 in-game minutes) ---
    public int CoalFuelTicks { get; set; } = 6;
    public int WoodFuelTicks { get; set; } = 2;
    public int HardwoodFuelTicks { get; set; } = 4;
    public int BatteryPackFuelTicks { get; set; } = 12;

    // --- Debug ---
    public bool DebugOverlayEnabled { get; set; } = false;
    public string DebugOverlayKeybind { get; set; } = "F8";
}
