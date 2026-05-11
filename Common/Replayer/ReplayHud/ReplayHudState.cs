using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud;

public class ReplayHudState : UIState
{
    private Spectate.SpectateHud spectateHud;
    private Info.InfoHud infoHud;
    private Playback.PlaybackHud playbackHud;

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

        if (ReplaySession.IsReplayPlayback)
            Spectate.TeammateOverlay.TeammateHudOverlay.Update();
        else
            Spectate.TeammateOverlay.TeammateHudOverlay.Clear();

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (ReplaySession.IsReplayPlayback)
            Spectate.TeammateOverlay.TeammateHudOverlay.Draw(spriteBatch);
    }

    private void UpdateVisibleHuds()
    {
        if (!ReplaySession.IsReplayPlayback)
        {
            RemoveHuds();
            return;
        }

        EnsureHuds();
    }

    private void EnsureHuds()
    {
        spectateHud ??= new Spectate.SpectateHud();
        infoHud ??= new Info.InfoHud();
        playbackHud ??= new Playback.PlaybackHud();

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

        Spectate.TeammateOverlay.TeammateHudOverlay.Clear();
    }

    public void ClosePlaybackHud()
    {
        playbackHud?.Remove();
    }
}