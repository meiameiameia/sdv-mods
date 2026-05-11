using System.Text.Json;

using SObject = StardewValley.Object;

namespace Meiameiameia.PowerGrid.Core;

internal sealed record MachineUpgradeStateSnapshot(int MaxSlots, IReadOnlyList<string> AppliedUpgradeIds)
{
    public int FilledSlots => AppliedUpgradeIds.Count;
    public bool HasOpenSlot => FilledSlots < MaxSlots;
}

internal static class MachineUpgradeState
{
    private const string MdUpgradeIds = "meiameiameia.PowerGrid/upgrades";

    public static MachineUpgradeStateSnapshot Read(SObject? machine, int maxSlots)
    {
        int clampedSlots = Math.Max(0, maxSlots);
        IReadOnlyList<string> upgradeIds = ReadUpgradeIds(machine);

        if (upgradeIds.Count > clampedSlots)
            upgradeIds = upgradeIds.Take(clampedSlots).ToArray();

        return new MachineUpgradeStateSnapshot(clampedSlots, upgradeIds);
    }

    public static bool HasUpgrade(SObject? machine, string upgradeId)
    {
        if (string.IsNullOrWhiteSpace(upgradeId))
            return false;

        return ReadUpgradeIds(machine).Contains(upgradeId, StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryInstall(SObject? machine, string upgradeId, int maxSlots, out string reason)
    {
        reason = "";

        if (machine == null)
        {
            reason = "missing-machine";
            return false;
        }

        if (maxSlots <= 0)
        {
            reason = "no-slots";
            return false;
        }

        if (string.IsNullOrWhiteSpace(upgradeId))
        {
            reason = "missing-upgrade";
            return false;
        }

        List<string> upgradeIds = ReadUpgradeIds(machine).ToList();
        if (upgradeIds.Contains(upgradeId, StringComparer.OrdinalIgnoreCase))
        {
            reason = "already-installed";
            return false;
        }

        if (upgradeIds.Count >= maxSlots)
        {
            reason = "slots-full";
            return false;
        }

        upgradeIds.Add(upgradeId);
        WriteUpgradeIds(machine, upgradeIds);
        return true;
    }

    public static bool TryRemove(SObject? machine, string upgradeId)
    {
        if (machine == null || string.IsNullOrWhiteSpace(upgradeId))
            return false;

        List<string> upgradeIds = ReadUpgradeIds(machine).ToList();
        int removed = upgradeIds.RemoveAll(id => string.Equals(id, upgradeId, StringComparison.OrdinalIgnoreCase));
        if (removed <= 0)
            return false;

        WriteUpgradeIds(machine, upgradeIds);
        return true;
    }

    public static void Clear(SObject? machine)
    {
        machine?.modData.Remove(MdUpgradeIds);
    }

    public static bool TryGetSerialized(SObject? machine, out string serializedUpgradeIds)
    {
        serializedUpgradeIds = "";
        if (machine == null
            || !machine.modData.TryGetValue(MdUpgradeIds, out string? raw)
            || string.IsNullOrWhiteSpace(raw)
            || ReadUpgradeIds(machine).Count == 0)
        {
            return false;
        }

        serializedUpgradeIds = raw;
        return true;
    }

    public static void SetSerialized(SObject? machine, string serializedUpgradeIds)
    {
        if (machine == null || string.IsNullOrWhiteSpace(serializedUpgradeIds))
            return;

        machine.modData[MdUpgradeIds] = serializedUpgradeIds;
    }

    private static IReadOnlyList<string> ReadUpgradeIds(SObject? machine)
    {
        if (machine == null || !machine.modData.TryGetValue(MdUpgradeIds, out string? raw) || string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        try
        {
            string[]? upgradeIds = JsonSerializer.Deserialize<string[]>(raw);
            if (upgradeIds == null || upgradeIds.Length == 0)
                return Array.Empty<string>();

            return upgradeIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static void WriteUpgradeIds(SObject machine, IReadOnlyList<string> upgradeIds)
    {
        string[] cleanIds = upgradeIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (cleanIds.Length == 0)
        {
            machine.modData.Remove(MdUpgradeIds);
            return;
        }

        machine.modData[MdUpgradeIds] = JsonSerializer.Serialize(cleanIds);
    }
}
