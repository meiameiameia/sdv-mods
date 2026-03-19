using Darth.PerfectionAdvisor.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace Darth.PerfectionAdvisor;

internal sealed class ModEntry : Mod
{
    private const string OpenCommand = "perfection_advisor_open";

    private ModConfig config = new();
    private PerfectionSummaryService? summaryService;

    public override void Entry(IModHelper helper)
    {
        this.config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
        this.summaryService = new PerfectionSummaryService(helper);

        helper.ConsoleCommands.Add(
            OpenCommand,
            "Open the standalone Perfection Advisor summary.\nUsage: perfection_advisor_open",
            this.CmdOpenAdvisor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GmcmIntegration.Register(this.Helper, this.ModManifest, this.config, () =>
        {
            this.config = new ModConfig();
        }, () =>
        {
            this.config = this.Helper.ReadConfig<ModConfig>() ?? new ModConfig();
        });
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return;
        if (Game1.activeClickableMenu != null)
            return;
        if (!this.config.OpenAdvisorKeybind.JustPressed())
            return;

        this.OpenAdvisor();
    }

    private void CmdOpenAdvisor(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Perfection Advisor can only be opened after a save is loaded.", LogLevel.Warn);
            return;
        }

        this.OpenAdvisor();
    }

    private void OpenAdvisor()
    {
        if (!this.config.EnableAdvisor)
        {
            Game1.showRedMessage("Perfection Advisor is disabled. Enable it in config or GMCM.");
            this.Monitor.Log("Perfection Advisor remains disabled by default until explicitly enabled by the player.", LogLevel.Info);
            return;
        }

        if (this.summaryService == null)
        {
            this.Monitor.Log("Summary service is not ready.", LogLevel.Warn);
            return;
        }

        string summaryText = this.summaryService.BuildSummary(this.config);
        Game1.activeClickableMenu = new DialogueBox(summaryText);
    }
}
