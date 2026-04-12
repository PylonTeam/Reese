using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;

namespace Reese.Common.UI;

public class ReeseUIHoverImage : UIImage
{
    public string HoverText;

    public bool UseTooltipMouseText;

    public ReeseUIHoverImage(Asset<Texture2D> texture, string hoverText)
        : base(texture)
    {
        HoverText = hoverText;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        if (base.IsMouseHovering)
        {
            Rectangle value = base.Parent.GetDimensions().ToRectangle();
            value.Y = 0;
            value.Height = Main.screenHeight;
            if (UseTooltipMouseText)
            {
                UICommon.TooltipMouseText(HoverText);
            }
            else
            {
                UICommon.DrawHoverStringInBounds(spriteBatch, HoverText, value);
            }
        }
    }
}
