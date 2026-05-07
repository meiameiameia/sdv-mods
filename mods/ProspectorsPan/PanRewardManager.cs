using StardewValley;

namespace Meiameiameia.ProspectorsPan;

internal sealed class PanRewardManager
{
    private readonly ModConfig config;

    public PanRewardManager(ModConfig config)
    {
        this.config = config;
    }

    public void AddBonusRewards(GameLocation location, Farmer player, IList<Item> rewards)
    {
        if (this.config.RewardGenerosity == RewardGenerosity.Vanilla)
        {
            return;
        }

        Random random = Game1.random;
        if (random.NextDouble() > this.GetBonusChance())
        {
            return;
        }

        RewardTier tier = this.GetRewardTier(player);
        RewardPool pool = this.GetRewardPool(location, tier);
        RewardEntry reward = pool.Choose(random);
        rewards.Add(ItemRegistry.Create(reward.ItemId, random.Next(reward.MinStack, reward.MaxStack + 1)));

        if (this.config.RewardGenerosity == RewardGenerosity.Generous && random.NextDouble() < 0.25)
        {
            RewardEntry extraReward = pool.Choose(random);
            rewards.Add(ItemRegistry.Create(extraReward.ItemId, random.Next(extraReward.MinStack, extraReward.MaxStack + 1)));
        }
    }

    private double GetBonusChance()
    {
        return this.config.RewardGenerosity switch
        {
            RewardGenerosity.Generous => 0.90,
            RewardGenerosity.Standard => 0.45,
            _ => 0.0
        };
    }

    private RewardTier GetRewardTier(Farmer player)
    {
        if (!this.config.EnableProgressionRewards)
        {
            return RewardTier.Basic;
        }

        if (Game1.MasterPlayer.mailReceived.Contains("Visited_Island") || Game1.MasterPlayer.hasOrWillReceiveMail("willyBoatFixed"))
        {
            return RewardTier.Island;
        }

        if (Game1.player.hasSkullKey || player.deepestMineLevel >= 120)
        {
            return RewardTier.Late;
        }

        if (player.deepestMineLevel >= 80)
        {
            return RewardTier.Mid;
        }

        return RewardTier.Basic;
    }

    private RewardPool GetRewardPool(GameLocation location, RewardTier tier)
    {
        string locationName = location.NameOrUniqueName;

        if (tier == RewardTier.Island && this.config.EnableGingerIslandRewards && locationName.Contains("Island", StringComparison.OrdinalIgnoreCase))
        {
            return IslandPool;
        }

        if (locationName.Contains("Beach", StringComparison.OrdinalIgnoreCase))
        {
            return tier >= RewardTier.Mid ? BeachMidPool : BeachBasicPool;
        }

        if (locationName.Contains("Desert", StringComparison.OrdinalIgnoreCase))
        {
            return DesertPool;
        }

        return tier switch
        {
            RewardTier.Island => LatePool,
            RewardTier.Late => LatePool,
            RewardTier.Mid => MidPool,
            _ => BasicPool
        };
    }

    private static readonly RewardPool BasicPool = new(
        new RewardEntry("(O)378", 2, 5, 28),
        new RewardEntry("(O)380", 1, 3, 18),
        new RewardEntry("(O)382", 1, 3, 16),
        new RewardEntry("(O)330", 1, 3, 12),
        new RewardEntry("(O)390", 3, 8, 12),
        new RewardEntry("(O)535", 1, 1, 10),
        new RewardEntry("(O)66", 1, 1, 2),
        new RewardEntry("(O)68", 1, 1, 2));

    private static readonly RewardPool MidPool = new(
        new RewardEntry("(O)380", 1, 4, 26),
        new RewardEntry("(O)384", 1, 2, 14),
        new RewardEntry("(O)382", 1, 4, 14),
        new RewardEntry("(O)536", 1, 1, 12),
        new RewardEntry("(O)537", 1, 1, 10),
        new RewardEntry("(O)60", 1, 1, 3),
        new RewardEntry("(O)62", 1, 1, 3),
        new RewardEntry("(O)70", 1, 1, 2));

    private static readonly RewardPool LatePool = new(
        new RewardEntry("(O)384", 1, 3, 24),
        new RewardEntry("(O)386", 1, 1, 6),
        new RewardEntry("(O)382", 2, 5, 12),
        new RewardEntry("(O)537", 1, 2, 14),
        new RewardEntry("(O)749", 1, 1, 8),
        new RewardEntry("(O)64", 1, 1, 3),
        new RewardEntry("(O)72", 1, 1, 1));

    private static readonly RewardPool BeachBasicPool = new(
        new RewardEntry("(O)330", 1, 4, 22),
        new RewardEntry("(O)382", 1, 3, 18),
        new RewardEntry("(O)390", 3, 8, 16),
        new RewardEntry("(O)372", 1, 2, 12),
        new RewardEntry("(O)393", 1, 2, 10),
        new RewardEntry("(O)535", 1, 1, 8),
        new RewardEntry("(O)60", 1, 1, 2));

    private static readonly RewardPool BeachMidPool = new(
        new RewardEntry("(O)330", 2, 5, 18),
        new RewardEntry("(O)384", 1, 2, 12),
        new RewardEntry("(O)382", 1, 4, 14),
        new RewardEntry("(O)372", 1, 3, 12),
        new RewardEntry("(O)749", 1, 1, 8),
        new RewardEntry("(O)62", 1, 1, 3),
        new RewardEntry("(O)72", 1, 1, 1));

    private static readonly RewardPool DesertPool = new(
        new RewardEntry("(O)384", 1, 3, 24),
        new RewardEntry("(O)386", 1, 1, 5),
        new RewardEntry("(O)382", 2, 5, 14),
        new RewardEntry("(O)749", 1, 1, 10),
        new RewardEntry("(O)64", 1, 1, 3),
        new RewardEntry("(O)72", 1, 1, 2));

    private static readonly RewardPool IslandPool = new(
        new RewardEntry("(O)848", 1, 3, 18),
        new RewardEntry("(O)386", 1, 1, 8),
        new RewardEntry("(O)749", 1, 1, 12),
        new RewardEntry("(O)537", 1, 2, 10),
        new RewardEntry("(O)64", 1, 1, 3),
        new RewardEntry("(O)72", 1, 1, 2));
}

internal readonly record struct RewardEntry(string ItemId, int MinStack, int MaxStack, int Weight);

internal sealed class RewardPool
{
    private readonly RewardEntry[] entries;
    private readonly int totalWeight;

    public RewardPool(params RewardEntry[] entries)
    {
        this.entries = entries;
        this.totalWeight = entries.Sum(entry => entry.Weight);
    }

    public RewardEntry Choose(Random random)
    {
        int roll = random.Next(this.totalWeight);
        foreach (RewardEntry entry in this.entries)
        {
            roll -= entry.Weight;
            if (roll < 0)
            {
                return entry;
            }
        }

        return this.entries[^1];
    }
}

internal enum RewardTier
{
    Basic,
    Mid,
    Late,
    Island
}
