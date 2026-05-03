using StardewModdingAPI;

namespace Meiameiameia.PowerGrid;

internal static class I18n
{
    private static ITranslationHelper? translations;

    public static void Init(ITranslationHelper translationHelper)
    {
        translations = translationHelper;
    }

    public static string Get(string key)
    {
        return translations?.Get(key).ToString() ?? key;
    }

    public static string Get(string key, object tokens)
    {
        return translations?.Get(key, tokens).ToString() ?? key;
    }
}
