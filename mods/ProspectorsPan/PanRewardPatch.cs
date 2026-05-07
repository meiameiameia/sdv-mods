using HarmonyLib;
using StardewValley;
using StardewValley.Tools;

namespace Meiameiameia.ProspectorsPan;

[HarmonyPatch(typeof(Pan), nameof(Pan.getPanItems))]
internal static class PanRewardPatch
{
    private static void Postfix(GameLocation location, Farmer who, List<Item> __result)
    {
        ModEntry.Instance.AddBonusPanRewards(location, who, __result);
    }
}
