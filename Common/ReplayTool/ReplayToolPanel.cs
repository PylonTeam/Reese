using Microsoft.Xna.Framework;
using Reese.Common.TimeScaleTool;
using Reese.Core.Utilities;
using Reese.UI;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Reese.Common.ReplayTool;

public sealed class ReplayToolPanel : DraggablePanel
{
    private readonly SliderElement positionSlider;
    private readonly SliderElement speedSlider;
    private readonly UIElement controlsRow;
    private readonly UIElement playbackButtonRow;
    private readonly UIElement speedButtonRow;
    private readonly IconActionButton playButton;
    private readonly IconActionButton pauseButton;
    private readonly IconActionButton nextFrameButton;
    private readonly IconActionButton stopButton;
    private readonly IconActionButton speedDownButton;
    private readonly IconActionButton speedUpButton;
    private readonly CompactTextPanel<string> infoPanel;

    private readonly uint initialDurationTicks;

    protected override float MinResizeHeight => 210f;
    protected override float MaxResizeHeight => 320f;
    protected override float MinResizeWidth => 390f;
    protected override float MaxResizeWidth => 560f;

    protected override void OnClosePanelLeftClick()
    {
        Remove();
    }

    protected override void OnRefreshPanelLeftClick()
    {
        Width.Set(420f, 0f);
        Height.Set(260f, 0f);
        Recalculate();
        RefreshVisualState();
    }

    public ReplayToolPanel() : base("Replay")
    {
        Width.Set(420f, 0f);
        Height.Set(260f, 0f);
        HAlign = 0.82f;
        VAlign = 0.62f;

        initialDurationTicks = Math.Max(1u, Replayer.ActiveDurationTicks);

        positionSlider = new SliderElement(
            label: "Position",
            min: 0f,
            max: initialDurationTicks,
            defaultValue: Replayer.CurrentTick,
            step: 1f,
            onValueChanged: value => Replayer.SeekToTick((uint)Math.Round(value)),
            labelFormatter: value => $"Position: {FormatTicks((uint)Math.Round(value))} / {FormatTicks(Replayer.ActiveDurationTicks)}")
        {
            Left = { Pixels = 10f },
            Top = { Pixels = 10f },
            Width = { Percent = 1f, Pixels = -20f },
            Height = { Pixels = 42f }
        };
        ContentPanel.Append(positionSlider);

        speedSlider = new SliderElement(
            label: "Speed",
            min: 0f,
            max: 8f,
            defaultValue: ModContent.GetInstance<TimeScaleSystem>().TimeScale,
            onValueChanged: SetSpeed,
            snapValues: TimeScaleSystem.SnapValues,
            labelFormatter: value => $"Speed: {value:0.###}x")
        {
            Left = { Pixels = 10f },
            Top = { Pixels = 56f },
            Width = { Percent = 1f, Pixels = -20f },
            Height = { Pixels = 42f }
        };
        ContentPanel.Append(speedSlider);

        controlsRow = new UIElement
        {
            Left = { Pixels = 10f },
            Top = { Pixels = 104f },
            Width = { Percent = 1f, Pixels = -20f },
            Height = { Pixels = 35f }
        };
        ContentPanel.Append(controlsRow);

        playbackButtonRow = new UIElement
        {
            Width = { Percent = 0.55f, Pixels = -4f },
            Height = { Pixels = 35f }
        };
        controlsRow.Append(playbackButtonRow);

        speedButtonRow = new UIElement
        {
            Left = { Percent = 0.55f, Pixels = 4f },
            Width = { Percent = 0.45f, Pixels = -4f },
            Height = { Pixels = 35f }
        };
        controlsRow.Append(speedButtonRow);

        playButton = new IconActionButton(Ass.Icon_Play, "Play", (_, _) => Resume());
        pauseButton = new IconActionButton(Ass.Icon_Pause, "Pause", (_, _) => Pause());
        nextFrameButton = new IconActionButton(Ass.Icon_NextFrame, "Next Frame", (_, _) => StepOneFrame());
        stopButton = new IconActionButton(Ass.Icon_Stop, "Stop Replay", (_, _) => Replayer.StopPlayback());

        playButton.SetOuterWidth(32f);
        pauseButton.SetOuterWidth(32f);
        nextFrameButton.SetOuterWidth(32f);
        stopButton.SetOuterWidth(32f);

        AppendPlaybackButton(playButton, 0);
        AppendPlaybackButton(pauseButton, 1);
        AppendPlaybackButton(nextFrameButton, 2);
        AppendPlaybackButton(stopButton, 3);

        speedDownButton = new IconActionButton(Ass.Icon_SpeedDown, "Speed Down", (_, _) => StepSpeedDown());
        speedUpButton = new IconActionButton(Ass.Icon_SpeedUp, "Speed Up", (_, _) => StepSpeedUp());

        speedDownButton.SetOuterWidth(64f);
        speedUpButton.SetOuterWidth(64f);

        AppendSpeedButton(speedDownButton, 0);
        AppendSpeedButton(speedUpButton, 1);

        infoPanel = new CompactTextPanel<string>(
            "",
            0.8f,
            false,
            highlightOnHover: false,
            textHAlign: 0f,
            textVAlign: 0f,
            textOffset: new Vector2(4f, 3f))
        {
            Left = { Pixels = 10f },
            Top = { Pixels = 150f },
            Width = { Percent = 1f, Pixels = -20f },
            Height = { Pixels = 72f }
        };
        ContentPanel.Append(infoPanel);

        RefreshVisualState();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        RefreshVisualState();
    }

