using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud;

public class ReplayHudState : UIState
{
    private ReplaySpectate.SpectateHud spectateHud;
    private ReplayInfo.InfoHud infoHud;
    private ReplayControls.PlaybackHud playbackHud;

    private bool showSpectateHud = true;
    private bool showInfoHud = true;
    private bool showPlaybackHud = true;

    public bool HasVisibleHuds => showSpectateHud || showInfoHud || showPlaybackHud;

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
        showSpectateHud = true;
        showInfoHud = true;
        showPlaybackHud = true;
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

        if (ReplayPlayback.IsReplayPlayback)
            ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Update();
        else
            ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Clear();

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (ReplayPlayback.IsReplayPlayback)
            ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Draw(spriteBatch);
    }

    private void UpdateVisibleHuds()
    {
        if (!ReplayPlayback.IsReplayPlayback)
        {
            RemoveHuds();
            return;
        }

        EnsureHuds();
    }

    private void EnsureHuds()
    {
        if (showSpectateHud)
            spectateHud ??= new ReplaySpectate.SpectateHud();

        if (showInfoHud)
            infoHud ??= new ReplayInfo.InfoHud();

        if (showPlaybackHud)
            playbackHud ??= new ReplayControls.PlaybackHud();

        if (showSpectateHud && spectateHud.Parent == null) Append(spectateHud);
        if (showInfoHud && infoHud.Parent == null) Append(infoHud);
        if (showPlaybackHud && playbackHud.Parent == null) Append(playbackHud);
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
        showSpectateHud = false;
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
        showPlaybackHud = false;
        playbackHud?.Remove();
        playbackHud = null;
    }
}
