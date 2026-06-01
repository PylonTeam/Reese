using Reese.Common.Replayer.ReplayEvents;
using System.Collections.Generic;
using Terraria.ID;

namespace Reese.Common.Recorder;

[Autoload(Side = ModSide.Server)]
internal sealed class ReplayTimelineTrackerSystem : ModSystem
{
    private readonly Dictionary<string, bool> bossStates = [];
    private bool hasRecordingSnapshot;
    private int previousInvasionType = InvasionID.None;

    public void ResetForRecordingStart()
    {
        hasRecordingSnapshot = true;
        SnapshotBosses();
        previousInvasionType = Main.invasionType;
    }

    public void EndRecording()
    {
        hasRecordingSnapshot = false;
        bossStates.Clear();
        previousInvasionType = InvasionID.None;
    }

    public override void PostUpdateEverything()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        Recorder recorder = ModContent.GetInstance<Recorder>();
        if (!recorder.IsRecording)
        {
            if (hasRecordingSnapshot)
                EndRecording();

            return;
        }

        if (!hasRecordingSnapshot)
            ResetForRecordingStart();

        TrackBosses(recorder.Ticks);
        TrackInvasions(recorder.Ticks);
    }

    private void SnapshotBosses()
    {
        bossStates.Clear();

        foreach (ReplayBossDefinition boss in ReplayBossDefinitions.GetDefinitions())
            bossStates[boss.Key] = boss.IsDefeated();
    }

    private void TrackBosses(uint tick)
    {
        foreach (ReplayBossDefinition boss in ReplayBossDefinitions.GetDefinitions())
        {
            bool defeated = boss.IsDefeated();

            if (!bossStates.TryGetValue(boss.Key, out bool previousDefeated))
            {
                bossStates[boss.Key] = defeated;
                continue;
            }

            if (!previousDefeated && defeated)
                ReplayTimelineRecorder.Add(boss.CreateEvent(tick));

            bossStates[boss.Key] = defeated;
        }
    }

    private void TrackInvasions(uint tick)
    {
        int currentInvasionType = Main.invasionType;

        if (currentInvasionType != InvasionID.None && currentInvasionType != previousInvasionType)
            ReplayTimelineRecorder.Add(ReplayInvasionDefinitions.CreateEvent(tick, currentInvasionType));

        previousInvasionType = currentInvasionType;
    }
}
