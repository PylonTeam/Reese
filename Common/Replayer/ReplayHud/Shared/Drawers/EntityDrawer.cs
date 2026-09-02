using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.GhostHooks;
using Reese.Core.Configs;
using ReLogic.Graphics;
using System;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.ID;

namespace Reese.Common.Replayer.ReplayHud.Shared.Drawers;

public static class EntityDrawer
{
    private static readonly Rectangle DefaultPlayerFrame = new(0, 0, 40, 56);

    private static readonly RasterizerState ClippedCullNone = new()
    {
        CullMode = CullMode.None,
        ScissorTestEnable = true
    };

    private static readonly RasterizerState ClippedCullCounterClockwise = new()
    {
        CullMode = CullMode.CullCounterClockwiseFace,
        ScissorTestEnable = true
    };

    #region Entity Background Texture
    public static Texture2D EntityBackground => Main.Assets.Request<Texture2D>("Images/UI/PlayerBackground").Value;

    public static void DrawEntityBackground(SpriteBatch sb, Rectangle area)
    {
        DrawPlayerBackgroundSlice(sb, EntityBackground, area, Color.White*1f);
    }

    private static void DrawPlayerBackgroundSlice(SpriteBatch sb, Texture2D texture, Rectangle area, Color color)
    {
        const int left = 6;
        const int right = 6;
        const int top = 6;
        const int bottom = 6;

        if (area.Width <= 0 || area.Height <= 0)
            return;

        int middleSourceWidth = texture.Width - left - right;
        int middleSourceHeight = texture.Height - top - bottom;
        int middleDestinationWidth = Math.Max(0, area.Width - left - right);
        int middleDestinationHeight = Math.Max(0, area.Height - top - bottom);

        DrawSlice(sb, texture, new Rectangle(0, 0, left, top), new Rectangle(area.X, area.Y, left, top), color);
        DrawSlice(sb, texture, new Rectangle(left, 0, middleSourceWidth, top), new Rectangle(area.X + left, area.Y, middleDestinationWidth, top), color);
        DrawSlice(sb, texture, new Rectangle(texture.Width - right, 0, right, top), new Rectangle(area.Right - right, area.Y, right, top), color);

        DrawSlice(sb, texture, new Rectangle(0, top, left, middleSourceHeight), new Rectangle(area.X, area.Y + top, left, middleDestinationHeight), color);
        DrawSlice(sb, texture, new Rectangle(left, top, middleSourceWidth, middleSourceHeight), new Rectangle(area.X + left, area.Y + top, middleDestinationWidth, middleDestinationHeight), color);
        DrawSlice(sb, texture, new Rectangle(texture.Width - right, top, right, middleSourceHeight), new Rectangle(area.Right - right, area.Y + top, right, middleDestinationHeight), color);

        DrawSlice(sb, texture, new Rectangle(0, texture.Height - bottom, left, bottom), new Rectangle(area.X, area.Bottom - bottom, left, bottom), color);
        DrawSlice(sb, texture, new Rectangle(left, texture.Height - bottom, middleSourceWidth, bottom), new Rectangle(area.X + left, area.Bottom - bottom, middleDestinationWidth, bottom), color);
        DrawSlice(sb, texture, new Rectangle(texture.Width - right, texture.Height - bottom, right, bottom), new Rectangle(area.Right - right, area.Bottom - bottom, right, bottom), color);
    }

    private static void DrawSlice(SpriteBatch sb, Texture2D texture, Rectangle source, Rectangle destination, Color color)
    {
        if (source.Width <= 0 || source.Height <= 0 || destination.Width <= 0 || destination.Height <= 0)
            return;

        sb.Draw(texture, destination, source, color);
    }
    #endregion

