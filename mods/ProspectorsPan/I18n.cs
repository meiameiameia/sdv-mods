using StardewModdingAPI;

namespace Meiameiameia.ProspectorsPan;

internal static class I18n
{
    private static ITranslationHelper translations = null!;

    public static void Init(ITranslationHelper translationHelper)
    {
        translations = translationHelper;
    }

    public static string Get(string key)
    {
        return translations.Get(key);
    }
}
