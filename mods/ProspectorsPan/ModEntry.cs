using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace Meiameiameia.ProspectorsPan;

internal sealed class ModEntry : Mod
{
    private static readonly MethodInfo? UpdateOrePanAnimationMethod = typeof(GameLocation).GetMethod("updateOrePanAnimation", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static ModEntry Instance { get; private set; } = null!;

    private readonly Dictionary<string, TrackedPanSpot> trackedSpots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> lastAssistedSpotTimes = new(StringComparer.Ordinal);
    private Harmony? harmony;
    private Rectangle lastCornerHintBounds = Rectangle.Empty;
    private Point cornerHintDragOffset;
    private bool panUsedThisTick;
    private bool draggingCornerHint;

    internal ModConfig Config { get; private set; } = new();
    private PanRewardManager RewardManager { get; set; } = null!;

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        I18n.Init(helper.Translation);
        this.Config = helper.ReadConfig<ModConfig>();
        this.NormalizeConfig();
        this.RewardManager = new PanRewardManager(this.Config);

        this.harmony = new Harmony(this.ModManifest.UniqueID);
        this.harmony.PatchAll();

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        helper.Events.Display.RenderedHud += this.OnRenderedHud;

        helper.ConsoleCommands.Add("prospectorspan_hint_reset", I18n.Get("command.hint-reset"), this.ResetHintPositionCommand);

        this.Monitor.Log(helper.Translation.Get("message.loaded"), LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GmcmIntegration.Register(this.Helper, this.ModManifest, () => this.Config, config =>
        {
            this.Config = config;
            this.NormalizeConfig();
            this.RewardManager = new PanRewardManager(this.Config);
        });
    }

    internal void AddBonusPanRewards(GameLocation location, Farmer player, IList<Item> rewards)
    {
        if (!this.Config.EnableBonusRewards)
        {
            return;
        }

        this.RewardManager.AddBonusRewards(location, player, rewards);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.trackedSpots.Clear();
        this.lastAssistedSpotTimes.Clear();
        this.panUsedThisTick = false;
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !this.Config.EnableSpotTuning)
        {
            return;
        }

        GameLocation? location = Game1.currentLocation;
        if (location is null)
        {
            return;
        }

        this.TrackOrRestoreSpot(location, e.NewTime);
        this.TryExtraSpawnAttempts(location);
        this.TryPlaceReachableSpot(location, e.NewTime);
        this.TrackOrRestoreSpot(location, e.NewTime);
        this.panUsedThisTick = false;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
        {
            return;
        }

        if (Game1.player.CurrentTool is not Pan)
        {
            return;
        }

        Point panPoint = Game1.currentLocation.orePanPoint.Value;
        if (panPoint != Point.Zero && Utility.distance(panPoint.X, Game1.player.TilePoint.X, panPoint.Y, Game1.player.TilePoint.Y) <= 3f)
        {
            this.panUsedThisTick = true;
        }
    }

    private void ResetHintPositionCommand(string command, string[] args)
    {
        this.Config.PanHintCornerX = 24;
        this.Config.PanHintCornerYFromBottom = 176;
        this.Helper.WriteConfig(this.Config);
        this.Monitor.Log(I18n.Get("command.hint-reset.done"), LogLevel.Info);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
        {
            return;
        }

        bool wantsDrag = this.Helper.Input.IsDown(SButton.MouseLeft)
            && (this.Helper.Input.IsDown(SButton.LeftShift) || this.Helper.Input.IsDown(SButton.RightShift));

        if (!wantsDrag)
        {
            if (this.draggingCornerHint)
            {
                this.draggingCornerHint = false;
                this.Helper.WriteConfig(this.Config);
            }

            return;
        }

        Vector2 mouse = this.Helper.Input.GetCursorPosition().ScreenPixels;
        if (!this.draggingCornerHint && !this.TryStartCornerHintDrag(mouse))
        {
            return;
        }

        int x = (int)mouse.X - this.cornerHintDragOffset.X;
        int y = (int)mouse.Y - this.cornerHintDragOffset.Y;
        this.SetCornerHintPosition(x, y);
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady
            || !this.Config.EnablePanHint
            || this.Config.PanHintMode == PanHintMode.Off
            || Game1.player.CurrentTool is not Pan
            || Game1.currentLocation is null)
        {
            return;
        }