    #region Player
    public static void DrawPlayerCardPreview(SpriteBatch sb, Player player, Rectangle area)
    {
        //PlayerOutlines.ForcePreviewOutline = true;

        try
        {
            DrawPlayerPreview(sb, player, area);
        }
        finally
        {
            //PlayerOutlines.ForcePreviewOutline = false;
        }

        //switch (SpectatorClientSettings.DrawPlayers)
        //{
        //    case SpectatorPlayerDrawMode.FullPlayer:
        //        DrawPlayerPreview(sb, player, area);
        //        break;
        //    case SpectatorPlayerDrawMode.PlayerHeads:
        //        DrawPlayerHead(sb, player, area.Center.ToVector2(), Math.Min(area.Width, area.Height) / 42f);
        //        break;
        //}
    }

    public static void DrawPlayerPreview(SpriteBatch sb, Player player, Rectangle area)
    {
        if (player?.active != true || area.Width <= 0 || area.Height <= 0)
            return;

        float scale = GetPlayerScale();

        Vector2 drawSize = new(player.width * scale, player.height * scale);
        Vector2 drawPos = new(
            (int)MathF.Round(area.Center.X - drawSize.X * 0.5f),
            (int)MathF.Round(area.Center.Y - drawSize.Y * 0.5f + player.gfxOffY * scale));

        drawPos.Y += GetPlayerScaleVerticalOffset();

        if (player.statLife <= 0)
            DrawRespawnTime(sb, player, area);

        DrawFullPlayer(sb, player, drawPos, scale);
    }

    private static void DrawRespawnTime(SpriteBatch sb, Player player, Rectangle area)
    {
        area.Y -= 6;
        DrawCenteredTexture(sb, Ass.IconDead.Value, area, 0.45f);
        area.Y += 8;

        int seconds = Math.Max(0, (int)Math.Ceiling(player.respawnTimer / 60f));
        string text = seconds.ToString();

        DynamicSpriteFont font = FontAssets.DeathText.Value;
        float textScale = 0.6f;

        Vector2 textSize = font.MeasureString(text) * textScale;
        Vector2 textPosition = new(
            area.X + (area.Width - textSize.X) * 0.5f,
            area.Y + (area.Height - textSize.Y) * 0.5f + 8f
        );

        Utils.DrawBorderStringBig(sb, text, textPosition, Color.White*0.5f, textScale, 0f, 0f);
    }

    private static void DrawCenteredTexture(SpriteBatch sb, Texture2D texture, Rectangle area, float scale)
    {
        if (texture == null)
            return;

        Vector2 origin = texture.Size() * 0.5f;
        Vector2 position = area.Center.ToVector2();

        sb.Draw(texture, position, null, Color.White*1f, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private static float GetPlayerScale()
    {
        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();
        return 0.8f;

        //float scale = clientConfig.replayHudSize switch
        //{
        //    ClientConfig.ReplayHudSize.Small => 1.0f,
        //    ClientConfig.ReplayHudSize.Medium => 1.25f,
        //    ClientConfig.ReplayHudSize.Large => 1.5f,
        //    _ => 1f
        //};

        //return scale;
    }

    private static float GetPlayerScaleVerticalOffset()
    {
        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();

        return -8f;
        //float scale = clientConfig.replayHudSize switch
        //{
        //    ClientConfig.ReplayHudSize.Small => 5f,
        //    ClientConfig.ReplayHudSize.Medium => 10f,
        //    ClientConfig.ReplayHudSize.Large => 20,
        //    _ => 1f
        //};

        //return scale;
    }

    public static void DrawFullPlayer(SpriteBatch sb, Player player, Vector2 position, float scale = 1f)
    {
        if (player?.active != true)
            return;

        // Resolved before any override is applied, because it reads the player's real ghost/dead state.
        bool drawAsGhost = (player.ghost || player.dead)
            && GhostDrawSystem.ShouldDrawGhost(player)
            && player.statLife > 0;

        Rectangle oldScissor = sb.GraphicsDevice.ScissorRectangle;
        RasterizerState oldRasterizer = sb.GraphicsDevice.RasterizerState;

        sb.End();
        sb.GraphicsDevice.ScissorRectangle = oldScissor;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, ClippedCullNone, null, Main.UIScaleMatrix);

        PlayerDrawState state = PlayerDrawState.Capture(player);
        FullBrightPlayerDrawer.ForceFullBrightOnce = true;

        try
        {
            if (drawAsGhost)
            {
                position.X += 2f;
                position.Y += 8f;
                DrawGhost(Main.Camera, player, position + Main.screenPosition, scale);
            }
            else if (player.statLife > 0)
            {
                player.ghost = false;
                player.dead = false;
                player.socialIgnoreLight = true;
                player.isDisplayDollOrInanimate = false;

                Main.PlayerRenderer.DrawPlayer(Main.Camera, player, position + Main.screenPosition, 0f, Vector2.Zero, 0f, scale);
            }
        }
        finally
        {
            FullBrightPlayerDrawer.ForceFullBrightOnce = false;
            state.Restore(player);

            sb.End();
            sb.GraphicsDevice.ScissorRectangle = oldScissor;
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, oldRasterizer, null, Main.UIScaleMatrix);
        }
    }

