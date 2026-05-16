using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using Meiameiameia.PowerGrid.Core;
using Meiameiameia.PowerGrid.Integrations;

namespace Meiameiameia.PowerGrid.UI;

internal sealed class PowerMonitorMenu : IClickableMenu
{
    private readonly GameLocation location;
    private readonly PowerQueryService powerQuery;
    private int scrollOffset;
    private readonly int maxVisibleLines = 18;

    private readonly List<string> lines = new();

    public PowerMonitorMenu(GameLocation location, PowerQueryService powerQuery)
        : base(
            (int)(Game1.uiViewport.Width * 0.1f),
            (int)(Game1.uiViewport.Height * 0.05f),
            (int)(Game1.uiViewport.Width * 0.8f),
            (int)(Game1.uiViewport.Height * 0.9f),
            showUpperRightCloseButton: true)
    {
        this.location = location;
        this.powerQuery = powerQuery;
        BuildLines();
    }

    private void BuildLines()
    {
        lines.Clear();
        string locName = location.NameOrUniqueName;
        IReadOnlyList<PowerNetworkSnapshot> networks = powerQuery.GetNetworkSnapshots(locName);
        IReadOnlyList<PowerConsumerSnapshot> consumers = powerQuery.GetConsumerSnapshots(locName);
        IReadOnlyList<PowerGeneratorSnapshot> generators = powerQuery.GetGeneratorSnapshots(locName);
        IReadOnlyList<PowerBatterySnapshot> batteries = powerQuery.GetBatterySnapshots(locName);

        lines.Add($"=== Power Monitor: {locName} ===");
        lines.Add("");

        if (networks.Count == 0)
        {
            lines.Add("No power networks detected in this location.");
            lines.Add("Place cables, generators, and batteries to get started.");
            return;
        }

        foreach (PowerNetworkSnapshot net in networks.OrderBy(net => net.NetworkId))
        {
            lines.Add($"--- Network #{net.NetworkId} ---");
            lines.Add($"Generators: {net.GeneratorCount}  |  Cables: {net.CableCount}  |  Batteries: {net.BatteryCount}  |  Consumers: {net.ConsumerCount}");
            lines.Add($"Generation: {net.TotalGenerationPerTick} EU/tick  |  Demand: {net.TotalDemandPerTick} EU/tick");

            string throughput = net.CableThroughputCap <= 0 ? "unlimited" : $"{net.CableThroughputCap} EU";
            lines.Add($"Cable Throughput Cap: {throughput}");
            lines.Add($"Battery Capacity: {net.TotalBatteryCapacity} EU");
            lines.Add("");

            foreach (PowerBatterySnapshot bat in batteries
                .Where(bat => bat.NetworkId == net.NetworkId)
                .OrderBy(bat => bat.TileY)
                .ThenBy(bat => bat.TileX))
            {
                float pct = bat.Capacity > 0 ? (float)bat.Charge / bat.Capacity * 100f : 0f;
                lines.Add($"  Battery ({bat.TileX},{bat.TileY}): {bat.Charge}/{bat.Capacity} EU ({pct:F0}%)");
            }

            foreach (PowerGeneratorSnapshot gen in generators
                .Where(gen => gen.NetworkId == net.NetworkId)
                .OrderBy(gen => gen.TileY)
                .ThenBy(gen => gen.TileX))
            {
                string fuelInfo = gen.RequiresFuel
                    ? $"Fuel ticks left: {gen.FuelTicksRemaining}"
                    : "Passive";
                lines.Add($"  Generator ({gen.TileX},{gen.TileY}): {gen.GenerationPerTick} EU/tick [{fuelInfo}]");
            }

            lines.Add("");

            lines.Add($"Last Tick: Generated={net.LastTickGenerated} EU, Batteries={net.LastTickFromBatteries} EU, Consumed={net.LastTickConsumed} EU, Stored={net.LastTickStoredInBatteries} EU");
            lines.Add("");

            if (consumers.Any(consumer => consumer.NetworkId == net.NetworkId))
            {
                lines.Add("Consumers:");
                foreach (PowerConsumerSnapshot consumer in consumers
                    .Where(consumer => consumer.NetworkId == net.NetworkId)
                    .OrderBy(consumer => consumer.TileY)
                    .ThenBy(consumer => consumer.TileX))
                {
                    lines.Add($"  ({consumer.TileX},{consumer.TileY}) {consumer.DisplayName}: {FormatConsumerLine(consumer, net)}");
                }
            }

            lines.Add("");
        }
    }

