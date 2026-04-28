using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.IO;

namespace Meiameiameia.PowerGrid;

/// <summary>
/// Temporary utility to extract vanilla sprite sources used for placeholders.
/// Run this once in-game via console command, then delete this file.
/// </summary>
internal static class PlaceholderExtractor
{
    public static void ExtractAll(IModHelper helper, IMonitor monitor)
    {
        string assetsPath = Path.Combine(helper.DirectoryPath, "Assets");
        Directory.CreateDirectory(assetsPath);

        Texture2D craftables = Game1.content.Load<Texture2D>("TileSheets/Craftables");
        const int tileW = 16;
        const int tileH = 32;
        int tilesPerRow = craftables.Width / tileW;

        // Non-cable items (single 16x32 sprites)
        var items = new[]
        {
            ("SteamGenerator", 13, new Color(180, 100, 80)),      // Furnace base
            ("WindGenerator", 10, new Color(200, 200, 220)),      // Bee House base
            ("BasicBattery", 36, new Color(255, 220, 100)),       // Lightning Rod base
            ("IridiumBattery", 36, new Color(180, 100, 255)),     // Lightning Rod base (purple tint)
            ("PowerConduit", 8, new Color(100, 200, 150))         // Scarecrow base
        };

        foreach (var (name, index, tint) in items)
        {
            ExtractSingle(craftables, name, index, tint, tilesPerRow, assetsPath, monitor);
        }

        // Cables (64x128 autotiling spritesheets)
        var cables = new[]
        {
            ("CopperCable", new Color(255, 140, 60)),
            ("IronCable", new Color(200, 200, 210)),
            ("IridiumCable", new Color(180, 100, 255))
        };

        foreach (var (name, tint) in cables)
        {
            ExtractCableSheet(name, tint, assetsPath, monitor);
        }

        monitor.Log($"[PlaceholderExtractor] Extracted all placeholder sprites to {assetsPath}", LogLevel.Info);
    }

    private static void ExtractSingle(Texture2D source, string name, int index, Color tint, int tilesPerRow, string outputPath, IMonitor monitor)
    {
        const int tileW = 16;
        const int tileH = 32;

        int col = index % tilesPerRow;
        int row = index / tilesPerRow;
        var srcRect = new Rectangle(col * tileW, row * tileH, tileW, tileH);

        var pixels = new Color[tileW * tileH];
        source.GetData(0, srcRect, pixels, 0, pixels.Length);

        // Apply tint
        for (int i = 0; i < pixels.Length; i++)
        {
            Color p = pixels[i];
            if (p.A == 0) continue;
            pixels[i] = new Color(
                (byte)(p.R * tint.R / 255),
                (byte)(p.G * tint.G / 255),
                (byte)(p.B * tint.B / 255),
                p.A);
        }

        Texture2D result = new(Game1.graphics.GraphicsDevice, tileW, tileH);
        result.SetData(pixels);

        string filePath = Path.Combine(outputPath, $"{name}.png");
        using (FileStream fs = File.Create(filePath))
        {
            result.SaveAsPng(fs, tileW, tileH);
        }

        monitor.Log($"[PlaceholderExtractor] Saved {name}.png", LogLevel.Debug);
    }

    private static void ExtractCableSheet(string name, Color tint, string outputPath, IMonitor monitor)
    {
        const int tileW = 16;
        const int tileH = 32;
        const int sheetW = tileW * 4;
        const int sheetH = tileH * 4;

        var pixels = new Color[sheetW * sheetH];

        // Generate 16-frame autotiling spritesheet
        for (int mask = 0; mask < 16; mask++)
        {
            int col = mask % 4;
            int row = mask / 4;
            int startX = col * tileW;
            int startY = row * tileH;

            for (int y = 0; y < tileH; y++)
            {
                for (int x = 0; x < tileW; x++)
                {
                    // Draw a central hub
                    bool isCenter = x >= 6 && x <= 9 && y >= 22 && y <= 25;
                    
                    // Draw connection arms based on bitmask (Up=1, Right=2, Down=4, Left=8)
                    bool isUp = (mask & 1) != 0 && x >= 6 && x <= 9 && y >= 16 && y < 22;
                    bool isRight = (mask & 2) != 0 && x > 9 && x <= 15 && y >= 22 && y <= 25;
                    bool isDown = (mask & 4) != 0 && x >= 6 && x <= 9 && y > 25 && y <= 31;
                    bool isLeft = (mask & 8) != 0 && x >= 0 && x < 6 && y >= 22 && y <= 25;

                    if (isCenter || isUp || isRight || isDown || isLeft)
                    {
                        pixels[(startY + y) * sheetW + (startX + x)] = tint;
                    }
                }
            }
        }

        Texture2D result = new(Game1.graphics.GraphicsDevice, sheetW, sheetH);
        result.SetData(pixels);

        string filePath = Path.Combine(outputPath, $"{name}.png");
        using (FileStream fs = File.Create(filePath))
        {
            result.SaveAsPng(fs, sheetW, sheetH);
        }

        monitor.Log($"[PlaceholderExtractor] Saved {name}.png (64x128 autotiling sheet)", LogLevel.Debug);
    }
}
