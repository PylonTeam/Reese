using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplayHud.Info;
using Reese.Common.Replayer.ReplayHud.Spectate.Stats;
using System;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Shared.Sections;

internal sealed class UISpectatorSectionElement : UIPanel
{
    private const float HeaderHeight = 34f;
    private const float RowHeight = 30f;
    private const float RowStep = 34f;
    private const int ContentInset = 7;

    private readonly SpectatorSectionBase section;

    public UISpectatorSectionElement(SpectatorSectionBase section)
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
    }

    private void BuildRows()
    {
        IReadOnlyList<SpectatorSectionRow> rows = section.GetRows();

        for (int i = 0; i < rows.Count; i++)
        {
            UISpectatorSectionRowElement row = new(rows[i], section, IsMouseInsideSpectatorInfoPanel);
            row.Top.Set(HeaderHeight + 4f + i * RowStep, 0f);
            row.Left.Set(ContentInset, 0f);
            row.Width.Set(-ContentInset * 2, 1f);
            row.Height.Set(RowHeight, 0f);
            Append(row);
        }
    }

    private bool IsMouseInsideSpectatorInfoPanel()
    {
        for (UIElement element = this; element is not null; element = element.Parent)
        {
            if (element is InfoHud panel)
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

    private sealed class UISpectatorSectionRowElement : UIElement
    {
        private readonly SpectatorSectionRow row;
        private readonly SpectatorSectionBase section;
        private readonly Func<bool> canShowHover;

        public UISpectatorSectionRowElement(SpectatorSectionRow row, SpectatorSectionBase section, Func<bool> canShowHover)
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
            Color textColor = row.GetTextColor?.Invoke() ?? GetDefaultTextColor();

            string commonTooltipText = section.UsesCommonRowTooltips && canShowHover() ? text : null;
            StatDrawer.DrawWorldStatPanel(sb, box, icon, text, commonTooltipText, textColor: textColor, iconScale: row.IconScale);
        }

        private Color GetDefaultTextColor()
        {
            bool isOption = row.IsOptionOverride ?? section.UsesOptionRowStyle;
            return isOption ? IsMouseHovering ? Color.White : Color.Gray : Color.White;
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
