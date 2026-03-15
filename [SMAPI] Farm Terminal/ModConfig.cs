using StardewModdingAPI.Utilities;

namespace Darth.FarmTerminal;

internal sealed class ModConfig
{
    public KeybindList OpenTerminalKeybind { get; set; } = KeybindList.Parse("F7");
}
