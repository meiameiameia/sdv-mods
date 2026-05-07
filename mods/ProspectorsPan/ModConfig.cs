namespace Meiameiameia.ProspectorsPan;

internal sealed class ModConfig
{
    public bool EnableSpotTuning { get; set; } = true;
    public float SpotSpawnMultiplier { get; set; } = 1.5f;
    public float SpotLifetimeMultiplier { get; set; } = 2.0f;
    public bool EnableReachableSpotAssist { get; set; } = true;
    public float ReachableSpotChance { get; set; } = 0.50f;
    public int ReachableSpotSearchRadius { get; set; } = 8;
    public int AssistedSpotCooldownMinutes { get; set; } = 30;
    public bool EnableBonusRewards { get; set; } = true;
    public RewardGenerosity RewardGenerosity { get; set; } = RewardGenerosity.Standard;
    public bool EnableProgressionRewards { get; set; } = true;
    public bool PreserveSpecialFindChances { get; set; } = true;
    public bool EnablePanHint { get; set; } = true;
    public PanHintPosition PanHintPosition { get; set; } = PanHintPosition.Corner;
    public PanHintMode PanHintMode { get; set; } = PanHintMode.DirectionAndDistance;
    public int PanHintCornerX { get; set; } = 24;
    public int PanHintCornerYFromBottom { get; set; } = 176;
    public bool EnableGingerIslandRewards { get; set; } = true;
}

internal enum RewardGenerosity
{
    Vanilla,
    Standard,
    Generous
}

internal enum PanHintMode
{
    Off,
    DirectionOnly,
    DirectionAndDistance
}

internal enum PanHintPosition
{
    Corner,
    World
}
