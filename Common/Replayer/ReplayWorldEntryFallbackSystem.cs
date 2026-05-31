using Reese.Common.Replayer.ReplayHud;

namespace Reese.Common.Replayer;

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayWorldEntryFallbackSystem : ModSystem
{
    private const uint GoToStartTriggerTick = 1500;
    private bool goToStartQueued;

    public override void UpdateUI(GameTime gameTime)
    {
        if (!ReplayPlayback.IsReplayPlayback)
        {
            goToStartQueued = false;
            return;
        }

        if (goToStartQueued || ReplayPlayback.CurrentTick <= GoToStartTriggerTick)
            return;

        if (Netplay.Connection?.Socket is not Replayer.ReplaySocket)
            return;

        if (!ModContent.GetInstance<ReplayHudSystem>().IsReplayHudOpen())
            return;

        goToStartQueued = true;
        GoToStartOnce();
    }

    private static void GoToStartOnce()
    {
        if (!ReplayPlayback.IsReplayPlayback)
            return;

        Log.Info($"Replay tick advanced to {ReplayPlayback.CurrentTick}; automatically invoking Go to start once.");
        ReplayPlayback.SeekToStart();
    }
}
