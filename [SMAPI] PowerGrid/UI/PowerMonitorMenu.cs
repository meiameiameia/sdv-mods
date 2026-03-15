using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using Darth.PowerGrid.Core;

namespace Darth.PowerGrid.UI;

internal sealed class PowerMonitorMenu : IClickableMenu
{
    private readonly GameLocation location;
    private readonly PowerManager powerMgr;
    private readonly BatteryStateManager batteryState;
    private readonly FuelManager fuelMgr;
    private readonly ConduitManager conduitMgr;
    private readonly ModConfig config;

    private readonly List<PowerNetwork> networks;
    private int scrollOffset;
    private readonly int maxVisibleLines = 18;

    private readonly List<string> lines = new();

    public PowerMonitorMenu(GameLocation location, PowerManager powerMgr, BatteryStateManager batteryState,
        FuelManager fuelMgr, ConduitManager conduitMgr, ModConfig config)
        : base(
            (int)(Game1.uiViewport.Width * 0.1f),
            (int)(Game1.uiViewport.Height * 0.05f),
            (int)(Game1.uiViewport.Width * 0.8f),
            (int)(Game1.uiViewport.Height * 0.9f),
            showUpperRightCloseButton: true)
    {
        this.location = location;
        this.powerMgr = powerMgr;
        this.batteryState = batteryState;
        this.fuelMgr = fuelMgr;
        this.conduitMgr = conduitMgr;
        this.config = config;

        networks = powerMgr.GetNetworks(location);
        BuildLines();
    }

    private void BuildLines()
    {
        lines.Clear();
        string locName = location.NameOrUniqueName;
        lines.Add($"=== Power Monitor: {locName} ===");
        lines.Add("");

        if (networks.Count == 0)
        {
            lines.Add("No power networks detected in this location.");
            lines.Add("Place cables, generators, and batteries to get started.");
            return;
        }

        foreach (var net in networks)
        {
            lines.Add($"--- Network #{net.NetworkId} ---");
            lines.Add($"Generators: {net.Generators.Count}  |  Cables: {net.Cables.Count}  |  Batteries: {net.Batteries.Count}  |  Consumers: {net.Consumers.Count}");
            lines.Add($"Generation: {net.TotalGenerationPerTick()} EU/tick  |  Demand: {net.TotalDemandPerTick()} EU/tick");

            string throughput = net.MinCableThroughput == int.MaxValue ? "unlimited" : $"{net.MinCableThroughput} EU";
            lines.Add($"Cable Throughput Cap: {throughput}");
            lines.Add($"Battery Capacity: {net.TotalBatteryCapacity()} EU");
            lines.Add("");

            // Battery details
            foreach (var bat in net.Batteries)
            {
                int charge = batteryState.GetCharge(bat);
                float pct = bat.Capacity > 0 ? (float)charge / bat.Capacity * 100f : 0f;
                lines.Add($"  Battery ({bat.Tile.X},{bat.Tile.Y}): {charge}/{bat.Capacity} EU ({pct:F0}%)");
            }

            // Generator details
            foreach (var gen in net.Generators)
            {
                string fuelInfo = gen.RequiresFuel
                    ? $"Fuel ticks left: {fuelMgr.GetFuelTicksRemaining(gen.UniqueKey)}"
                    : "Passive";
                lines.Add($"  Generator ({gen.Tile.X},{gen.Tile.Y}): {gen.GenerationPerTick} EU/tick [{fuelInfo}]");
            }

            lines.Add("");

            // Last tick report
            var reports = powerMgr.GetLastReports(locName);
            var report = reports.Find(r => r.NetworkId == net.NetworkId);
            if (report != null)
            {
                lines.Add($"Last Tick: Generated={report.TotalGenerated} EU, Batteries={report.TotalFromBatteries} EU, Consumed={report.TotalConsumed} EU, Stored={report.TotalStoredInBatteries} EU");
                lines.Add("");

                if (report.Allocations.Count > 0)
                {
                    lines.Add("Active Consumers:");
                    foreach (var alloc in report.Allocations)
                    {
                        string status = alloc.EUAllocated > 0 ? $"POWERED {alloc.SpeedupFraction:P0} speedup" : "UNPOWERED";
                        lines.Add($"  ({alloc.Consumer.Tile.X},{alloc.Consumer.Tile.Y}) {alloc.Consumer.ItemId}: {alloc.EUAllocated}/{alloc.EUDemanded} EU [{status}]");
                    }
                }
            }

            lines.Add("");
        }

        // Conduit links
        var conduitLinks = conduitMgr.GetAllLinks();
        if (conduitLinks.Count > 0)
        {
            lines.Add("=== Conduit Links ===");
            foreach (var link in conduitLinks)
            {
                lines.Add($"  {link.LocationA} ({link.TileA.X},{link.TileA.Y}) <-> {link.LocationB} ({link.TileB.X},{link.TileB.Y})");
            }
        }
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
            else if (line.Contains("POWERED"))
                color = new Color(20, 140, 20);
            else if (line.Contains("UNPOWERED"))
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
