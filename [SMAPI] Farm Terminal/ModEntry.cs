using Darth.FarmTerminal.Integrations.PowerGrid;
using Darth.FarmTerminal.UI.ViewModels;
using DarthMods.API.Power;
using System.IO;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewUI.Framework;
using StardewValley;
using StardewValley.Menus;

namespace Darth.FarmTerminal;

internal sealed class ModEntry : Mod
{
    private const string PowerGridUniqueId = "meiameiameia.PowerGrid";
    private const string StardewUiUniqueId = "focustense.StardewUI";
    private const string OpenCommand = "farmterminal_open";
    private const string ViewAssetPrefix = "Mods/meiameiameia.FarmTerminal/Views";
    private const string ShellAssetName = ViewAssetPrefix + "/TerminalShell";

    private ModConfig config = new();
    private IViewEngine? viewEngine;
    private TerminalViewModel? terminalViewModel;
    private IClickableMenu? activeTerminalMenu;

    public override void Entry(IModHelper helper)
    {
        this.config = helper.ReadConfig<ModConfig>() ?? new ModConfig();

        helper.ConsoleCommands.Add(
            OpenCommand,
            "Open the Farm Terminal read-only dashboard.\nUsage: farmterminal_open",
            this.CmdOpenTerminal);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.viewEngine = this.Helper.ModRegistry.GetApi<IViewEngine>(StardewUiUniqueId);
        if (this.viewEngine == null)
        {
            this.Monitor.Log("Farm Terminal requires StardewUI but could not obtain its API surface.", LogLevel.Error);
            return;
        }

        string viewDirectory = Path.Combine(this.Helper.DirectoryPath, "Assets", "Views");
        this.viewEngine.RegisterViews(ViewAssetPrefix, viewDirectory);
        this.viewEngine.PreloadModels(
            typeof(TerminalViewModel),
            typeof(TerminalTabViewModel));

        PowerGridTerminalQueryService queryService = new(() => this.Helper.ModRegistry.GetApi<IPowerGridApi>(PowerGridUniqueId));
        this.terminalViewModel = new TerminalViewModel(queryService, () => Game1.currentLocation?.NameOrUniqueName);
        this.Monitor.Log("Farm Terminal StardewUI shell registered.", LogLevel.Debug);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return;

        if (Game1.activeClickableMenu != null)
            return;

        if (!this.config.OpenTerminalKeybind.JustPressed())
            return;

        this.OpenTerminal();
    }

    private void CmdOpenTerminal(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Farm Terminal can only be opened after a save is loaded.", LogLevel.Warn);
            return;
        }

        this.OpenTerminal();
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsWorldReady || this.terminalViewModel == null || this.activeTerminalMenu == null)
            return;

        if (!ReferenceEquals(Game1.activeClickableMenu, this.activeTerminalMenu))
            return;

        this.terminalViewModel.Refresh();
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (this.activeTerminalMenu != null && !ReferenceEquals(e.NewMenu, this.activeTerminalMenu))
            this.activeTerminalMenu = null;
    }

    private void OpenTerminal()
    {
        if (this.viewEngine == null)
        {
            this.Monitor.Log("StardewUI is not available, so Farm Terminal cannot open.", LogLevel.Warn);
            return;
        }

        if (this.terminalViewModel == null)
        {
            PowerGridTerminalQueryService queryService = new(() => this.Helper.ModRegistry.GetApi<IPowerGridApi>(PowerGridUniqueId));
            this.terminalViewModel = new TerminalViewModel(queryService, () => Game1.currentLocation?.NameOrUniqueName);
        }

        this.terminalViewModel.Refresh();
        this.activeTerminalMenu = this.viewEngine.CreateMenuFromAsset(ShellAssetName, this.terminalViewModel);
        Game1.activeClickableMenu = this.activeTerminalMenu;
    }
}
