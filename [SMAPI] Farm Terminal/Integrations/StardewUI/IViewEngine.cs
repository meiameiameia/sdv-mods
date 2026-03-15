using StardewValley.Menus;

namespace StardewUI.Framework;

/// <summary>
/// Minimal StardewUI API surface used by Farm Terminal.
/// Obtain via helper.ModRegistry.GetApi&lt;IViewEngine&gt;("focustense.StardewUI").
/// </summary>
public interface IViewEngine
{
    IClickableMenu CreateMenuFromAsset(string assetName, object context);
    void RegisterViews(string assetPrefix, string modDirectory);
    void PreloadModels(params Type[] modelTypes);
}
