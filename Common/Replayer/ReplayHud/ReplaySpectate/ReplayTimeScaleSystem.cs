using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

/// <summary>
/// How it works:
/// basically hooking 3 methods
/// fast-forward is implemented in DoUpdate by repeating the call multiple times
/// slow-down is implemented in the others with an accumulator where I update 0.25 / 0.5 times per tick and only update after a full 1.0 update has been saved up
/// </summary>
[Autoload(Side = ModSide.Both)]
internal sealed class ReplayTimeScaleSystem : ModSystem
{
    public static readonly float[] SnapValues = [0f, 0.125f, 0.25f, 0.5f, 0.75f, 1f, 2f, 4f, 8f, 16f, 32f];

    /// <summary>
    /// Gets the current time scale factor applied to time-dependent operations.
    /// </summary>
    /// <remarks>A time scale of 1.0 represents normal speed. Values greater than 1.0 accelerate time, while
    /// values between 0.0 and 1.0 slow it down. A value of 0.0 effectively pauses time-dependent operations.</remarks>
    private float timeScale = 1f;

    public float TimeScale => ReplayPlayback.IsReplayPlayback ? timeScale : 1f;

    private double worldUpdateAccumulator;
    private double timeUpdateAccumulator;
    private double extraUpdateAccumulator;
    private bool runningExtraUpdates;

    private bool stepOneFrameRequested;
    private bool consumedWorldStep;
    private bool consumedTimeStep;
    private bool bootstrapTickAdvancementActive;
    private uint bootstrapTicksAdvanced;

    public override void Load()
    {
        if (Main.dedServ)
            return;

        On_Main.DoUpdate += HookDoUpdate;
        On_Main.DoUpdateInWorld += HookDoUpdateInWorld;
        On_Main.UpdateTime += HookUpdateTime;
    }

    public override void Unload()
    {
        if (Main.dedServ)
            return;

        On_Main.DoUpdate -= HookDoUpdate;
        On_Main.DoUpdateInWorld -= HookDoUpdateInWorld;
        On_Main.UpdateTime -= HookUpdateTime;
    }

    public void SetTimeScale(float value)
    {
        timeScale = ReplayPlayback.IsReplayPlayback ? SnapTimeScale(value) : 1f;
        ResetAccumulators();
        ClearStepRequest();
    }

    public void StepOneFrame()
    {
        if (Main.gameMenu || !ReplayPlayback.IsReplayPlayback)
            return;

        stepOneFrameRequested = true;
        consumedWorldStep = false;
        consumedTimeStep = false;
    }

    public static float SnapTimeScale(float rawValue)
    {
        float best = SnapValues[0];
        float bestDistance = Math.Abs(rawValue - best);

        for (int i = 1; i < SnapValues.Length; i++)
        {
            float value = SnapValues[i];
            float distance = Math.Abs(rawValue - value);

            if (distance < bestDistance)
            {
                best = value;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void ResetAccumulators()
    {
        worldUpdateAccumulator = 0d;
        timeUpdateAccumulator = 0d;
        extraUpdateAccumulator = 0d;
    }

    private void ClearStepRequest()
    {
        stepOneFrameRequested = false;
        consumedWorldStep = false;
        consumedTimeStep = false;
    }

    private void TryCompleteStepRequest()
    {
        if (stepOneFrameRequested && consumedWorldStep && consumedTimeStep)
            ClearStepRequest();
    }

    private void HookDoUpdate(On_Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
    {
        orig(self, ref gameTime);
        TryAdvanceReplayBootstrapTick();

        if (runningExtraUpdates || Main.gameMenu || TimeScale <= 1f)
            return;

        double extraUpdatesToRun = TimeScale - 1d;
        extraUpdateAccumulator += extraUpdatesToRun;

        int extraWholeUpdates = (int)extraUpdateAccumulator;
        if (extraWholeUpdates <= 0)
            return;

        extraUpdateAccumulator -= extraWholeUpdates;
        runningExtraUpdates = true;

        try
        {
            for (int i = 0; i < extraWholeUpdates; i++)
                orig(self, ref gameTime);
        }
        finally
        {
            runningExtraUpdates = false;
        }
    }

    private void TryAdvanceReplayBootstrapTick()
    {
        if (!ReplayPlayback.IsReplayPlayback)
        {
            ResetBootstrapTickAdvancement();
            return;
        }

        if (ReplayPlayback.HasEnteredReplayWorld)
        {
            ResetBootstrapTickAdvancement();
            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (Netplay.Connection?.Socket is not global::Reese.Common.Replayer.Replayer.ReplaySocket)
            return;

        if (!bootstrapTickAdvancementActive)
        {
            bootstrapTickAdvancementActive = true;
            bootstrapTicksAdvanced = 0;
            Log.Info($"Replay bootstrap tick advancement started at replay tick {ReplayPlayback.CurrentTick}.");
        }

        ModContent.GetInstance<global::Reese.Common.Replayer.Replayer>()?.AdvancePlaybackTick();
        bootstrapTicksAdvanced++;

        if (bootstrapTicksAdvanced % 60 == 0)
            Log.Info($"Replay bootstrap tick advancement has run for {bootstrapTicksAdvanced} ticks; current replay tick is {ReplayPlayback.CurrentTick}.");
    }

    private void ResetBootstrapTickAdvancement()
    {
        bootstrapTickAdvancementActive = false;
        bootstrapTicksAdvanced = 0;
    }

    private void HookDoUpdateInWorld(On_Main.orig_DoUpdateInWorld orig, Main self, Stopwatch sw)
    {
        if (Main.gameMenu || TimeScale >= 1f)
        {
            // Index Out Of Range sometimes here when scrubbing/seeking during a replay too fast...?
            orig(self, sw);
            NotifyWorldTickAdvanced();
            return;
        }

        if (stepOneFrameRequested)
        {
            orig(self, sw);
            NotifyWorldTickAdvanced();
            consumedWorldStep = true;
            TryCompleteStepRequest();
            return;
        }

        if (TimeScale <= 0f)
            return;

        worldUpdateAccumulator += TimeScale;

        if (worldUpdateAccumulator < 1d)
            return;

        worldUpdateAccumulator -= 1d;
        orig(self, sw);
        NotifyWorldTickAdvanced();
    }

    private static void NotifyWorldTickAdvanced()
    {
        if (Main.netMode != NetmodeID.Server && ReplayPlayback.IsReplayPlayback)
        {
            ModContent.GetInstance<global::Reese.Common.Replayer.Replayer>()?.AdvancePlaybackTick();
        }
    }

    private void HookUpdateTime(On_Main.orig_UpdateTime orig)
    {
        if (Main.gameMenu || TimeScale >= 1f)
        {
            orig();
            return;
        }

        if (stepOneFrameRequested)
        {
            orig();
            consumedTimeStep = true;
            TryCompleteStepRequest();
            return;
        }

        if (TimeScale <= 0f)
            return;

        timeUpdateAccumulator += TimeScale;

        if (timeUpdateAccumulator < 1d)
            return;

        timeUpdateAccumulator -= 1d;
        orig();
    }
}
