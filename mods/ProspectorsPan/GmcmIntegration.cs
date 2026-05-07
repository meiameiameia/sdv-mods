using StardewModdingAPI;

namespace Meiameiameia.ProspectorsPan;

public interface IGmcmApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
}

internal static class GmcmIntegration
{
    public static void Register(IModHelper helper, IManifest manifest, Func<ModConfig> getConfig, Action<ModConfig> setConfig)
    {
        IGmcmApi? gmcmApi = helper.ModRegistry.GetApi<IGmcmApi>("spacechase0.GenericModConfigMenu");
        if (gmcmApi is null)
        {
            return;
        }

        gmcmApi.Register(
            manifest,
            reset: () => setConfig(new ModConfig()),
            save: () => helper.WriteConfig(getConfig()));

        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.spots"));
        gmcmApi.AddBoolOption(manifest, () => getConfig().EnableSpotTuning, value => getConfig().EnableSpotTuning = value, () => I18n.Get("gmcm.enable-spot-tuning.name"), () => I18n.Get("gmcm.enable-spot-tuning.tooltip"));
        gmcmApi.AddNumberOption(manifest, () => Percent(getConfig().SpotSpawnMultiplier), value => getConfig().SpotSpawnMultiplier = value / 100f, () => I18n.Get("gmcm.spot-spawn-multiplier.name"), () => I18n.Get("gmcm.spot-spawn-multiplier.tooltip"), min: 25, max: 400, interval: 5, formatValue: value => $"{value}%");
        gmcmApi.AddNumberOption(manifest, () => Percent(getConfig().SpotLifetimeMultiplier), value => getConfig().SpotLifetimeMultiplier = value / 100f, () => I18n.Get("gmcm.spot-lifetime-multiplier.name"), () => I18n.Get("gmcm.spot-lifetime-multiplier.tooltip"), min: 50, max: 500, interval: 5, formatValue: value => $"{value}%");
        gmcmApi.AddBoolOption(manifest, () => getConfig().EnableReachableSpotAssist, value => getConfig().EnableReachableSpotAssist = value, () => I18n.Get("gmcm.reachable-assist.name"), () => I18n.Get("gmcm.reachable-assist.tooltip"));
        gmcmApi.AddNumberOption(manifest, () => Percent(getConfig().ReachableSpotChance), value => getConfig().ReachableSpotChance = value / 100f, () => I18n.Get("gmcm.reachable-chance.name"), () => I18n.Get("gmcm.reachable-chance.tooltip"), min: 0, max: 100, interval: 5, formatValue: value => $"{value}%");
        gmcmApi.AddNumberOption(manifest, () => getConfig().AssistedSpotCooldownMinutes, value => getConfig().AssistedSpotCooldownMinutes = value, () => I18n.Get("gmcm.assisted-cooldown.name"), () => I18n.Get("gmcm.assisted-cooldown.tooltip"), min: 0, max: 120, interval: 10);

        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.rewards"));
        gmcmApi.AddBoolOption(manifest, () => getConfig().EnableBonusRewards, value => getConfig().EnableBonusRewards = value, () => I18n.Get("gmcm.bonus-rewards.name"), () => I18n.Get("gmcm.bonus-rewards.tooltip"));
        gmcmApi.AddTextOption(manifest, () => getConfig().RewardGenerosity.ToString(), value =>
        {
            if (Enum.TryParse(value, out RewardGenerosity parsed))
            {
                getConfig().RewardGenerosity = parsed;
            }
        }, () => I18n.Get("gmcm.reward-generosity.name"), () => I18n.Get("gmcm.reward-generosity.tooltip"), allowedValues: Enum.GetNames<RewardGenerosity>(), formatAllowedValue: value => FormatEnumOption<RewardGenerosity>("gmcm.reward-generosity.option", value));
        gmcmApi.AddBoolOption(manifest, () => getConfig().EnableProgressionRewards, value => getConfig().EnableProgressionRewards = value, () => I18n.Get("gmcm.progression-rewards.name"), () => I18n.Get("gmcm.progression-rewards.tooltip"));
        gmcmApi.AddBoolOption(manifest, () => getConfig().EnableGingerIslandRewards, value => getConfig().EnableGingerIslandRewards = value, () => I18n.Get("gmcm.island-rewards.name"), () => I18n.Get("gmcm.island-rewards.tooltip"));

        gmcmApi.AddSectionTitle(manifest, () => I18n.Get("gmcm.section.hints"));
        gmcmApi.AddBoolOption(manifest, () => getConfig().EnablePanHint, value => getConfig().EnablePanHint = value, () => I18n.Get("gmcm.pan-hint.name"), () => I18n.Get("gmcm.pan-hint.tooltip"));
        gmcmApi.AddTextOption(manifest, () => getConfig().PanHintPosition.ToString(), value =>
        {
            if (Enum.TryParse(value, out PanHintPosition parsed))
            {
                getConfig().PanHintPosition = parsed;
            }
        }, () => I18n.Get("gmcm.pan-hint-position.name"), () => I18n.Get("gmcm.pan-hint-position.tooltip"), allowedValues: Enum.GetNames<PanHintPosition>(), formatAllowedValue: value => FormatEnumOption<PanHintPosition>("gmcm.pan-hint-position.option", value));
        gmcmApi.AddTextOption(manifest, () => getConfig().PanHintMode.ToString(), value =>
        {
            if (Enum.TryParse(value, out PanHintMode parsed))
            {
                getConfig().PanHintMode = parsed;
            }
        }, () => I18n.Get("gmcm.pan-hint-mode.name"), () => I18n.Get("gmcm.pan-hint-mode.tooltip"), allowedValues: Enum.GetNames<PanHintMode>(), formatAllowedValue: value => FormatEnumOption<PanHintMode>("gmcm.pan-hint-mode.option", value));
    }

    private static int Percent(float value)
    {
        return (int)MathF.Round(value * 100f);
    }

    private static string FormatEnumOption<TEnum>(string keyPrefix, string value)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, out TEnum parsed)
            ? I18n.Get($"{keyPrefix}.{parsed}")
            : value;
    }
}
