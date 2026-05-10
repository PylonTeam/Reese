using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace Reese.Common.Replayer.ReplaySpectate.UI.Tabs;

internal interface ITab
{
    SpectatorTab Tab { get; }
    string HeaderText { get; }
    string TooltipText { get; }
    Asset<Texture2D> Icon { get; }
    float IconScale { get; }
    Vector2 IconOffset { get; }
    void Refresh();
}

internal enum SpectatorTab
{
    NPCs,
    World,
    Replay
}
