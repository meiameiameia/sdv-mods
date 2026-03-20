using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace Darth.PerfectionAdvisor.UI;

internal sealed class PerfectionAdvisorMenu : IClickableMenu
{
    private sealed class TabPage
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();
        public Rectangle Bounds { get; set; }
    }

    private readonly PerfectionAdvisorSnapshot snapshot;
    private readonly List<TabPage> pages;
    private readonly int[] tabScrollOffsets;
    private readonly Rectangle contentArea;
    private int selectedTabIndex;

    public PerfectionAdvisorMenu(PerfectionAdvisorSnapshot snapshot)
        : base(
            Game1.uiViewport.Width / 2 - 480,
            Game1.uiViewport.Height / 2 - 360,
            960,
            720,
            showUpperRightCloseButton: true)
    {
        this.snapshot = snapshot;
        this.contentArea = new Rectangle(this.xPositionOnScreen + 32, this.yPositionOnScreen + 148, this.width - 64, this.height - 198);

        this.pages = new List<TabPage>
        {
            new() { Name = "Overview", Lines = snapshot.OverviewLines },
            new() { Name = "Progress", Lines = snapshot.ProgressLines },
            new() { Name = "Blockers", Lines = snapshot.BlockerLines },
            new() { Name = "Details", Lines = snapshot.DetailLines }
        };
        this.tabScrollOffsets = new int[this.pages.Count];

        this.LayoutTabs();
    }

    private int ContentLineHeight => Game1.smallFont.LineSpacing + 4;

    private void LayoutTabs()
    {
        int tabTop = this.yPositionOnScreen + 98;
        int left = this.xPositionOnScreen + 32;
        int gap = 8;
        int tabWidth = (this.width - 64 - (gap * (this.pages.Count - 1))) / this.pages.Count;
        int tabHeight = 40;

        for (int i = 0; i < this.pages.Count; i++)
        {
            int x = left + i * (tabWidth + gap);
            this.pages[i].Bounds = new Rectangle(x, tabTop, tabWidth, tabHeight);
        }
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        this.xPositionOnScreen = newBounds.Width / 2 - this.width / 2;
        this.yPositionOnScreen = newBounds.Height / 2 - this.height / 2;
        this.upperRightCloseButton = new ClickableTextureComponent(
            new Rectangle(this.xPositionOnScreen + this.width - 36, this.yPositionOnScreen + 8, 28, 28),
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            2f);

        this.LayoutTabs();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        for (int i = 0; i < this.pages.Count; i++)
        {
            if (this.pages[i].Bounds.Contains(x, y))
            {
                this.selectedTabIndex = i;
                return;
            }
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int selected = this.selectedTabIndex;
        int maxScroll = Math.Max(0, this.GetWrappedLinesForSelectedTab().Count - this.GetVisibleLineCount());
        int current = this.tabScrollOffsets[selected];

        if (direction > 0)
            current = Math.Max(0, current - 1);
        else if (direction < 0)
            current = Math.Min(maxScroll, current + 1);

        this.tabScrollOffsets[selected] = current;
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape || key == Keys.E)
        {
            this.exitThisMenu();
            return;
        }

        if (key == Keys.Left)
        {
            this.selectedTabIndex = Math.Max(0, this.selectedTabIndex - 1);
            return;
        }

        if (key == Keys.Right)
        {
            this.selectedTabIndex = Math.Min(this.pages.Count - 1, this.selectedTabIndex + 1);
            return;
        }

        if (key == Keys.Up)
        {
            this.tabScrollOffsets[this.selectedTabIndex] = Math.Max(0, this.tabScrollOffsets[this.selectedTabIndex] - 1);
            return;
        }

        if (key == Keys.Down)
        {
            int maxScroll = Math.Max(0, this.GetWrappedLinesForSelectedTab().Count - this.GetVisibleLineCount());
            this.tabScrollOffsets[this.selectedTabIndex] = Math.Min(maxScroll, this.tabScrollOffsets[this.selectedTabIndex] + 1);
            return;
        }

        base.receiveKeyPress(key);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
        drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);

        string title = "Perfection Advisor";
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        b.DrawString(Game1.dialogueFont, title, new Vector2(this.xPositionOnScreen + (this.width - titleSize.X) / 2f, this.yPositionOnScreen + 22), Color.Black);
        b.DrawString(Game1.smallFont, $"Mode: {this.snapshot.ModeLabel}", new Vector2(this.xPositionOnScreen + 34, this.yPositionOnScreen + 68), new Color(60, 60, 90));

        for (int i = 0; i < this.pages.Count; i++)
        {
            TabPage page = this.pages[i];
            Color tabColor = i == this.selectedTabIndex ? new Color(255, 248, 220) : new Color(235, 235, 235);
            drawTextureBox(b, page.Bounds.X, page.Bounds.Y, page.Bounds.Width, page.Bounds.Height, tabColor);

            Vector2 textSize = Game1.smallFont.MeasureString(page.Name);
            Vector2 textPos = new(
                page.Bounds.X + (page.Bounds.Width - textSize.X) / 2f,
                page.Bounds.Y + (page.Bounds.Height - textSize.Y) / 2f);
            b.DrawString(Game1.smallFont, page.Name, textPos, Color.Black);
        }

        drawTextureBox(b, this.contentArea.X, this.contentArea.Y, this.contentArea.Width, this.contentArea.Height, Color.White);
        this.DrawSelectedTabContent(b);

        b.DrawString(
            Game1.smallFont,
            "Left/Right: Tab   Up/Down/Scroll: Move   Esc: Close",
            new Vector2(this.xPositionOnScreen + 34, this.yPositionOnScreen + this.height - 38),
            Color.Gray);

        base.draw(b);
        this.drawMouse(b);
    }

    private void DrawSelectedTabContent(SpriteBatch b)
    {
        List<string> wrappedLines = this.GetWrappedLinesForSelectedTab();
        int visibleLines = this.GetVisibleLineCount();
        int maxScroll = Math.Max(0, wrappedLines.Count - visibleLines);
        int scroll = Math.Clamp(this.tabScrollOffsets[this.selectedTabIndex], 0, maxScroll);
        this.tabScrollOffsets[this.selectedTabIndex] = scroll;

        int y = this.contentArea.Y + 14;
        int end = Math.Min(wrappedLines.Count, scroll + visibleLines);
        for (int i = scroll; i < end; i++)
        {
            string line = wrappedLines[i];
            Color color = line.StartsWith("-") ? new Color(45, 70, 105) : Color.Black;
            b.DrawString(Game1.smallFont, line, new Vector2(this.contentArea.X + 14, y), color);
            y += this.ContentLineHeight;
        }

        if (scroll > 0)
            b.DrawString(Game1.smallFont, "^", new Vector2(this.contentArea.Right - 22, this.contentArea.Y + 8), Color.Gray);
        if (end < wrappedLines.Count)
            b.DrawString(Game1.smallFont, "v", new Vector2(this.contentArea.Right - 22, this.contentArea.Bottom - 24), Color.Gray);
    }

    private int GetVisibleLineCount()
    {
        return Math.Max(1, (this.contentArea.Height - 24) / this.ContentLineHeight);
    }

    private List<string> GetWrappedLinesForSelectedTab()
    {
        IReadOnlyList<string> lines = this.pages[this.selectedTabIndex].Lines;
        List<string> wrapped = new();
        int wrapWidth = this.contentArea.Width - 28;

        if (lines.Count == 0)
        {
            wrapped.Add("No data available.");
            return wrapped;
        }

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                wrapped.Add(string.Empty);
                continue;
            }

            string parsed = Game1.parseText(line, Game1.smallFont, wrapWidth);
            string[] split = parsed.Split(Environment.NewLine);
            foreach (string item in split)
                wrapped.Add(item);
        }

        return wrapped;
    }
}
