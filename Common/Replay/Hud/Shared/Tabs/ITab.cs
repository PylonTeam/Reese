using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace Reese.Common.Replay.Hud.Shared.Tabs;

internal interface ITab
{
    SpectatorTab Tab { get; }
    string HeaderText { get; }
    string TooltipText { get; }
    Asset<Texture2D> Icon { get; }
    float IconScale { get; }
    Vector2 IconOffset => Vector2.Zero;
    Vector2 TextOffset => Vector2.Zero;
    void Refresh();
}

internal enum SpectatorTab
{
    Settings,
    NPCs,
    World,
    Replay,
    Players
}
