using Reese.Common.Replayer.ReplayEvents;
using System.Collections.Generic;
using Terraria.ID;

namespace Reese.Common.Recorder;

[Autoload(Side = ModSide.Server)]
internal sealed class ReplayTimelineTrackerSystem : ModSystem
{
    private readonly HashSet<string> activeBosses = [];
    private bool hasRecordingSnapshot;
    private int previousInvasionType = InvasionID.None;

    public void ResetForRecordingStart()
    {
        hasRecordingSnapshot = true;
        SnapshotActiveBosses();
        previousInvasionType = Main.invasionType;
    }

    public void EndRecording()
    {
        hasRecordingSnapshot = false;
        activeBosses.Clear();
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

        RefreshActiveBosses();
        TrackInvasions(recorder.Ticks);
    }

    public void RecordBossSummoned(NPC npc)
    {
        if (!CanRecordBossEvent(npc, out Recorder recorder, out ReplayBossDefinition boss))
            return;

        if (activeBosses.Add(boss.Key))
            ReplayTimelineRecorder.Add(boss.CreateSummonedEvent(recorder.Ticks));
    }

    public void RecordBossDefeated(NPC npc)
    {
        if (!CanRecordBossEvent(npc, out Recorder recorder, out ReplayBossDefinition boss))
            return;

        if (ReplayBossDefinitions.HasActiveInstance(boss, npc.whoAmI))
            return;

        activeBosses.Remove(boss.Key);
        ReplayTimelineRecorder.Add(boss.CreateDefeatedEvent(recorder.Ticks));
    }

    private void SnapshotActiveBosses()
    {
        activeBosses.Clear();

        foreach (ReplayBossDefinition boss in ReplayBossDefinitions.GetDefinitions())
        {
            if (ReplayBossDefinitions.HasActiveInstance(boss))
                activeBosses.Add(boss.Key);
        }
    }

    private void RefreshActiveBosses()
    {
        activeBosses.RemoveWhere(key => !HasActiveBossWithKey(key));
    }

    private static bool HasActiveBossWithKey(string key)
    {
        foreach (ReplayBossDefinition boss in ReplayBossDefinitions.GetDefinitions())
        {
            if (boss.Key == key)
                return ReplayBossDefinitions.HasActiveInstance(boss);
        }

        return false;
    }

    private static bool CanRecordBossEvent(NPC npc, out Recorder recorder, out ReplayBossDefinition boss)
    {
        recorder = null;
        boss = default;

        if (Main.netMode == NetmodeID.MultiplayerClient || npc == null)
            return false;

        recorder = ModContent.GetInstance<Recorder>();
        return recorder?.IsRecording == true && ReplayBossDefinitions.TryGetDefinition(npc.type, out boss);
    }

    private void TrackInvasions(uint tick)
    {
        int currentInvasionType = Main.invasionType;

        if (currentInvasionType != InvasionID.None && currentInvasionType != previousInvasionType)
            ReplayTimelineRecorder.Add(ReplayInvasionDefinitions.CreateEvent(tick, currentInvasionType));

        previousInvasionType = currentInvasionType;
    }
}
