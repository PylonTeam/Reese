using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replay.Hud.ReplaySpectate;
using Reese.Common.Replay.Hud.ReplaySpectate.TeammateOverlay;
using Reese.Common.Spectator;
using Terraria.UI;

namespace Reese.Common.Replay.Hud.Ghost;

public sealed class GhostHudState : UIState
{
    private SpectateHud spectateHud;
    private GhostHud ghostHud;

    private bool showGhostHud = true;

    public bool HasVisibleHuds => ReplayClientSettings.ShowSpectateHud || showGhostHud;

    public override void OnActivate()
    {
        RemoveAllChildren();

        spectateHud = null;
        ghostHud = null;

        ShowAllHuds();

        EnsureHuds();
    }

    public void ShowAllHuds()
    {
        showGhostHud = true;
    }

    public void Rebuild()
    {
        RemoveAllChildren();
        spectateHud = null;
        ghostHud = null;
        EnsureHuds();
    }

    public override void Update(GameTime gameTime)
    {
        UpdateVisibleHuds();

        if (SpectatorMode.CanSpectate)
            TeammateHudOverlay.Update();
        else
            TeammateHudOverlay.Clear();

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (SpectatorMode.CanSpectate)
            TeammateHudOverlay.Draw(spriteBatch);
    }

    private void UpdateVisibleHuds()
    {
        if (!SpectatorMode.CanUseGhostHud)
        {
            RemoveHuds();
            return;
        }

        EnsureHuds();
    }

    private void EnsureHuds()
    {
        if (ReplayClientSettings.ShowSpectateHud)
            spectateHud ??= new SpectateHud();
        else
        {
            spectateHud?.Remove();
            spectateHud = null;
        }

        if (showGhostHud)
            ghostHud ??= new GhostHud();

        if (ReplayClientSettings.ShowSpectateHud && spectateHud.Parent == null) Append(spectateHud);
        if (showGhostHud && ghostHud.Parent == null) Append(ghostHud);
    }

    private void RemoveHuds()
    {
        spectateHud?.Remove();
        ghostHud?.Remove();

        spectateHud = null;
        ghostHud = null;

        TeammateHudOverlay.Clear();
    }

    public void CloseSpectateHud()
    {
        if (ReplayClientSettings.ShowSpectateHud)
            ReplayClientSettings.ToggleShowSpectateHud();

        spectateHud?.Remove();
        spectateHud = null;
    }

    public void CloseGhostHud()
    {
        showGhostHud = false;
        ghostHud?.Remove();
        ghostHud = null;
    }
}
