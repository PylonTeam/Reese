using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using Reese.Core.Stats;
using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

internal sealed class UINPCDetailPanel : UIPanel
{
    public int NPCIndex { get; }

    private readonly float scale;

    public UINPCDetailPanel(int npcIndex, float scale)
    {
        NPCIndex = npcIndex;
        this.scale = scale;

        SetPadding(0f);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        BackgroundColor = new Color(28, 36, 76) * 0.92f;
        BorderColor = Color.Yellow;
        base.DrawSelf(sb);

        if (!UINPCCard.IsValidNPC(NPCIndex))
            return;

        NPC npc = Main.npc[NPCIndex];
        Rectangle rect = GetDimensions().ToRectangle();
        int shrink = (int)MathF.Round(5f * scale);
        int buttonSize = (int)MathF.Round(32f * scale);
        int buttonGap = (int)MathF.Round(2f * scale);
        int previewWidth = buttonSize * 2 + buttonGap;
        Rectangle contentRect = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);
        Rectangle previewRect = new(contentRect.X, contentRect.Y, previewWidth, contentRect.Height - buttonSize - (int)MathF.Round(3f * scale));
        Rectangle infoRect = new(previewRect.Right + (int)MathF.Round(6f * scale), contentRect.Y + (int)MathF.Round(6f * scale), contentRect.Right - previewRect.Right - (int)MathF.Round(14f * scale), contentRect.Height);
        Rectangle nameRect = new(infoRect.X, infoRect.Y - 2, infoRect.Width, (int)MathF.Round(24f * scale));

        EntityDrawer.DrawEntityBackground(sb, previewRect);
        EntityDrawer.DrawNPCPreview(sb, npc, previewRect);

        string displayName = StatDrawer.Truncate(FontAssets.MouseText.Value, npc.FullName, nameRect.Width, scale);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(displayName) * scale;
        Utils.DrawBorderString(sb, displayName, new Vector2(nameRect.X, nameRect.Y + (nameRect.Height - nameSize.Y) * 0.5f + 4f), Color.White, scale);

        int statH = (int)MathF.Round(27f * scale);
        int statG = (int)MathF.Round(3f * scale);
        Rectangle lifeRect = new(infoRect.X, nameRect.Bottom + (int)MathF.Round(2f * scale), infoRect.Width, statH);
        Rectangle damageRect = new(infoRect.X, lifeRect.Bottom + statG, infoRect.Width, statH);
        Rectangle defenseRect = new(infoRect.X, damageRect.Bottom + statG, infoRect.Width, statH);

        StatDrawer.DrawNPCStat(sb, lifeRect, NPCStats.Life(npc), scale);
        StatDrawer.DrawNPCStat(sb, damageRect, NPCStats.Damage(npc), scale);
        StatDrawer.DrawNPCStat(sb, defenseRect, NPCStats.Defense(npc), scale);
    }
}
