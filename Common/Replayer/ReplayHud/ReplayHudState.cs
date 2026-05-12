using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud;

public class ReplayHudState : UIState
{
    private ReplaySpectate.SpectateHud spectateHud;
    private ReplayInfo.InfoHud infoHud;
    private ReplayControls.PlaybackHud playbackHud;

    public override void OnActivate()
    {
        RemoveAllChildren();

        spectateHud = null;
        infoHud = null;
        playbackHud = null;

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
            Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Update();
        else
            Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Clear();

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (ReplayPlayback.IsReplayPlayback)
            Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay.TeammateHudOverlay.Draw(spriteBatch);
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
        spectateHud ??= new ReplaySpectate.SpectateHud();
        infoHud ??= new ReplayInfo.InfoHud();
        playbackHud ??= new ReplayControls.PlaybackHud();

        if (spectateHud.Parent is null)
            Append(spectateHud);

        if (infoHud.Parent is null)
            Append(infoHud);

        if (playbackHud.Parent is null)
            Append(playbackHud);
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

    public void ClosePlaybackHud()
    {
        playbackHud?.Remove();
    }
}