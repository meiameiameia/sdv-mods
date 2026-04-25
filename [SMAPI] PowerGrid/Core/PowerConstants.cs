using Microsoft.Xna.Framework;

namespace Meiameiameia.PowerGrid.Core;

internal static class PowerConstants
{
    public const string ModUniqueId = "meiameiameia.PowerGrid";
    public const string ModPrefix = "meiameiameia.PowerGrid_";

    // BigCraftable item IDs (without qualified prefix)
    public const string CopperCableId = ModPrefix + "CopperCable";
    public const string IronCableId = ModPrefix + "IronCable";
    public const string IridiumCableId = ModPrefix + "IridiumCable";

    public const string SteamGeneratorId = ModPrefix + "SteamGenerator";
    public const string WindGeneratorId = ModPrefix + "WindGenerator";

    public const string BasicBatteryId = ModPrefix + "BasicBattery";
    public const string IridiumBatteryId = ModPrefix + "IridiumBattery";

    public const string PowerConduitId = ModPrefix + "PowerConduit";
    public const string IndustrialPreservesJarId = ModPrefix + "IndustrialPreservesJar";
    public const string MetalCaskId = ModPrefix + "MetalCask";
    public const string MetalKegId = ModPrefix + "MetalKeg";
    public const string HardIridiumKegId = ModPrefix + "HardIridiumKeg";

    // Qualified versions (for Data/Machines keys etc.)
    public static string Q(string itemId) => $"(BC){itemId}";

    // Texture asset keys
    public static string TextureAsset(string uniqueId, string name) => $"Mods/{uniqueId}/Assets/{name}";

    // Save data keys
    public const string SaveDataKey = "meiameiameia.PowerGrid_BatteryState";
    public const string ConduitSaveDataKey = "meiameiameia.PowerGrid_ConduitLinks";
    public const string FuelSaveDataKey = "meiameiameia.PowerGrid_FuelState";

    // Simulation
    public const int TickIntervalMinutes = 10;

    public static string MakeNodeKey(string locationName, Vector2 tile, string itemId)
    {
        return FormattableString.Invariant($"{locationName}|{tile.X}|{tile.Y}|{itemId}");
    }
}
