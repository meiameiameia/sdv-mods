using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using Meiameiameia.PowerGrid.Core;
using Meiameiameia.PowerGrid.Integrations;

using SObject = StardewValley.Object;

namespace Meiameiameia.PowerGrid.UI;

internal sealed record MachinePanelSpec(string MachineItemId, int MaxUpgradeSlots, IReadOnlyList<string> SupportedUpgradeIds);

internal sealed class MachinePanelMenu : IClickableMenu
{
    private static readonly Color HeaderColor = new(60, 60, 160);
    private static readonly Color PositiveColor = new(20, 140, 20);
    private static readonly Color WarningColor = new(170, 120, 20);
    private static readonly Color NegativeColor = new(180, 40, 40);
    private static readonly Color MutedColor = new(75, 75, 75);
    private static readonly Color ButtonColor = new(226, 170, 91);
    private static readonly Color DisabledButtonColor = new(145, 130, 110);
    private static readonly Color ModuleFrameColor = new(78, 78, 86);
    private static readonly Color ModulePanelColor = new(42, 45, 54);
    private static readonly Color ModuleInsetColor = new(20, 23, 30);
    private static readonly Color ModuleLineColor = new(112, 118, 130);

    private readonly GameLocation location;
    private readonly Vector2 tile;
    private readonly SObject machine;
    private readonly MachinePanelSpec spec;
    private readonly PowerQueryService powerQuery;
    private readonly Action refreshPower;
    private readonly ClickableComponent installButton;
    private readonly ClickableComponent removeButton;

    private PowerConsumerSnapshot? consumer;
    private PowerNetworkSnapshot? network;
    private MachineUpgradeStateSnapshot upgradeState = new(0, Array.Empty<string>());

