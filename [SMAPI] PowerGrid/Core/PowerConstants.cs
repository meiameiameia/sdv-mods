using Microsoft.Xna.Framework;

namespace Darth.PowerGrid.Core;

internal static class PowerConstants
{
    public const string ModUniqueId = "meiameiameia.PowerGrid";
    public const string LegacyModUniqueId = "darth.PowerGrid";
    public const string ModPrefix = "darth.PowerGrid_";

    // BigCraftable item IDs (without qualified prefix)
    public const string CopperCableId = ModPrefix + "CopperCable";
    public const string IronCableId = ModPrefix + "IronCable";
    public const string IridiumCableId = ModPrefix + "IridiumCable";

    public const string SteamGeneratorId = ModPrefix + "SteamGenerator";
    public const string WindGeneratorId = ModPrefix + "WindGenerator";

    public const string BasicBatteryId = ModPrefix + "BasicBattery";
    public const string IridiumBatteryId = ModPrefix + "IridiumBattery";

    public const string PowerConduitId = ModPrefix + "PowerConduit";

    // Qualified versions (for Data/Machines keys etc.)
    public static string Q(string itemId) => $"(BC){itemId}";

    // Texture asset keys
    public static string TextureAsset(string uniqueId, string name) => $"Mods/{uniqueId}/{name}";

    // Save data keys
    public const string SaveDataKey = "darth.PowerGrid_BatteryState";
    public const string ConduitSaveDataKey = "darth.PowerGrid_ConduitLinks";
    public const string FuelSaveDataKey = "darth.PowerGrid_FuelState";

    // Simulation
    public const int TickIntervalMinutes = 10;

    public static string MakeNodeKey(string locationName, Vector2 tile, string itemId)
    {
        return FormattableString.Invariant($"{locationName}|{tile.X}|{tile.Y}|{itemId}");
    }
}
