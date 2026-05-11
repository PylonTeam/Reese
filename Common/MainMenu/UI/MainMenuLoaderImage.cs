using ReLogic.Content;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace Reese.Common.MainMenu.UI;

internal sealed class MainMenuLoaderImage : UIElement
{
    public bool WithBackground { get; set; }
    public bool Loading { get; set; }
    public int FrameTick;
    public int Frame;

    private readonly float scale;
    private readonly Asset<Texture2D> backgroundTexture;
    private readonly Asset<Texture2D> loaderTexture;

    public MainMenuLoaderImage(float hAlign, float vAlign, float scale = 1f)
    {
        this.scale = scale;
        backgroundTexture = UICommon.LoaderBgTexture;
        loaderTexture = UICommon.LoaderTexture;

        Width.Set(200f * scale, 0f);
        Height.Set(200f * scale, 0f);
        HAlign = hAlign;
        VAlign = vAlign;
        IgnoresMouseInteraction = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (!Loading || loaderTexture?.Value == null)
            return;

        if (++FrameTick >= 5)
        {
            FrameTick = 0;
            if (++Frame >= 16)
                Frame = 0;
        }

        CalculatedStyle dimensions = GetDimensions();
        Vector2 position = new((int)dimensions.X, (int)dimensions.Y);

        if (WithBackground && backgroundTexture?.Value != null)
            spriteBatch.Draw(backgroundTexture.Value, position, new Rectangle(0, 0, 200, 200), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        Rectangle frame = new(200 * (Frame / 8), 200 * (Frame % 8), 200, 200);
        spriteBatch.Draw(loaderTexture.Value, position, frame, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