    public MachinePanelMenu(GameLocation location, Vector2 tile, SObject machine, MachinePanelSpec spec, PowerQueryService powerQuery, Action refreshPower)
        : base(
            (Game1.uiViewport.Width - 980) / 2,
            (Game1.uiViewport.Height - 520) / 2,
            980,
            520,
            showUpperRightCloseButton: true)
    {
        this.location = location;
        this.tile = tile;
        this.machine = machine;
        this.spec = spec;
        this.powerQuery = powerQuery;
        this.refreshPower = refreshPower;
        this.installButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 324, yPositionOnScreen + height - 112, 260, 64), "install");
        this.removeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 324, yPositionOnScreen + height - 112, 260, 64), "remove");

        Refresh();
    }

    private bool HasInstalledUpgrade => InstalledUpgradeId != null;

    private string? InstalledUpgradeId => spec.SupportedUpgradeIds
        .FirstOrDefault(id => upgradeState.AppliedUpgradeIds.Contains(id, StringComparer.OrdinalIgnoreCase));

    private void Refresh()
    {
        refreshPower();
        consumer = powerQuery.GetConsumerSnapshots(location.NameOrUniqueName)
            .FirstOrDefault(candidate =>
                candidate.TileX == (int)tile.X
                && candidate.TileY == (int)tile.Y
                && string.Equals(candidate.ItemId, spec.MachineItemId, StringComparison.Ordinal));
        network = consumer == null
            ? null
            : powerQuery.GetNetworkSnapshots()
                .FirstOrDefault(candidate =>
                    candidate.NetworkId == consumer.NetworkId
                    && candidate.LocationNames.Any(name => string.Equals(name, consumer.LocationName, StringComparison.Ordinal)));
        upgradeState = MachineUpgradeState.Read(machine, spec.MaxUpgradeSlots);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (HasInstalledUpgrade)
        {
            if (removeButton.containsPoint(x, y))
                RemoveUpgrade();
            return;
        }

        if (installButton.containsPoint(x, y))
            InstallUpgrade();
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape || key == Keys.E)
        {
            exitThisMenu();
            return;
        }

        base.receiveKeyPress(key);
    }

    private void InstallUpgrade()
    {
        if (!upgradeState.HasOpenSlot)
        {
            Game1.addHUDMessage(new HUDMessage(I18n.Get("message.upgrade.slots-full"), HUDMessage.error_type));
            return;
        }

        string? upgradeId = GetHeldCompatibleUpgradeId();
        if (upgradeId == null)
        {
            Game1.addHUDMessage(new HUDMessage(I18n.Get("ui.machine-panel.message.missing-upgrade", new { upgrade = FormatCompatibleUpgradeNames() }), HUDMessage.error_type));
            return;
        }

        if (!TryConsumeUpgradeFromInventory(upgradeId))
        {
            Game1.addHUDMessage(new HUDMessage(I18n.Get("ui.machine-panel.message.missing-upgrade", new { upgrade = GetUpgradeDisplayName(upgradeId) }), HUDMessage.error_type));
            return;
        }

        if (!MachineUpgradeState.TryInstall(machine, upgradeId, spec.MaxUpgradeSlots, out string reason))
        {
            ReturnUpgradeToPlayer(upgradeId);
            string messageKey = reason switch
            {
                "already-installed" => "message.upgrade.already-installed",
                "slots-full" => "message.upgrade.slots-full",
                _ => "message.upgrade.cannot-install"
            };
            Game1.addHUDMessage(new HUDMessage(I18n.Get(messageKey), HUDMessage.error_type));
            Refresh();
            return;
        }

        Game1.playSound("Ship");
        Game1.addHUDMessage(new HUDMessage(I18n.Get("message.upgrade.installed", new { upgrade = GetUpgradeDisplayName(upgradeId) }), HUDMessage.achievement_type));
        Refresh();
    }

    private void RemoveUpgrade()
    {
        string? upgradeId = InstalledUpgradeId;
        if (upgradeId == null || !MachineUpgradeState.TryRemove(machine, upgradeId))
            return;

        ReturnUpgradeToPlayer(upgradeId);
        Game1.playSound("dwop");
        Game1.addHUDMessage(new HUDMessage(I18n.Get("ui.machine-panel.message.removed-upgrade", new { upgrade = GetUpgradeDisplayName(upgradeId) }), HUDMessage.newQuest_type));
        Refresh();
    }

    private string? GetHeldCompatibleUpgradeId()
    {
        return Game1.player.ActiveObject is { } activeObject
            && spec.SupportedUpgradeIds.Contains(activeObject.ItemId, StringComparer.OrdinalIgnoreCase)
            ? activeObject.ItemId
            : null;
    }

    private static bool TryConsumeUpgradeFromInventory(string itemId)
    {
        for (int i = 0; i < Game1.player.Items.Count; i++)
        {
            if (Game1.player.Items[i] is not SObject obj || obj.ItemId != itemId)
                continue;

            obj.Stack--;
            if (obj.Stack <= 0)
                Game1.player.Items[i] = null;

            return true;
        }

        return false;
    }

    private void ReturnUpgradeToPlayer(string itemId)
    {
        SObject upgrade = ItemRegistry.Create<SObject>(PowerConstants.QObject(itemId));
        if (Game1.player.addItemToInventoryBool(upgrade))
            return;

        Game1.createItemDebris(upgrade, Game1.player.getStandingPosition(), Game1.player.FacingDirection, location);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        int left = xPositionOnScreen + 48;
        int top = yPositionOnScreen + 40;
        int lineHeight = (int)(Game1.smallFont.LineSpacing * 1.2f);
        int cardX = xPositionOnScreen + width - 364;
        int cardY = yPositionOnScreen + 104;
        int leftColumnWidth = cardX - left - 32;

        b.DrawString(Game1.dialogueFont, machine.DisplayName, new Vector2(left, top), HeaderColor);
        top += 76;

        DrawSectionHeader(b, I18n.Get("ui.machine-panel.section.status"), left, top);
        top += 40;
        DrawLine(b, FormatMachineStatus(), left, top, GetStatusColor(), leftColumnWidth);
        top += lineHeight;
        DrawLine(b, I18n.Get("ui.machine-panel.location", new { location = FormatLocationName(location), x = (int)tile.X, y = (int)tile.Y }), left, top, MutedColor, leftColumnWidth);

        top += 66;
        DrawSectionHeader(b, I18n.Get("ui.machine-panel.section.power"), left, top);
        top += 40;
        DrawLine(b, FormatPowerLine(), left, top, GetPowerStatusColor(), leftColumnWidth);
        top += lineHeight;
        DrawLine(b, FormatProgressLine(), left, top, MutedColor, leftColumnWidth);

        top += 66;
        DrawSectionHeader(b, I18n.Get("ui.machine-panel.section.upgrades"), left, top);
        top += 40;
        DrawLine(b, FormatUpgradeLine(), left, top, HasInstalledUpgrade ? PositiveColor : MutedColor, leftColumnWidth);

        DrawMachineCard(b, cardX, cardY);
        DrawActionButton(b);

        base.draw(b);
        drawMouse(b);
    }

    private void DrawMachineCard(SpriteBatch b, int x, int y)
    {
        const int cardWidth = 300;
        const int cardHeight = 276;
        const float machineScale = 1.18f;
        const float upgradeScale = 0.56f;

        drawTextureBox(b, x, y, cardWidth, cardHeight, ModuleFrameColor);
        DrawFlatRect(b, new Rectangle(x + 20, y + 20, cardWidth - 40, cardHeight - 40), ModulePanelColor);
        DrawInsetPanel(b, new Rectangle(x + 32, y + 30, 236, 40));

        b.DrawString(Game1.smallFont, I18n.Get("ui.machine-panel.card.title"), new Vector2(x + 44, y + 36), Color.WhiteSmoke);
        DrawStatusLight(b, new Rectangle(x + 230, y + 38, 22, 22));

        Rectangle machineBay = new(x + 106, y + 90, 96, 96);
        DrawInsetPanel(b, machineBay);
        DrawObjectPreview(b, machine, machineBay, machineScale, 1f, new Vector2(6f, 1f));

        Rectangle upgradeSlot = new(x + 130, y + 206, 48, 48);
        DrawFlatRect(b, new Rectangle(machineBay.Center.X - 2, machineBay.Bottom, 4, upgradeSlot.Y - machineBay.Bottom), ModuleLineColor);
        DrawInsetPanel(b, upgradeSlot);
        if (InstalledUpgradeId is { } installedUpgradeId)
        {
            SObject upgrade = ItemRegistry.Create<SObject>(PowerConstants.QObject(installedUpgradeId));
            DrawObjectPreview(b, upgrade, upgradeSlot, upgradeScale, 1f, new Vector2(-14f, -16f));
        }

    }

    private static void DrawObjectPreview(SpriteBatch b, SObject item, Rectangle bay, float scale, float alpha, Vector2 offset)
    {
        Vector2 size = new(64f * scale, 64f * scale);
        Vector2 position = new(
            bay.X + (bay.Width - size.X) / 2f,
            bay.Y + (bay.Height - size.Y) / 2f);
        position += offset;
        item.drawInMenu(b, position, scale, alpha, 1f);
    }

    private void DrawStatusLight(SpriteBatch b, Rectangle bounds)
    {
        PowerConsumerPowerState powerState = PowerConsumerPowerStatus.Classify(consumer, network);
        Color lightColor = PowerConsumerPowerStatus.IsPositive(powerState)
            ? PositiveColor
            : PowerConsumerPowerStatus.IsWarning(powerState)
                ? WarningColor
                : new Color(95, 95, 100);

        DrawInsetPanel(b, bounds);
        DrawFlatRect(b, new Rectangle(bounds.X + 6, bounds.Y + 6, bounds.Width - 12, bounds.Height - 12), lightColor);
    }

    private static void DrawInsetPanel(SpriteBatch b, Rectangle bounds)
    {
        DrawFlatRect(b, bounds, ModuleInsetColor);
        DrawFlatRect(b, new Rectangle(bounds.X, bounds.Y, bounds.Width, 3), Color.Black * 0.65f);
        DrawFlatRect(b, new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), Color.Black * 0.65f);
        DrawFlatRect(b, new Rectangle(bounds.Right - 3, bounds.Y, 3, bounds.Height), Color.White * 0.18f);
        DrawFlatRect(b, new Rectangle(bounds.X, bounds.Bottom - 3, bounds.Width, 3), Color.White * 0.18f);
    }

    private static void DrawFlatRect(SpriteBatch b, Rectangle bounds, Color color)
    {
        b.Draw(Game1.fadeToBlackRect, bounds, color);
    }

    private void DrawActionButton(SpriteBatch b)
    {
        bool installed = HasInstalledUpgrade;
        ClickableComponent button = installed ? removeButton : installButton;
        bool enabled = installed || upgradeState.HasOpenSlot;
        Color color = enabled ? ButtonColor : DisabledButtonColor;
        string label = installed
            ? I18n.Get("ui.machine-panel.button.remove")
            : I18n.Get("ui.machine-panel.button.install");

        drawTextureBox(b, button.bounds.X, button.bounds.Y, button.bounds.Width, button.bounds.Height, color);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(
            Game1.smallFont,
            label,
            new Vector2(button.bounds.X + (button.bounds.Width - size.X) / 2f, button.bounds.Y + (button.bounds.Height - size.Y) / 2f),
            enabled ? Color.Black : MutedColor);
    }

    private static void DrawSectionHeader(SpriteBatch b, string text, int x, int y)
    {
        b.DrawString(Game1.smallFont, text, new Vector2(x, y), HeaderColor);
    }

    private static void DrawLine(SpriteBatch b, string text, int x, int y, Color color, int maxWidth = int.MaxValue)
    {
        if (maxWidth != int.MaxValue)
            text = TrimToPixelWidth(text, Game1.smallFont, maxWidth);

        b.DrawString(Game1.smallFont, text, new Vector2(x, y), color);
    }

    private static string TrimToPixelWidth(string text, SpriteFont font, int maxWidth)
    {
        if (maxWidth <= 0 || font.MeasureString(text).X <= maxWidth)
            return text;

        const string suffix = "...";
        int length = text.Length;
        while (length > 0 && font.MeasureString(text[..length].TrimEnd() + suffix).X > maxWidth)
            length--;

        return length <= 0
            ? suffix
            : text[..length].TrimEnd() + suffix;
    }

    private static string FormatLocationName(GameLocation location)
    {
        string name = location.NameOrUniqueName;
        return string.Equals(name, "FarmHouse", StringComparison.OrdinalIgnoreCase)
            ? I18n.Get("ui.machine-panel.location.farmhouse")
            : name;
    }

    private string FormatMachineStatus()
    {
        if (machine.heldObject.Value != null && machine.MinutesUntilReady <= 0)
            return I18n.Get("ui.machine-panel.status.ready");

        if (machine.MinutesUntilReady > 0)
            return I18n.Get("ui.machine-panel.status.processing", new { minutes = machine.MinutesUntilReady });

        return I18n.Get("ui.machine-panel.status.waiting");
    }

    private Color GetStatusColor()
    {
        if (machine.heldObject.Value != null && machine.MinutesUntilReady <= 0)
            return PositiveColor;

        if (machine.MinutesUntilReady > 0)
            return GetPowerStatusColor();

        return MutedColor;
    }

    private string FormatPowerLine()
    {
        PowerConsumerPowerState state = PowerConsumerPowerStatus.Classify(consumer, network);
        if (state == PowerConsumerPowerState.NotConnected)
            return I18n.Get("ui.machine-panel.power.no-network");

        return I18n.Get("ui.machine-panel.power.line", new
        {
            state = FormatPowerState(state),
            eu = consumer?.EUAllocated ?? 0,
            demand = consumer?.DemandPerTick ?? 0,
            speed = MathF.Round((consumer?.SpeedupFraction ?? 0f) * 100f)
        });
    }

    private string FormatProgressLine()
    {
        PowerConsumerPowerState state = PowerConsumerPowerStatus.Classify(consumer, network);
        if (state == PowerConsumerPowerState.NotConnected || consumer == null)
            return I18n.Get("ui.machine-panel.power.connect");

        if (consumer.IsProcessing)
            return I18n.Get("ui.machine-panel.progress.processing", new { minutes = consumer.MinutesRemaining });

        if (state == PowerConsumerPowerState.GridOffline)
            return I18n.Get("ui.machine-panel.progress.offline");

        return I18n.Get("ui.machine-panel.progress.standby");
    }

    private Color GetPowerStatusColor()
    {
        PowerConsumerPowerState state = PowerConsumerPowerStatus.Classify(consumer, network);
        if (PowerConsumerPowerStatus.IsPositive(state))
            return PositiveColor;

        if (PowerConsumerPowerStatus.IsWarning(state))
            return WarningColor;

        return NegativeColor;
    }

    private static string FormatPowerState(PowerConsumerPowerState state)
    {
        return state switch
        {
            PowerConsumerPowerState.GridOffline => I18n.Get("ui.power-state.grid-offline"),
            PowerConsumerPowerState.Standby => I18n.Get("ui.power-state.standby"),
            PowerConsumerPowerState.Powered => I18n.Get("ui.power-state.powered"),
            PowerConsumerPowerState.LowPower => I18n.Get("ui.power-state.low-power"),
            PowerConsumerPowerState.ProcessingUnpowered => I18n.Get("ui.power-state.processing-unpowered"),
            _ => I18n.Get("ui.power-state.not-connected")
        };
    }

    private string FormatUpgradeLine()
    {
        string? installedUpgradeId = InstalledUpgradeId;
        if (installedUpgradeId != null)
        {
            return I18n.Get("ui.machine-panel.upgrade.installed", new
            {
                upgrade = GetUpgradeDisplayName(installedUpgradeId),
                filled = upgradeState.FilledSlots,
                slots = upgradeState.MaxSlots
            });
        }

        return I18n.Get("ui.machine-panel.upgrade.empty", new
        {
            upgrade = FormatCompatibleUpgradeNames(),
            selected = GetHeldCompatibleUpgradeId() is { } heldUpgradeId ? GetUpgradeDisplayName(heldUpgradeId) : I18n.Get("ui.machine-panel.upgrade.none-held"),
            filled = upgradeState.FilledSlots,
            slots = upgradeState.MaxSlots
        });
    }

    private string FormatCompatibleUpgradeNames()
    {
        return string.Join(", ", spec.SupportedUpgradeIds.Select(GetUpgradeDisplayName));
    }

    private static string GetUpgradeDisplayName(string itemId)
    {
        return ItemRegistry.Create<SObject>(PowerConstants.QObject(itemId)).DisplayName;
    }
}
