using System;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Replay.ReplayHud.Shared.UI;

internal sealed class ClosePanel : UIPanel
{
    private readonly Action onClose;

    public ClosePanel(Action onClose, float scale = 1f, Color? backgroundColor = null)
    {
        this.onClose = onClose;

        Height = new StyleDimension(0f, 1f);
        Width = new StyleDimension(40f * scale, 0f);
        HAlign = 1f;
        VAlign = 0.5f;
        BorderColor = Color.Black;

        if (backgroundColor.HasValue)
            BackgroundColor = backgroundColor.Value;

        SetPadding(0f);

        OnMouseOver += (_, _) =>
        {
            BorderColor = Color.Yellow;
            SoundEngine.PlaySound(SoundID.MenuTick);
        };

        OnMouseOut += (_, _) =>
        {
            BorderColor = Color.Black;
        };

        OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            this.onClose?.Invoke();
        };

        Append(new UIText("X", large: true, textScale: 0.55f * scale)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            TextColor = Color.White
        });
    }
}
