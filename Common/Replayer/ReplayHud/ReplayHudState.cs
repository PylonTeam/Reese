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

    public override void OnActivate()
    {
        RemoveAllChildren();

        spectateHud = null;
        infoHud = null;
        playbackHud = null;

        showSpectateHud = true;
        showInfoHud = true;
        showPlaybackHud = true;

        EnsureHuds();
    }

    public void Rebuild()
    {
        spectateHud?.Rebuild();
        infoHud?.Rebuild();
        playbackHud?.Rebuild();
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
        {
            spectateHud ??= new ReplaySpectate.SpectateHud();

            if (spectateHud.Parent is null)
                Append(spectateHud);
        }
        else
        {
            spectateHud?.Remove();
        }

        if (showInfoHud)
        {
            infoHud ??= new ReplayInfo.InfoHud();

            if (infoHud.Parent is null)
                Append(infoHud);
        }
        else
        {
            infoHud?.Remove();
        }

        if (showPlaybackHud)
        {
            playbackHud ??= new ReplayControls.PlaybackHud();

            if (playbackHud.Parent is null)
                Append(playbackHud);
        }
        else
        {
            playbackHud?.Remove();
        }
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
    }

    public void CloseInfoHud()
    {
        showInfoHud = false;
        infoHud?.Remove();
    }

    public void ClosePlaybackHud()
    {
        showPlaybackHud = false;
        playbackHud?.Remove();
    }
}