        Point panPoint = Game1.currentLocation.orePanPoint.Value;
        if (panPoint == Point.Zero)
        {
            return;
        }

        float distance = Utility.distance(panPoint.X, Game1.player.TilePoint.X, panPoint.Y, Game1.player.TilePoint.Y);
        if (distance > Math.Max(12, this.Config.ReachableSpotSearchRadius + 4))
        {
            return;
        }

        if (this.Config.PanHintPosition == PanHintPosition.World)
        {
            this.DrawPanHint(e.SpriteBatch, panPoint, distance);
        }
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady
            || !this.Config.EnablePanHint
            || this.Config.PanHintMode == PanHintMode.Off
            || this.Config.PanHintPosition != PanHintPosition.Corner
            || Game1.player.CurrentTool is not Pan
            || Game1.currentLocation is null)
        {
            this.lastCornerHintBounds = Rectangle.Empty;
            return;
        }

        Point panPoint = Game1.currentLocation.orePanPoint.Value;
        if (panPoint == Point.Zero)
        {
            this.lastCornerHintBounds = Rectangle.Empty;
            return;
        }

        float distance = Utility.distance(panPoint.X, Game1.player.TilePoint.X, panPoint.Y, Game1.player.TilePoint.Y);
        if (distance > Math.Max(12, this.Config.ReachableSpotSearchRadius + 4))
        {
            this.lastCornerHintBounds = Rectangle.Empty;
            return;
        }

        Vector2 direction = new(panPoint.X - Game1.player.TilePoint.X, panPoint.Y - Game1.player.TilePoint.Y);
        string arrow = this.GetArrow(direction);
        string text = this.Config.PanHintMode == PanHintMode.DirectionOnly
            ? I18n.Get("hint.direction").Replace("{{arrow}}", arrow, StringComparison.Ordinal)
            : I18n.Get("hint.direction-distance")
                .Replace("{{arrow}}", arrow, StringComparison.Ordinal)
                .Replace("{{tiles}}", Math.Max(1, (int)MathF.Round(distance)).ToString(), StringComparison.Ordinal);

        this.DrawCornerHint(e.SpriteBatch, text);
    }

    private void DrawPanHint(SpriteBatch spriteBatch, Point panPoint, float distance)
    {
        Vector2 spotScreenPosition = Game1.GlobalToLocal(Game1.viewport, new Vector2(panPoint.X * Game1.tileSize + Game1.tileSize / 2f, panPoint.Y * Game1.tileSize + Game1.tileSize / 2f));
        Vector2 playerScreenPosition = Game1.GlobalToLocal(Game1.viewport, Game1.player.Position);
        Vector2 direction = spotScreenPosition - playerScreenPosition;

        if (direction.LengthSquared() > 0.01f)
        {
            direction.Normalize();
        }

        string arrow = this.GetArrow(direction);
        string text = this.Config.PanHintMode == PanHintMode.DirectionOnly
            ? I18n.Get("hint.direction").Replace("{{arrow}}", arrow, StringComparison.Ordinal)
            : I18n.Get("hint.direction-distance")
                .Replace("{{arrow}}", arrow, StringComparison.Ordinal)
                .Replace("{{tiles}}", Math.Max(1, (int)MathF.Round(distance)).ToString(), StringComparison.Ordinal);

        if (this.Config.PanHintPosition == PanHintPosition.World)
        {
            this.DrawWorldHint(spriteBatch, spotScreenPosition, arrow);
            return;
        }

        this.DrawCornerHint(spriteBatch, text);
    }

    private void DrawCornerHint(SpriteBatch spriteBatch, string text)
    {
        Vector2 textPosition = new(this.Config.PanHintCornerX, Game1.uiViewport.Height - this.Config.PanHintCornerYFromBottom);
        Vector2 size = Game1.smallFont.MeasureString(text);
        Rectangle background = new((int)textPosition.X - 8, (int)textPosition.Y - 5, (int)size.X + 16, (int)size.Y + 10);
        this.lastCornerHintBounds = background;

        spriteBatch.Draw(Game1.staminaRect, background, Color.Black * 0.55f);
        Utility.drawTextWithShadow(spriteBatch, text, Game1.smallFont, textPosition, Color.White);
    }

    private void DrawWorldHint(SpriteBatch spriteBatch, Vector2 spotScreenPosition, string arrow)
    {
        Vector2 textPosition = new((int)spotScreenPosition.X - 12, (int)spotScreenPosition.Y - 58);
        Vector2 size = Game1.smallFont.MeasureString(arrow);
        Rectangle background = new((int)textPosition.X - 6, (int)textPosition.Y - 4, (int)size.X + 12, (int)size.Y + 8);

        spriteBatch.Draw(Game1.staminaRect, background, Color.Black * 0.45f);
        Utility.drawTextWithShadow(spriteBatch, arrow, Game1.smallFont, textPosition, Color.White);
    }

    private bool TryStartCornerHintDrag(Vector2 mousePosition)
    {
        if (!this.Config.EnablePanHint
            || this.Config.PanHintPosition != PanHintPosition.Corner
            || this.lastCornerHintBounds == Rectangle.Empty)
        {
            return false;
        }

        Rectangle dragBounds = this.lastCornerHintBounds;
        dragBounds.Inflate(18, 18);

        Point mouse = new((int)mousePosition.X, (int)mousePosition.Y);
        if (!dragBounds.Contains(mouse))
        {
            return false;
        }

        this.draggingCornerHint = true;
        this.cornerHintDragOffset = new Point(mouse.X - this.lastCornerHintBounds.X, mouse.Y - this.lastCornerHintBounds.Y);
        return true;
    }

    private void SetCornerHintPosition(int x, int y)
    {
        const int margin = 8;
        int maxX = Math.Max(margin, Game1.uiViewport.Width - this.lastCornerHintBounds.Width - margin);
        int maxY = Math.Max(margin, Game1.uiViewport.Height - this.lastCornerHintBounds.Height - margin);

        int clampedX = Math.Clamp(x, margin, maxX);
        int clampedY = Math.Clamp(y, margin, maxY);

        this.Config.PanHintCornerX = clampedX + 8;
        this.Config.PanHintCornerYFromBottom = Game1.uiViewport.Height - (clampedY + 5);
    }

    private string GetArrow(Vector2 direction)
    {
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
        {
            return direction.X >= 0 ? ">" : "<";
        }

        return direction.Y >= 0 ? "v" : "^";
    }

    private void TryExtraSpawnAttempts(GameLocation location)
    {
        if (location.orePanPoint.Value != Point.Zero)
        {
            return;
        }

        float extraAttempts = Math.Max(0f, this.Config.SpotSpawnMultiplier - 1f);
        int guaranteedAttempts = (int)Math.Floor(extraAttempts);
        float fractionalAttempt = extraAttempts - guaranteedAttempts;

        for (int i = 0; i < guaranteedAttempts; i++)
        {
            if (location.orePanPoint.Value != Point.Zero)
            {
                return;
            }

            location.performOrePanTenMinuteUpdate(Game1.random);
        }

        if (location.orePanPoint.Value == Point.Zero && fractionalAttempt > 0f && Game1.random.NextDouble() < fractionalAttempt)
        {
            location.performOrePanTenMinuteUpdate(Game1.random);
        }
    }

    private void TryPlaceReachableSpot(GameLocation location, int timeOfDay)
    {
        if (!this.Config.EnableReachableSpotAssist || location.orePanPoint.Value != Point.Zero || Game1.player.CurrentTool is not Pan)
        {
            return;
        }

        string locationKey = location.NameOrUniqueName;
        if (this.lastAssistedSpotTimes.TryGetValue(locationKey, out int lastAssistedSpotTime)
            && CountTenMinuteTicks(lastAssistedSpotTime, timeOfDay) * 10 < this.Config.AssistedSpotCooldownMinutes)
        {
            return;
        }

        if (Game1.random.NextDouble() >= this.Config.ReachableSpotChance)
        {
            return;
        }

        if (!this.TryFindReachableWaterTile(location, Game1.player.TilePoint, out Point spotTile))
        {
            return;
        }

        location.orePanPoint.Value = spotTile;
        UpdateOrePanAnimationMethod?.Invoke(location, Array.Empty<object>());
        this.trackedSpots[locationKey] = new TrackedPanSpot(spotTile, timeOfDay);
        this.lastAssistedSpotTimes[locationKey] = timeOfDay;
    }

    private bool TryFindReachableWaterTile(GameLocation location, Point playerTile, out Point spotTile)
    {
        List<Point> candidates = new();
        int radius = this.Config.ReachableSpotSearchRadius;

        for (int x = playerTile.X - radius; x <= playerTile.X + radius; x++)
        {
            for (int y = playerTile.Y - radius; y <= playerTile.Y + radius; y++)
            {
                Point tile = new(x, y);
                if (this.IsReachablePanWaterTile(location, playerTile, tile))
                {
                    candidates.Add(tile);
                }
            }
        }

        if (candidates.Count == 0)
        {
            spotTile = Point.Zero;
            return false;
        }

        spotTile = candidates[Game1.random.Next(candidates.Count)];
        return true;
    }

    private bool IsReachablePanWaterTile(GameLocation location, Point playerTile, Point waterTile)
    {
        if (!location.isTileOnMap(waterTile)
            || !location.isWaterTile(waterTile.X, waterTile.Y)
            || location.doesTileHaveProperty(waterTile.X, waterTile.Y, "Passable", "Buildings") is not null
            || location.doesTileHaveProperty(waterTile.X, waterTile.Y, "Water", "Back") is null)
        {
            return false;
        }

        float distanceFromPlayer = Utility.distance(waterTile.X, playerTile.X, waterTile.Y, playerTile.Y);
        if (distanceFromPlayer < 1f || distanceFromPlayer > this.Config.ReachableSpotSearchRadius)
        {
            return false;
        }

        foreach (Vector2 adjacentTile in Utility.getAdjacentTileLocations(new Vector2(waterTile.X, waterTile.Y)))
        {
            if (Utility.distance((int)adjacentTile.X, playerTile.X, (int)adjacentTile.Y, playerTile.Y) <= 3f
                && location.isTileOnMap(adjacentTile)
                && location.isTilePassable(adjacentTile))
            {
                return true;
            }
        }

        return distanceFromPlayer <= 3f;
    }

    private void TrackOrRestoreSpot(GameLocation location, int timeOfDay)
    {
        string key = location.NameOrUniqueName;
        Point currentPoint = location.orePanPoint.Value;

        if (currentPoint != Point.Zero)
        {
            if (!this.trackedSpots.TryGetValue(key, out TrackedPanSpot currentTrackedSpot) || currentTrackedSpot.Tile != currentPoint)
            {
                this.trackedSpots[key] = new TrackedPanSpot(currentPoint, timeOfDay);
            }

            return;
        }

        if (this.panUsedThisTick || !this.trackedSpots.TryGetValue(key, out TrackedPanSpot trackedSpot))
        {
            this.trackedSpots.Remove(key);
            return;
        }

        int protectedTicks = Math.Max(0, (int)Math.Round(this.Config.SpotLifetimeMultiplier) - 1);
        if (protectedTicks <= 0 || CountTenMinuteTicks(trackedSpot.FirstSeenTime, timeOfDay) > protectedTicks)
        {
            this.trackedSpots.Remove(key);
            return;
        }

        location.orePanPoint.Value = trackedSpot.Tile;
    }

    private void NormalizeConfig()
    {
        this.Config.SpotSpawnMultiplier = Math.Clamp(this.Config.SpotSpawnMultiplier, 0.25f, 4f);
        this.Config.SpotLifetimeMultiplier = Math.Clamp(this.Config.SpotLifetimeMultiplier, 0.5f, 5f);
        this.Config.ReachableSpotChance = Math.Clamp(this.Config.ReachableSpotChance, 0f, 1f);
        this.Config.ReachableSpotSearchRadius = Math.Clamp(this.Config.ReachableSpotSearchRadius, 3, 16);
        this.Config.AssistedSpotCooldownMinutes = Math.Clamp(this.Config.AssistedSpotCooldownMinutes, 0, 120);
        this.Config.PanHintCornerX = Math.Clamp(this.Config.PanHintCornerX, 8, 4000);
        this.Config.PanHintCornerYFromBottom = Math.Clamp(this.Config.PanHintCornerYFromBottom, 8, 4000);
        this.Helper.WriteConfig(this.Config);
    }

    private static int CountTenMinuteTicks(int fromTime, int toTime)
    {
        return Math.Max(0, ToMinutes(toTime) - ToMinutes(fromTime)) / 10;
    }

    private static int ToMinutes(int time)
    {
        return (time / 100 * 60) + (time % 100);
    }

    private readonly record struct TrackedPanSpot(Point Tile, int FirstSeenTime);
}
