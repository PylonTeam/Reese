using Reese.Common.Replayer.ReplayEvents;
using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using System;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplayControls;

internal sealed class ReplayEventMarkerLayer : UIElement
{
    private const int BossIconSize = 30;
    private const int EventIconSize = 22;
    private const int MarkerWidth = 5;
    private static Player snapshotHeadPlayer;

    public ReplayEventMarkerLayer()
    {
        IgnoresMouseInteraction = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (ReplayPlayback.TimelineEvents.Count == 0)
            return;

        uint durationTicks = Math.Max(1u, ReplayPlayback.DurationTicks);
        Rectangle area = GetDimensions().ToRectangle();
        Rectangle track = GetTrackRectangle(area);

        if (track.Width <= 0)
            return;

        Point mouse = Main.MouseScreen.ToPoint();
        ReplayTimelineEvent hoveredEvent = default;
        bool hasHover = false;

        foreach (ReplayTimelineEvent timelineEvent in ReplayPlayback.TimelineEvents)
        {
            if (!ShouldDraw(timelineEvent))
                continue;

            float ratio = MathHelper.Clamp(timelineEvent.Tick / (float)durationTicks, 0f, 1f);
            int x = (int)Math.Round(track.Left + ratio * track.Width);
            Color color = GetColor(timelineEvent.Category);
            Rectangle marker = new(x - MarkerWidth / 2, track.Y - 4, MarkerWidth, track.Height + 8);

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, marker, color * 0.85f);

            Rectangle iconArea = GetIconArea(timelineEvent, x, track.Y);
            DrawIcon(spriteBatch, timelineEvent, iconArea, color);

            Rectangle hoverArea = Rectangle.Union(marker, iconArea);
            hoverArea.Inflate(5, 5);

            if (hoverArea.Contains(mouse))
            {
                hoveredEvent = timelineEvent;
                hasHover = true;
            }
        }

        if (!hasHover)
            return;

