using Microsoft.Xna.Framework.Graphics;
using Reese.Common.ReplaySpectate.Drawers;
using Reese.Common.ReplaySpectate.UI;
using System;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.ReplaySpectate.UI.Tabs.World;

internal sealed class UIWorldSectionElement : UIPanel
{
    private const float HeaderHeight = 34f;
    private const float RowHeight = 30f;
    private const float RowStep = 34f;
    private const int ContentInset = 7;
    private const int BossSlotStep = 43;
    private const int BossSlotSize = 38;

    private readonly WorldSectionBase section;

    public UIWorldSectionElement(WorldSectionBase section)
    {
        this.section = section;

        Width.Set(0f, 1f);
        Height.Set(section.Height, 0f);
        SetPadding(0f);
        BackgroundColor = new Color(28, 36, 76) * 0.92f;
        BorderColor = new Color(116, 154, 255) * 0.75f;

        BuildRows();
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);

        Rectangle box = GetDimensions().ToRectangle();
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(box.X + 10, box.Y + 28, box.Width - 20, 2), Color.White * 0.10f);
        Utils.DrawBorderString(sb, section.HeaderText, new Vector2(box.X + 10, box.Y + 6), new Color(255, 228, 140), 0.9f);

        DrawBossGrid(sb, box);
    }

    private void BuildRows()
    {
        IReadOnlyList<WorldSectionRow> rows = section.GetRows();

        for (int i = 0; i < rows.Count; i++)
        {
            UIWorldSectionRowElement row = new(rows[i], section, IsMouseInsideSpectatorInfoPanel);
            row.Top.Set(HeaderHeight + 4f + i * RowStep, 0f);
            row.Left.Set(ContentInset, 0f);
            row.Width.Set(-ContentInset * 2, 1f);
            row.Height.Set(RowHeight, 0f);
            Append(row);
        }
    }

    private void DrawBossGrid(SpriteBatch sb, Rectangle box)
    {
        IReadOnlyList<WorldBossEntry> bosses = section.GetBosses();
        if (bosses.Count == 0)
            return;

        int rows = section.GetRows().Count;
        int gridX = box.X + ContentInset + 2;
        int gridY = box.Y + (int)(HeaderHeight + 4f + rows * RowStep + 10f);
        int gridColumns = Math.Max(1, (box.Right - ContentInset - gridX + 8) / BossSlotStep);
        bool canShowHover = IsMouseInsideSpectatorInfoPanel();

        for (int i = 0; i < bosses.Count; i++)
        {
            int col = i % gridColumns;
            int row = i / gridColumns;
            WorldBossEntry boss = bosses[i];
            Rectangle slot = new(gridX + col * BossSlotStep, gridY + row * BossSlotStep, BossSlotSize, BossSlotSize);

            Utils.DrawInvBG(sb, slot, new Color(83, 97, 168) * 0.80f);

            int headNpc = boss.HeadNpcId;
            if (headNpc >= 0 && headNpc < NPCID.Sets.BossHeadTextures.Length && NPCID.Sets.BossHeadTextures[headNpc] != -1)
                Main.BossNPCHeadRenderer.DrawWithOutlines(null, NPCID.Sets.BossHeadTextures[headNpc], slot.Center.ToVector2(), boss.Downed ? Color.White : Color.White * 0.25f, 0f, 0.78f, SpriteEffects.None);

            if (boss.Downed)
            {
                Texture2D checkTexture = Ass.Icon_CheckmarkGreen.Value;
                sb.Draw(checkTexture, slot.Center.ToVector2(), null, Color.White, 0f, checkTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }

            if (canShowHover)
                ShowHover(slot, $"{boss.Name} ({(boss.Downed ? "Downed" : "Not downed")})");
        }
    }

    private bool IsMouseInsideSpectatorInfoPanel()
    {
        for (UIElement element = this; element is not null; element = element.Parent)
        {
            if (element is SpectatorInfoPanel panel)
                return panel.ContainsPoint(Main.MouseScreen);
        }

        return true;
    }

    private static void ShowHover(Rectangle area, string text)
    {
        if (!area.Contains(Main.MouseScreen.ToPoint()))
            return;

        Main.LocalPlayer.mouseInterface = true;
        Main.instance.MouseText(text);
    }

    private sealed class UIWorldSectionRowElement : UIElement
    {
        private readonly WorldSectionRow row;
        private readonly WorldSectionBase section;
        private readonly Func<bool> canShowHover;

        public UIWorldSectionRowElement(WorldSectionRow row, WorldSectionBase section, Func<bool> canShowHover)
        {
            this.row = row;
            this.section = section;
            this.canShowHover = canShowHover;

            if (row.OnLeftClick is not null)
                OnLeftClick += (_, _) => row.OnLeftClick();

            if (row.OnRightClick is not null)
                OnRightClick += (_, _) => row.OnRightClick();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            Rectangle box = GetDimensions().ToRectangle();
            Texture2D icon = row.GetIcon?.Invoke();
            string text = row.GetText?.Invoke() ?? string.Empty;
            Color textColor = row.GetTextColor?.Invoke() ?? (IsMouseHovering ? Color.White : Color.Gray);

            string commonTooltipText = section.UsesCommonRowTooltips && canShowHover() ? text : null;
            StatDrawer.DrawWorldStatPanel(sb, box, icon, text, commonTooltipText, textColor: textColor, iconScale: row.IconScale);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!IsMouseHovering || !canShowHover())
                return;

            Main.LocalPlayer.mouseInterface = true;

            if (!string.IsNullOrEmpty(row.Tooltip))
                Main.instance.MouseText(row.Tooltip);
        }
    }
}
