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
        if (area.Width <= 0 || area.Height <= 0)
            return;

        Player drawPlayer = CreateFullDrawPlayer(player);

        //float scale = Math.Min(area.Width / (drawPlayer.width + 4f), area.Height / drawPlayer.height);
        float scale = GetPlayerScale();

        Vector2 drawSize = new(drawPlayer.width * scale, drawPlayer.height * scale);
        Vector2 drawPos = new(
            (int)MathF.Round(area.Center.X - drawSize.X * 0.5f),
            (int)MathF.Round(area.Center.Y - drawSize.Y * 0.5f + drawPlayer.gfxOffY * scale));

        drawPos.Y += GetPlayerScaleVerticalOffset();

        //DebugDrawer.DrawRectangle(area);

        if (drawPlayer.statLife <= 0)
        {
            DrawRespawnTime(sb, player, area);
        }

        DrawFullPlayer(sb, player, drawPos, scale);
    }

    private static void DrawRespawnTime(SpriteBatch sb, Player player, Rectangle area)
    {
        area.Y -= 5;
        DrawCenteredTexture(sb, Ass.IconDead.Value, area, 0.45f);
        area.Y += 5;

        int seconds = Math.Max(0, (int)Math.Ceiling(player.respawnTimer / 60f));
        string text = seconds.ToString();

        DynamicSpriteFont font = FontAssets.DeathText.Value;
        float textScale = 0.6f;

        Vector2 textSize = font.MeasureString(text) * textScale;
        Vector2 textPosition = new(
            area.X + (area.Width - textSize.X) * 0.5f,
            area.Y + (area.Height - textSize.Y) * 0.5f + 10f
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
        Player drawPlayer = CreateFullDrawPlayer(player);
        Rectangle oldScissor = sb.GraphicsDevice.ScissorRectangle;
        RasterizerState oldRasterizer = sb.GraphicsDevice.RasterizerState;

        sb.End();
        sb.GraphicsDevice.ScissorRectangle = oldScissor;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, ClippedCullNone, null, Main.UIScaleMatrix);

        FullBrightPlayerDrawer.ForceFullBrightOnce = true;
        //PlayerOutlines.ForcePreviewOutline = true;

        try
        {
            // debug
            //if (Main.GameUpdateCount % 60 == 0)
                //Log.Chat($"{drawPlayer.name}: ({drawPlayer.whoAmI}) ghost={drawPlayer.ghost}, dead={drawPlayer.dead}, life={drawPlayer.statLife}");

            bool isDead = drawPlayer.dead || drawPlayer.statLife <= 0;
            bool drawAsGhost = drawPlayer.ghost && !isDead;

            if (drawAsGhost)
            {
                position.X += 2f;
                position.Y += 8f;
                DrawGhost(Main.Camera, drawPlayer, position + Main.screenPosition, scale);
            }
            else
            {
                drawPlayer.ghost = false;
                drawPlayer.dead = false;

                //if (drawPlayer.statLife <= 0)
                    //drawPlayer.statLife = 1;

                if (drawPlayer.statLife > 0)
                {
                    Main.PlayerRenderer.DrawPlayer(Main.Camera, drawPlayer, position + Main.screenPosition, 0f, Vector2.Zero, 0f, scale);
                }
            }
        }
        finally
        {
            FullBrightPlayerDrawer.ForceFullBrightOnce = false;
            //PlayerOutlines.ForcePreviewOutline = false;
        }

        sb.End();
        sb.GraphicsDevice.ScissorRectangle = oldScissor;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, oldRasterizer, null, Main.UIScaleMatrix);
    }

    public static void DrawPlayerHead(SpriteBatch sb, Player player, Vector2 position, float scale = 1f)
    {
        Player drawPlayer = CreateHeadDrawPlayer(player);
        Color borderColor = player.team > 0 ? Main.teamColor[player.team] : Color.Black;

        FullBrightPlayerDrawer.ForceFullBrightOnce = true;

        try
        {
            Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, drawPlayer, position, scale: scale, borderColor: borderColor);
        }
        finally
        {
            FullBrightPlayerDrawer.ForceFullBrightOnce = false;
        }
    }

    private static Player CreateFullDrawPlayer(Player player)
    {
        Player drawPlayer = player.SerializedClone();
        CopyPlayerDrawAppearance(player, drawPlayer);
        drawPlayer.position = player.position;
        drawPlayer.velocity = player.velocity;
        drawPlayer.direction = player.direction;
        drawPlayer.gravDir = player.gravDir;
        drawPlayer.fullRotation = player.fullRotation;
        drawPlayer.fullRotationOrigin = player.fullRotationOrigin;
        drawPlayer.selectedItem = player.selectedItem;
        drawPlayer.itemAnimation = player.itemAnimation;
        drawPlayer.itemAnimationMax = player.itemAnimationMax;
        drawPlayer.itemRotation = player.itemRotation;
        drawPlayer.heldProj = player.heldProj;
        drawPlayer.bodyFrame = player.bodyFrame;
        drawPlayer.legFrame = player.legFrame;
        drawPlayer.headFrame = player.headFrame;
        drawPlayer.wingFrame = player.wingFrame;
        drawPlayer.wings = player.wings;
        drawPlayer.gfxOffY = player.gfxOffY;
        drawPlayer.dead = false;
        drawPlayer.ghost = (player.ghost || player.dead) && GhostDrawSystem.ShouldDrawGhost(player);

        if (drawPlayer.ghost)
        {
            drawPlayer.ghostFade = 1f;
            drawPlayer.ghostDir = 1;
        }

        drawPlayer.socialIgnoreLight = true;
        drawPlayer.isDisplayDollOrInanimate = false;

        return drawPlayer;
    }

    private static void CopyPlayerDrawAppearance(Player from, Player to)
    {
        to.head = from.head;
        to.body = from.body;
        to.legs = from.legs;

        to.cHead = from.cHead;
        to.cBody = from.cBody;
        to.cLegs = from.cLegs;

        to.face = from.face;
        to.neck = from.neck;
        to.front = from.front;
        to.back = from.back;
        to.waist = from.waist;
        to.shield = from.shield;
        to.shoe = from.shoe;
        to.balloon = from.balloon;
        to.beard = from.beard;

        to.handon = from.handon;
        to.handoff = from.handoff;

        to.wings = from.wings;
        to.wingsLogic = from.wingsLogic;
        to.wingFrame = from.wingFrame;
        to.wingFrameCounter = from.wingFrameCounter;

        to.carpet = from.carpet;
        to.carpetFrame = from.carpetFrame;

        to.shieldRaised = from.shieldRaised;
        to.shieldParryTimeLeft = from.shieldParryTimeLeft;

        to.invis = from.invis;
        to.headcovered = from.headcovered;
        to.head = from.head;
    }

    private static Player CreateHeadDrawPlayer(Player player)
    {
        Player headPlayer = player.SerializedClone();
        headPlayer.dead = false;
        headPlayer.ghost = (player.ghost || player.dead) && GhostDrawSystem.ShouldDrawGhost(player);

        if (headPlayer.ghost)
        {
            headPlayer.ghostFade = 1f;
            headPlayer.ghostDir = 1;
        }

        headPlayer.socialIgnoreLight = true;
        headPlayer.isDisplayDollOrInanimate = true;

        return headPlayer;
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