        Main.LocalPlayer.mouseInterface = true;
        Main.instance.MouseText(hoveredEvent.Text);
    }

    private static bool ShouldDraw(ReplayTimelineEvent timelineEvent)
    {
        return timelineEvent.Category switch
        {
            ReplayEventCategory.BossDefeated => ReplayClientSettings.ShowBossesDefeated,
            ReplayEventCategory.PlayerDeath => ReplayClientSettings.ShowPlayerDeaths,
            ReplayEventCategory.PlayerKill => ReplayClientSettings.ShowPlayerDeaths,
            ReplayEventCategory.InvasionStarted => ReplayClientSettings.ShowInvasions,
            _ => true
        };
    }

    private static Rectangle GetTrackRectangle(Rectangle area)
    {
        int sliderHeight = Math.Max(1, (int)Math.Round(PlaybackLayout.SliderHeight));
        Rectangle slider = new(area.X, area.Bottom - sliderHeight, area.Width, sliderHeight);
        int innerPaddingX = 4;
        int innerPaddingY = Math.Max(2, (slider.Height - 12) / 2);

        slider.Inflate(-innerPaddingX, -innerPaddingY);
        return slider;
    }

    private static Rectangle GetIconArea(ReplayTimelineEvent timelineEvent, int x, int trackTop)
    {
        int size = timelineEvent.Category == ReplayEventCategory.BossDefeated ? BossIconSize : EventIconSize;
        int top = trackTop - size - 4;

        return new Rectangle(x - size / 2, top, size, size);
    }

    private static Color GetColor(ReplayEventCategory category)
    {
        return category switch
        {
            ReplayEventCategory.BossDefeated => new Color(255, 215, 84),
            ReplayEventCategory.PlayerDeath => new Color(255, 88, 88),
            ReplayEventCategory.InvasionStarted => new Color(120, 220, 255),
            ReplayEventCategory.PlayerJoined => new Color(88, 220, 126),
            ReplayEventCategory.PlayerLeft => new Color(155, 155, 165),
            ReplayEventCategory.PlayerKill => new Color(255, 132, 72),
            _ => Main.OurFavoriteColor
        };
    }

    private static void DrawIcon(SpriteBatch spriteBatch, ReplayTimelineEvent timelineEvent, Rectangle area, Color color)
    {
        switch (timelineEvent.IconKind)
        {
            case ReplayEventIconKind.BossHead:
                DrawBossHead(spriteBatch, timelineEvent.IconId, area);
                return;

            case ReplayEventIconKind.MapDeath:
                DrawTexture(spriteBatch, TextureAssets.MapDeath.Value, area, Color.White);
                return;

            case ReplayEventIconKind.Item:
                DrawItem(spriteBatch, timelineEvent.IconId, area);
                return;

            case ReplayEventIconKind.PlayerHead:
                DrawPlayerHead(spriteBatch, timelineEvent, area);
                return;
        }

        DrawDiamond(spriteBatch, area, color);
    }

    private static void DrawBossHead(SpriteBatch spriteBatch, int headNpcId, Rectangle area)
    {
        if (headNpcId < 0 ||
            headNpcId >= NPCID.Sets.BossHeadTextures.Length ||
            NPCID.Sets.BossHeadTextures[headNpcId] == -1)
        {
            DrawDiamond(spriteBatch, area, new Color(255, 215, 84));
            return;
        }

        Main.BossNPCHeadRenderer.DrawWithOutlines(null, NPCID.Sets.BossHeadTextures[headNpcId], area.Center.ToVector2(), Color.White, 0f, 0.62f, SpriteEffects.None);
    }

    private static void DrawItem(SpriteBatch spriteBatch, int itemId, Rectangle area)
    {
        if (itemId <= 0 || itemId >= TextureAssets.Item.Length)
        {
            DrawDiamond(spriteBatch, area, new Color(120, 220, 255));
            return;
        }

        Main.instance.LoadItem(itemId);
        DrawTexture(spriteBatch, TextureAssets.Item[itemId].Value, area, Color.White);
    }

    private static void DrawPlayerHead(SpriteBatch spriteBatch, ReplayTimelineEvent timelineEvent, Rectangle area)
    {
        Player player = GetPlayerHeadDrawPlayer(timelineEvent);
        if (player == null)
        {
            DrawTexture(spriteBatch, Ass.IconPlayerHead.Value, area, timelineEvent.Category == ReplayEventCategory.PlayerLeft ? Color.Gray : Color.White);
            return;
        }

        float scale = Math.Min(area.Width, area.Height) / 42f;
        Vector2 position = area.Center.ToVector2();
        bool grayscale = timelineEvent.Category == ReplayEventCategory.PlayerLeft;

        if (grayscale)
            DrawPlayerHeadGrayscale(spriteBatch, player, position, scale);
        else
            DrawPlayerHeadDirect(player, position, scale);

        if (timelineEvent.Category == ReplayEventCategory.PlayerKill)
            DrawKillWeaponOverlay(spriteBatch, timelineEvent.IconId, area);
    }

    private static Player GetPlayerHeadDrawPlayer(ReplayTimelineEvent timelineEvent)
    {
        if (timelineEvent.PlayerHead.HasValue)
        {
            snapshotHeadPlayer ??= new Player();
            timelineEvent.PlayerHead.Value.ApplyTo(snapshotHeadPlayer);
            return snapshotHeadPlayer;
        }

        int playerIndex = timelineEvent.IconId;
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return null;

        Player player = Main.player[playerIndex];
        return player != null && !string.IsNullOrWhiteSpace(player.name) ? player : null;
    }

    private static void DrawPlayerHeadGrayscale(SpriteBatch spriteBatch, Player player, Vector2 position, float scale)
    {
        if (!EffectLoader.TryGetGrayscaleEffect(out Effect effect))
        {
            DrawPlayerHeadDirect(player, position, scale);
            return;
        }

        GraphicsDevice device = spriteBatch.GraphicsDevice;
        Rectangle scissor = device.ScissorRectangle;
        RasterizerState oldRasterizer = device.RasterizerState;

        effect.Parameters["Intensity"]?.SetValue(1f);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, oldRasterizer, effect, Main.UIScaleMatrix);
        device.ScissorRectangle = scissor;
        DrawPlayerHeadDirect(player, position, scale);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, oldRasterizer, null, Main.UIScaleMatrix);
        device.ScissorRectangle = scissor;
    }

    private static void DrawPlayerHeadDirect(Player player, Vector2 position, float scale)
    {
        Color borderColor = player.team > 0 && player.team < Main.teamColor.Length ? Main.teamColor[player.team] : Color.Black;
        FullBrightPlayerDrawer.ForceFullBrightOnce = true;

        try
        {
            Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, position, scale: scale, borderColor: borderColor);
        }
        finally
        {
            FullBrightPlayerDrawer.ForceFullBrightOnce = false;
        }
    }

    private static void DrawKillWeaponOverlay(SpriteBatch spriteBatch, int itemId, Rectangle headArea)
    {
        int overlaySize = Math.Max(12, (int)Math.Round(Math.Min(headArea.Width, headArea.Height) * 0.62f));
        Rectangle overlayArea = new(
            headArea.Right - overlaySize + 3,
            headArea.Y - 3,
            overlaySize,
            overlaySize);

        DrawItem(spriteBatch, itemId, overlayArea);
    }

    private static void DrawTexture(SpriteBatch spriteBatch, Texture2D texture, Rectangle area, Color color)
    {
        if (texture == null)
            return;

        Rectangle source = texture.Bounds;
        float scale = Math.Min(area.Width / (float)source.Width, area.Height / (float)source.Height);
        Vector2 position = area.Center.ToVector2();
        Vector2 origin = source.Size() * 0.5f;

        spriteBatch.Draw(texture, position, source, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private static void DrawDiamond(SpriteBatch spriteBatch, Rectangle area, Color color)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        int centerX = area.Center.X;
        int centerY = area.Center.Y;
        int radius = Math.Max(4, Math.Min(area.Width, area.Height) / 3);

        for (int y = -radius; y <= radius; y++)
        {
            int halfWidth = radius - Math.Abs(y);
            spriteBatch.Draw(pixel, new Rectangle(centerX - halfWidth, centerY + y, halfWidth * 2 + 1, 1), color);
        }
    }
}
