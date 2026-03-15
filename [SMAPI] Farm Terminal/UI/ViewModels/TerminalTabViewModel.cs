namespace Darth.FarmTerminal.UI.ViewModels;

internal sealed class TerminalTabViewModel : BindableBase
{
    private readonly string label;
    private readonly string tooltip;
    private bool active;

    public TerminalTabViewModel(string id, string label, string tooltip, bool active = false)
    {
        this.Id = id;
        this.label = label;
        this.tooltip = tooltip;
        this.active = active;
    }

    public string Id { get; }

    public string Label => this.label;

    public string Tooltip => this.tooltip;

    public string ButtonText => this.active ? $"[{this.label}]" : this.label;

    public bool Active
    {
        get => this.active;
        set
        {
            if (!this.SetProperty(ref this.active, value))
                return;

            this.OnPropertyChanged(nameof(this.ButtonText));
        }
    }
}