    private void AppendPlaybackButton(IconActionButton button, int index)
    {
        const float gap = 10f;
        float width = button.GetOuterWidth();
        float centerOffset = (index - 1.5f) * (width + gap);

        button.HAlign = 0.5f;
        button.Left.Set(centerOffset, 0f);
        button.Top.Set(7f, 0f);

        playbackButtonRow.Append(button);
    }

    private void AppendSpeedButton(IconActionButton button, int index)
    {
        const float gap = 12f;
        float width = button.GetOuterWidth();
        float centerOffset = (index - 0.5f) * (width + gap);

        button.HAlign = 0.5f;
        button.Left.Set(centerOffset, 0f);
        button.Top.Set(7f, 0f);

        speedButtonRow.Append(button);
    }

    private static void SetSpeed(float value)
    {
        ModContent.GetInstance<TimeScaleSystem>().SetTimeScale(value);
    }

    private void Resume()
    {
        SetSpeed(1f);
        RefreshVisualState();
    }

    private void Pause()
    {
        SetSpeed(0f);
        RefreshVisualState();
    }

    private void StepOneFrame()
    {
        SetSpeed(0f);
        ModContent.GetInstance<TimeScaleSystem>().StepOneFrame();
        RefreshVisualState();
    }

    private void StepSpeedUp()
    {
        StepSpeed(1);
    }

    private void StepSpeedDown()
    {
        StepSpeed(-1);
    }

    private void StepSpeed(int direction)
    {
        float[] values = TimeScaleSystem.SnapValues;
        float current = ModContent.GetInstance<TimeScaleSystem>().TimeScale;
        int currentIndex = Array.IndexOf(values, TimeScaleSystem.SnapTimeScale(current));

        if (currentIndex < 0)
            currentIndex = Array.IndexOf(values, 1f);

        int nextIndex = Utils.Clamp(currentIndex + direction, 0, values.Length - 1);
        SetSpeed(values[nextIndex]);
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        uint durationTicks = Replayer.ActiveDurationTicks;
        uint currentTick = Replayer.CurrentTick;
        float speed = ModContent.GetInstance<TimeScaleSystem>().TimeScale;
        bool paused = speed <= 0f;

        positionSlider.SetValue(Math.Min(currentTick, initialDurationTicks));
        speedSlider.SetValue(speed);

        playButton.SetSelected(!paused);
        pauseButton.SetSelected(paused);
        nextFrameButton.SetSelected(false);
        stopButton.SetSelected(false);

        string replayName = string.IsNullOrWhiteSpace(ReplaySession.CurrentPath)
            ? "?"
            : System.IO.Path.GetFileNameWithoutExtension(ReplaySession.CurrentPath);
        string worldName = string.IsNullOrWhiteSpace(Replayer.ActiveMetadata?.WorldName)
            ? "?"
            : Replayer.ActiveMetadata.WorldName;
        string playerName = string.IsNullOrWhiteSpace(Replayer.ActiveMetadata?.PlayerName)
            ? "?"
            : Replayer.ActiveMetadata.PlayerName;
        string state = ReplaySession.IsReplayPlayback
            ? paused ? "Paused" : "Playing"
            : "Ended";

        infoPanel.SetText(
            $"State: {state}\n" +
            $"World: {worldName}  Player: {playerName}\n" +
            $"Time: {FormatTicks(currentTick)} / {FormatTicks(durationTicks)}  Speed: {speed:0.###}x\n" +
            $"File: {replayName}");
    }

    private static string FormatTicks(uint ticks)
    {
        var span = TimeSpan.FromSeconds(ticks / 60d);
        return span.TotalHours >= 1d
            ? $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes:00}:{span.Seconds:00}";
    }
}
