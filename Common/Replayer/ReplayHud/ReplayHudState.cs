using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Spectator;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud;

public class ReplayHudState : UIState
{
    private ReplaySpectate.SpectateHud spectateHud;
    private ReplayInfo.InfoHud infoHud;
    private ReplayControls.PlaybackHud playbackHud;

    private bool showInfoHud = true;

    public bool HasVisibleHuds => ReplayClientSettings.ShowSpectateHud || showInfoHud || ReplayClientSettings.ShowPlaybackHud;

    public override void OnActivate()
    {
        RemoveAllChildren();

        spectateHud = null;
        infoHud = null;
        playbackHud = null;

        ShowAllHuds();

        EnsureHuds();
    }

    public void ShowAllHuds()
    {
        showInfoHud = true;
    }

    public void Rebuild()
    {
        RemoveAllChildren();
        spectateHud = null;
        infoHud = null;
        playbackHud = null;
        EnsureHuds();
    }

    public override void Update(GameTime gameTime)
    {
        UpdateVisibleHuds();

        if (SpectatorMode.CanSpectate)
            ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Update();
        else
            ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Clear();

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (SpectatorMode.CanSpectate)
            ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Draw(spriteBatch);
    }

    private void UpdateVisibleHuds()
    {
        if (!SpectatorMode.CanUseReplayHud)
        {
            RemoveHuds();
            return;
        }

        EnsureHuds();
    }

    private void EnsureHuds()
    {
        if (ReplayClientSettings.ShowSpectateHud)
            spectateHud ??= new ReplaySpectate.SpectateHud();
        else
        {
            spectateHud?.Remove();
            spectateHud = null;
        }

        if (showInfoHud)
            infoHud ??= new ReplayInfo.InfoHud();

        if (ReplayClientSettings.ShowPlaybackHud)
            playbackHud ??= new ReplayControls.PlaybackHud();
        else
        {
            playbackHud?.Remove();
            playbackHud = null;
        }

        if (ReplayClientSettings.ShowSpectateHud && spectateHud.Parent == null) Append(spectateHud);
        if (showInfoHud && infoHud.Parent == null) Append(infoHud);
        if (ReplayClientSettings.ShowPlaybackHud && playbackHud.Parent == null) Append(playbackHud);
    }

    private void RemoveHuds()
    {
        spectateHud?.Remove();
        infoHud?.Remove();
        playbackHud?.Remove();

        spectateHud = null;
        infoHud = null;
        playbackHud = null;

        ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Clear();
    }

    // Close huds
    public void CloseSpectateHud()
    {
        if (ReplayClientSettings.ShowSpectateHud)
            ReplayClientSettings.ToggleShowSpectateHud();

        spectateHud?.Remove();
        spectateHud = null;
    }

    public void CloseInfoHud()
    {
        showInfoHud = false;
        infoHud?.Remove();
        infoHud = null;
    }

    public void ClosePlaybackHud()
    {
        if (ReplayClientSettings.ShowPlaybackHud)
            ReplayClientSettings.ToggleShowPlaybackHud();

        playbackHud?.Remove();
        playbackHud = null;
    }
}
