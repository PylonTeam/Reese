using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;

namespace Reese.Common.MainMenu;

public class HoverInfoPill : UIPanel
{
    private readonly string tooltip;
    private readonly UIText text;

    public HoverInfoPill(string value, string tooltip)
    {
        this.tooltip = tooltip;

        Width.Set(120f, 0f);
        Height.Set(24f, 0f);
        SetPadding(4f);

        BackgroundColor = new Color(44, 57, 105) * 0.95f;
        BorderColor = new Color(70, 90, 160) * 0.9f;

        text = new UIText(value, 0.75f, false)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        Append(text);
    }

    public void SetValue(string value)
    {
        text.SetText(value);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;
            UICommon.TooltipMouseText(tooltip);
        }
    }
}