    private static string FormatConsumerLine(PowerConsumerSnapshot consumer, PowerNetworkSnapshot network)
    {
        PowerConsumerPowerState state = PowerConsumerPowerStatus.Classify(consumer, network);
        string stateLabel = FormatPowerState(state).ToUpperInvariant();

        if (consumer.ProgressMode == "days" && !string.IsNullOrWhiteSpace(consumer.ProgressText))
        {
            return $"{stateLabel}, EU {consumer.EUAllocated}/{consumer.DemandPerTick}, live speed {consumer.SpeedupFraction:P0}, {consumer.ProgressText}";
        }

        if (consumer.IsProcessing)
        {
            return $"{stateLabel}, EU {consumer.EUAllocated}/{consumer.DemandPerTick}, speed {consumer.SpeedupFraction:P0}, accel {consumer.MinutesAccelerated}m, about {consumer.MinutesRemaining}m remaining";
        }

        return $"{stateLabel}, demand {consumer.DemandPerTick} EU/tick, speed {consumer.SpeedupFraction:P0}";
    }

    private static string FormatPowerState(PowerConsumerPowerState state)
    {
        return state switch
        {
            PowerConsumerPowerState.GridOffline => "grid offline",
            PowerConsumerPowerState.Standby => "standby",
            PowerConsumerPowerState.Powered => "powered",
            PowerConsumerPowerState.LowPower => "low power",
            PowerConsumerPowerState.ProcessingUnpowered => "processing unpowered",
            _ => "not connected"
        };
    }

    public override void draw(SpriteBatch b)
    {
        // Dim background
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

        // Draw menu box
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        // Draw text lines
        int lineHeight = (int)(Game1.smallFont.LineSpacing * 1.1f);
        int textX = xPositionOnScreen + 32;
        int textY = yPositionOnScreen + 32;

        int endLine = Math.Min(scrollOffset + maxVisibleLines, lines.Count);
        for (int i = scrollOffset; i < endLine; i++)
        {
            Color color = Color.Black;
            string line = lines[i];

            if (line.StartsWith("==="))
                color = new Color(60, 60, 160);
            else if (line.StartsWith("---"))
                color = new Color(100, 100, 100);
            else if (line.Contains("POWERED") || line.Contains("STANDBY"))
                color = new Color(20, 140, 20);
            else if (line.Contains("UNPOWERED") || line.Contains("LOW POWER") || line.Contains("GRID OFFLINE"))
                color = new Color(180, 40, 40);

            b.DrawString(Game1.smallFont, line, new Vector2(textX, textY), color);
            textY += lineHeight;
        }

        // Scroll indicators
        if (scrollOffset > 0)
            b.DrawString(Game1.smallFont, "^ Scroll Up ^", new Vector2(textX + width / 2 - 60, yPositionOnScreen + 12), Color.Gray);
        if (endLine < lines.Count)
            b.DrawString(Game1.smallFont, "v Scroll Down v", new Vector2(textX + width / 2 - 60, yPositionOnScreen + height - 28), Color.Gray);

        // Draw close button
        base.draw(b);
        drawMouse(b);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction > 0 && scrollOffset > 0)
            scrollOffset--;
        else if (direction < 0 && scrollOffset + maxVisibleLines < lines.Count)
            scrollOffset++;
    }

    public override void receiveKeyPress(Microsoft.Xna.Framework.Input.Keys key)
    {
        if (key == Microsoft.Xna.Framework.Input.Keys.Escape || key == Microsoft.Xna.Framework.Input.Keys.E)
        {
            exitThisMenu();
            return;
        }
        base.receiveKeyPress(key);
    }
}