    public static void DrawPlayerHead(SpriteBatch sb, Player player, Vector2 position, float scale = 1f)
    {
        if (player?.active != true)
            return;

        PlayerDrawState state = PlayerDrawState.Capture(player);
        FullBrightPlayerDrawer.ForceFullBrightOnce = true;

        try
        {
            ApplyHeadDrawOverrides(player);
            Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, position, scale: scale, borderColor: Main.teamColor[player.team]);
        }
        finally
        {
            FullBrightPlayerDrawer.ForceFullBrightOnce = false;
            state.Restore(player);
        }
    }

    private static void ApplyHeadDrawOverrides(Player player)
    {
        player.dead = false;
        player.ghost = false;
        player.headRotation = 0f;
        player.socialIgnoreLight = true;
        player.isDisplayDollOrInanimate = false;
        player.statLife = Math.Max(1, player.statLife);
        player.statLifeMax = Math.Max(1, player.statLifeMax);

        if (player.direction == 0)
            player.direction = 1;

        if (player.gravDir == 0f)
            player.gravDir = 1f;

        if (player.bodyFrame.Width <= 0 || player.bodyFrame.Height <= 0)
            player.bodyFrame = DefaultPlayerFrame;

        if (player.legFrame.Width <= 0 || player.legFrame.Height <= 0)
            player.legFrame = DefaultPlayerFrame;

        if (player.headFrame.Width <= 0 || player.headFrame.Height <= 0)
            player.headFrame = DefaultPlayerFrame;
    }

    /// <summary>
    /// The fields <see cref="DrawFullPlayer"/> and <see cref="DrawPlayerHead"/> override, captured so the
    /// overrides can be applied straight to the live player and put back once the draw is done.
    /// <para/>
    /// This used to clone the player per draw with SerializedClone, which is a full save-file round trip
    /// (inventory, equips, every mod's ModPlayer data) and cost milliseconds per frame with a full lobby.
    /// </summary>
    private readonly struct PlayerDrawState
    {
        private readonly bool ghost;
        private readonly bool dead;
        private readonly bool socialIgnoreLight;
        private readonly bool isDisplayDollOrInanimate;
        private readonly int direction;
        private readonly int statLife;
        private readonly int statLifeMax;
        private readonly float gravDir;
        private readonly float headRotation;
        private readonly Rectangle bodyFrame;
        private readonly Rectangle legFrame;
        private readonly Rectangle headFrame;

        private PlayerDrawState(Player player)
        {
            ghost = player.ghost;
            dead = player.dead;
            socialIgnoreLight = player.socialIgnoreLight;
            isDisplayDollOrInanimate = player.isDisplayDollOrInanimate;
            direction = player.direction;
            statLife = player.statLife;
            statLifeMax = player.statLifeMax;
            gravDir = player.gravDir;
            headRotation = player.headRotation;
            bodyFrame = player.bodyFrame;
            legFrame = player.legFrame;
            headFrame = player.headFrame;
        }

        public static PlayerDrawState Capture(Player player) => new(player);

        public void Restore(Player player)
        {
            player.ghost = ghost;
            player.dead = dead;
            player.socialIgnoreLight = socialIgnoreLight;
            player.isDisplayDollOrInanimate = isDisplayDollOrInanimate;
            player.direction = direction;
            player.statLife = statLife;
            player.statLifeMax = statLifeMax;
            player.gravDir = gravDir;
            player.headRotation = headRotation;
            player.bodyFrame = bodyFrame;
            player.legFrame = legFrame;
            player.headFrame = headFrame;
        }
    }

    private static void DrawGhost(Camera camera, Player drawPlayer, Vector2 position, float scale)
    {
        byte mouseTextColor = Main.mouseTextColor;
        SpriteEffects effects = drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Color baseColor = new(mouseTextColor / 2 + 100, mouseTextColor / 2 + 100, mouseTextColor / 2 + 100, mouseTextColor / 2 + 100);
        Color color = drawPlayer.GetImmuneAlpha(baseColor, 0f);

        Rectangle frame = new(0, TextureAssets.Ghost.Height() / 4 * drawPlayer.ghostFrame, TextureAssets.Ghost.Width(), TextureAssets.Ghost.Height() / 4);
        Vector2 origin = new(frame.Width * 0.5f, frame.Height * 0.5f);
        Vector2 center = new(position.X - camera.UnscaledPosition.X + drawPlayer.width * scale * 0.5f, position.Y - camera.UnscaledPosition.Y + drawPlayer.height * scale * 0.5f);

        camera.SpriteBatch.Draw(TextureAssets.Ghost.Value, center, frame, color, 0f, origin, scale, effects, 0f);
    }
    #endregion

    #region NPC
    public static void DrawNPCPreview(SpriteBatch sb, NPC npc, Rectangle area)
    {
        const float bottomPadding = 5f;

        if (npc == null || area.Width <= 0 || area.Height <= 0 || npc.type <= NPCID.None)
            return;

        Texture2D texture = TextureAssets.Npc[npc.type].Value;
        Rectangle source = npc.frame.Width > 0 && npc.frame.Height > 0 ? npc.frame : texture.Frame();

        float scale = Math.Min((area.Width - 4f) / source.Width, (area.Height - bottomPadding) / source.Height);
        //scale = BoostSmallPreviewScale(scale, 1.45f);

        Vector2 position = new(area.Center.X, area.Bottom - bottomPadding - source.Height * scale * 0.5f);
        SpriteEffects effects = npc.spriteDirection >= 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Color color = npc.GetAlpha(Color.White);

        sb.Draw(texture, position, source, color, 0f, source.Size() * 0.5f, scale, effects, 0f);
    }

    public static void DrawNPCHead(SpriteBatch sb, NPC npc, Rectangle area)
    {
        int bossHeadId = npc.type >= NPCID.None && npc.type < NPCID.Sets.BossHeadTextures.Length ? NPCID.Sets.BossHeadTextures[npc.type] : -1;

        if (bossHeadId >= 0)
        {
            Main.BossNPCHeadRenderer.DrawWithOutlines(null, bossHeadId, area.Center.ToVector2(), Color.White, 0f, 0.52f, SpriteEffects.None);
            return;
        }

        Texture2D texture = TextureAssets.Npc[npc.type].Value;
        Rectangle source = npc.frame.Width > 0 && npc.frame.Height > 0 ? npc.frame : texture.Frame();
        float scale = Math.Min(area.Width / (float)source.Width, area.Height / (float)source.Height);

        sb.Draw(texture, area.Center.ToVector2(), source, Color.White, 0f, source.Size() * 0.5f, scale, npc.spriteDirection >= 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);
    }
    #endregion
}
