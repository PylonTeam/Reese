using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replay.Hud.ReplayInfo;
using Reese.Common.Replay.Hud.Shared.UI;
using Reese.Core.Stats;
using System;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replay.Hud.Shared.Sections;

internal sealed class UISpectatorSectionElement : UIPanel
{
    private const float HeaderHeight = 34f;
    private const float RowHeight = 30f;
    private const float RowStep = 34f;

    private const float SliderWidth = 104f;
    private const float SliderHeight = 14f;
    private const float SliderRightPadding = 8f;
    private const float SliderTextLeftPadding = 10f;
    private const float SliderTextGap = 8f;

    private const int ContentInset = 7;

    private readonly SpectatorSectionBase section;

    public UISpectatorSectionElement(SpectatorSectionBase section)
    {
        this.section = section;

        Width.Set(0f, 1f);
        Height.Set(section.Height, 0f);
        SetPadding(0f);
        BackgroundColor = new Color(20, 27, 62) * 0.95f;
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
        float y = HeaderHeight + 4f;

        for (int i = 0; i < rows.Count; i++)
        {
            UISpectatorSectionRowElement row = new(rows[i], section, IsMouseInsideSpectatorInfoPanel);
            row.Top.Set(y, 0f);
            row.Left.Set(ContentInset, 0f);
            row.Width.Set(-ContentInset * 2, 1f);
            row.Height.Set(RowHeight, 0f);
            Append(row);

            y += RowStep;
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

    private sealed class UISpectatorSectionRowElement : UIElement
    {
        private readonly SpectatorSectionRow row;
        private readonly SpectatorSectionBase section;
        private readonly Func<bool> canShowHover;
        private readonly Slider sliderElement;

        public UISpectatorSectionRowElement(SpectatorSectionRow row, SpectatorSectionBase section, Func<bool> canShowHover)
        {
            this.row = row;
            this.section = section;
            this.canShowHover = canShowHover;

            if (row.OnLeftClick is not null)
                OnLeftClick += (_, _) => row.OnLeftClick();

            if (row.OnRightClick is not null)
                OnRightClick += (_, _) => row.OnRightClick();

            if (row.Slider.HasValue)
            {
                SliderRowConfig sliderRowConfig = row.Slider.Value;

                sliderElement = new Slider
                {
                    Top = new StyleDimension((RowHeight - SliderHeight) * 0.5f, 0f),
                    Left = new StyleDimension(-SliderWidth - SliderRightPadding, 1f),
                    Height = new StyleDimension(SliderHeight, 0f),
                    HighlightColor = Main.OurFavoriteColor
                };

                sliderElement.Width.Set(SliderWidth, 0f);
                sliderElement.SetRatio(sliderRowConfig.GetRatio());
                sliderElement.OnDrag += sliderRowConfig.SetRatio;
                sliderElement.OnRelease += sliderRowConfig.SetRatio;

                Append(sliderElement);
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            Rectangle box = GetDimensions().ToRectangle();
            Texture2D icon = row.GetIcon?.Invoke();
            string text = row.GetText?.Invoke() ?? string.Empty;
            Color textColor = row.GetTextColor?.Invoke() ?? GetDefaultTextColor();

            if (sliderElement is not null)
            {
                StatDrawer.DrawWorldStatPanel(sb, box, null, "", null, textColor: Color.White, iconScale: 1f, label: null);

                float scale = 0.82f;
                float sliderLeft = box.Right - SliderRightPadding - SliderWidth;
                float maxTextWidth = sliderLeft - box.X - SliderTextLeftPadding - SliderTextGap;
                Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * scale;

                if (textSize.X > maxTextWidth && maxTextWidth > 20f)
                    scale *= maxTextWidth / textSize.X;

                Utils.DrawBorderString(sb, text, new Vector2(box.X + SliderTextLeftPadding, box.Y + 6f), textColor, scale);
            }
            else
            {
                string commonTooltipText = section.UsesCommonRowTooltips && canShowHover() ? text : null;
                string label = section.UsesOptionRowStyle ? null : row.Label;
                Color valueColor = row.GetTextColor is null && !section.UsesOptionRowStyle ? Color.Gray : textColor;

                StatDrawer.DrawWorldStatPanel(sb, box, icon, text, commonTooltipText, textColor: valueColor, iconScale: row.IconScale, label: label);
            }
        }

        private Color GetDefaultTextColor()
        {
            bool isOption = row.IsOptionOverride ?? section.UsesOptionRowStyle;
            return isOption ? IsMouseHovering ? Color.White : Color.Gray : Color.White;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (sliderElement is not null)
            {
                if (!sliderElement.IsHeld)
                    sliderElement.SetRatio(row.Slider.Value.GetRatio());

                sliderElement.HighlightColor = Main.OurFavoriteColor;
            }

            if (!IsMouseHovering || !canShowHover())
                return;

            Main.LocalPlayer.mouseInterface = true;

            if (!string.IsNullOrEmpty(row.Tooltip))
                Main.instance.MouseText(row.Tooltip);
        }
    }
}
