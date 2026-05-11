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
    public const string EnergizedIridiumCableId = ModPrefix + "EnergizedIridiumCable";

    public const string BiofuelId = ModPrefix + "Biofuel";
    public const string RadioisotopeFuelId = ModPrefix + "RadioisotopeFuel";
    public const string HeatingCoilId = ModPrefix + "HeatingCoil";
    public const string EfficiencyCoreId = ModPrefix + "EfficiencyCore";
    public const string CatalystChamberId = ModPrefix + "CatalystChamber";
    public const string SortingMagnetId = ModPrefix + "SortingMagnet";
    public const string DryingRackArrayId = ModPrefix + "DryingRackArray";
    public const string HeatRegulatorId = ModPrefix + "HeatRegulator";

    public const string SteamGeneratorId = ModPrefix + "SteamGenerator";
    public const string CombustionGeneratorId = ModPrefix + "CombustionGenerator";
    public const string RadioisotopeGeneratorId = ModPrefix + "RadioisotopeGenerator";
    public const string WindGeneratorId = ModPrefix + "WindGenerator";

    public const string BasicBatteryId = ModPrefix + "BasicBattery";
    public const string IridiumBatteryId = ModPrefix + "IridiumBattery";

    public const string PowerConduitId = ModPrefix + "PowerConduit";
    public const string IndustrialPreservesJarId = ModPrefix + "IndustrialPreservesJar";
    public const string MetalCaskId = ModPrefix + "MetalCask";
    public const string MetalKegId = ModPrefix + "MetalKeg";
    public const string HardIridiumKegId = ModPrefix + "HardIridiumKeg";
    public const string ElectricSmelterId = ModPrefix + "ElectricSmelter";
    public const string IndustrialRecyclerId = ModPrefix + "IndustrialRecycler";
    public const string PoweredDehydratorId = ModPrefix + "PoweredDehydrator";

    // Qualified versions (for Data/Machines keys etc.)
    public static string Q(string itemId) => $"(BC){itemId}";
    public static string QObject(string itemId) => $"(O){itemId}";

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
