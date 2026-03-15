using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using Darth.PowerGrid.Core;

namespace Darth.PowerGrid.UI;

internal static class DebugOverlay
{
    public static void Draw(SpriteBatch b, GameLocation location, PowerManager powerMgr, BatteryStateManager batteryState)
    {
        var networks = powerMgr.GetNetworks(location);
        if (networks.Count == 0)
            return;

        var consumerPowered = new Dictionary<Vector2, bool>();
        foreach (var report in powerMgr.GetLastReports(location.NameOrUniqueName))
        {
            foreach (var allocation in report.Allocations)
            {
                if (allocation.Consumer.LocationName != location.NameOrUniqueName)
                    continue;

                consumerPowered[allocation.Consumer.Tile] = allocation.EUAllocated > 0;
            }
        }

        // Assign distinct colors per network
        Color[] netColors = new[]
        {
            new Color(0, 200, 0, 80),
            new Color(0, 100, 255, 80),
            new Color(255, 150, 0, 80),
            new Color(200, 0, 200, 80),
            new Color(255, 255, 0, 80),
        };

        for (int n = 0; n < networks.Count; n++)
        {
            Color overlayColor = netColors[n % netColors.Length];
            PowerNetwork net = networks[n];

            // Draw overlay on all nodes in this network
            DrawNodes(b, net.Cables, overlayColor, "C");
            DrawNodes(b, net.Generators, new Color(255, 80, 0, 100), "G");
            DrawNodes(b, net.Batteries, new Color(0, 255, 0, 100), "B");
            DrawConsumerNodes(b, net.Consumers, consumerPowered);
            DrawNodes(b, net.Conduits, new Color(255, 255, 0, 100), "X");
        }
    }

    private static void DrawNodes(SpriteBatch b, List<PowerNode> nodes, Color color, string label)
    {
        foreach (var node in nodes)
        {
            Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, new Vector2(node.Tile.X * 64, node.Tile.Y * 64));

            // Draw colored rectangle overlay
            b.Draw(Game1.fadeToBlackRect, new Rectangle((int)screenPos.X, (int)screenPos.Y, 64, 64), color);

            // Draw label
            b.DrawString(Game1.tinyFont, label, screenPos + new Vector2(2, 2), Color.White);
        }
    }

    private static void DrawConsumerNodes(SpriteBatch b, List<PowerNode> nodes, Dictionary<Vector2, bool> consumerPowered)
    {
        foreach (var node in nodes)
        {
            Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, new Vector2(node.Tile.X * 64, node.Tile.Y * 64));
            bool powered = consumerPowered.TryGetValue(node.Tile, out bool isPowered) && isPowered;

            Color color = powered
                ? new Color(0, 180, 255, 110)
                : new Color(120, 120, 180, 90);
            string label = powered ? "M+" : "M-";

            b.Draw(Game1.fadeToBlackRect, new Rectangle((int)screenPos.X, (int)screenPos.Y, 64, 64), color);
            b.DrawString(Game1.tinyFont, label, screenPos + new Vector2(2, 2), Color.White);
        }
    }
}
