using Microsoft.Xna.Framework;
using Reese.Common.UI;
using Reese.Core;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace Reese.Common.TimeScaleTool;

public sealed class TimeScalePanel : DraggablePanel
{
    #region Class fields
    private readonly SliderElement timeScaleSlider;
    private readonly UIHorizontalSeparator divider;

    private readonly UIElement topControlsRow;
    private readonly UIElement topLeftColumn;
    private readonly UIElement topRightColumn;
    private readonly UIElement playbackButtonRow;

    private readonly IconActionButton playButton;
    private readonly IconActionButton pauseButton;
    private readonly IconActionButton nextFrameButton;
    private readonly IconActionButton stopButton;
    private readonly IconActionButton speedDownButton;
    private readonly IconActionButton speedUpButton;

    private readonly UIElement columnsRoot;
    private readonly UIElement leftColumn;
    private readonly UIElement rightColumn;

    private readonly CompactTextPanel<string> frameSkipButton;
    private readonly CompactTextPanel<string> showHideElapsedButton;
    private readonly CompactTextPanel<string> showHideGameUpdateCountButton;
    private readonly CompactTextPanel<string> infoPanel;

    private bool showElapsed = true;
    private bool showGameUpdateCount = true;

    protected override void OnClosePanelLeftClick()
    {
        Remove();
    }

    protected override void OnRefreshPanelLeftClick()
    {
        Width.Set(380f, 0f);
        Height.Set(290f, 0f);
        Recalculate();
        RefreshVisualState();
    }

    protected override float MinResizeHeight => 145f;
    protected override float MaxResizeHeight => 350f;
    protected override float MinResizeWidth => 360f;
    protected override float MaxResizeWidth => 540f;
    #endregion

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        RefreshVisualState();
    }

    public TimeScalePanel() : base("Timescale")
    {
        Log.Debug("Building TimeScalePanel.");

        Width.Set(380f, 0f);
        Height.Set(290f, 0f);
        HAlign = 0.85f;
        VAlign = 0.65f;

        // Add slider, keep this comment!
        timeScaleSlider = new SliderElement(
            label: "Timescale",
            min: 0f,
            max: 8f,
            defaultValue: ModContent.GetInstance<TimeScaleSystem>().TimeScale,
            onValueChanged: value =>
            {
                ModContent.GetInstance<TimeScaleSystem>().SetTimeScale(value);
                RefreshVisualState();
            },
            snapValues: TimeScaleSystem.SnapValues
        )
        {
            Left = { Pixels = 10f },
            Top = { Pixels = 10f },
            Width = { Percent = 1f, Pixels = -20f },
            Height = { Pixels = 42f }
        };
        ContentPanel.Append(timeScaleSlider);

        // Add top controls, keep this comment!
        topControlsRow = new UIElement
        {
            Left = { Pixels = 10f },
            Top = { Pixels = 56f },
            Width = { Percent = 1f, Pixels = -20f },
            Height = { Pixels = 35f }
        };
        ContentPanel.Append(topControlsRow);

        topLeftColumn = new UIElement
        {
            Width = { Percent = 0.5f, Pixels = -4f },
            Height = { Percent = 1f, Pixels = 0f }
        };
        topControlsRow.Append(topLeftColumn);

        topRightColumn = new UIElement
        {
            Left = { Percent = 0.5f, Pixels = 4f },
            Width = { Percent = 0.5f, Pixels = -4f },
            Height = { Percent = 1f, Pixels = 0f }
        };
        topControlsRow.Append(topRightColumn);

        // Add playback buttons on the left side, keep this comment!
        playbackButtonRow = new UIElement
        {
            Width = { Percent = 1f, Pixels = 0f },
            Height = { Pixels = 35f }
        };
        topLeftColumn.Append(playbackButtonRow);

        playButton = new IconActionButton(Ass.Icon_Play, "Play", (_, _) => Resume());
        pauseButton = new IconActionButton(Ass.Icon_Pause, "Pause", (_, _) => Pause());
        nextFrameButton = new IconActionButton(Ass.Icon_NextFrame, "Next Frame", (_, _) => StepOneFrame());
        stopButton = new IconActionButton(Ass.Icon_Stop, "Stop", (_, _) => Pause());

        playButton.SetOuterWidth(32f);
        pauseButton.SetOuterWidth(32f);
        nextFrameButton.SetOuterWidth(32f);
        stopButton.SetOuterWidth(32f);

        AppendPlaybackButton(playButton, 0);
        AppendPlaybackButton(pauseButton, 1);
        AppendPlaybackButton(nextFrameButton, 2);
        AppendPlaybackButton(stopButton, 3);

        // Add speed controls on the right side, keep this comment!
        speedDownButton = new IconActionButton(Ass.Icon_SpeedDown, "Speed Down", (_, _) => StepTimeScaleDown());
        speedUpButton = new IconActionButton(Ass.Icon_SpeedUp, "Speed Up", (_, _) => StepTimeScaleUp());
        
        speedDownButton.SetOuterWidth(64f);
        speedUpButton.SetOuterWidth(64f);

        AppendTopRightButton(speedDownButton, 0);
        AppendTopRightButton(speedUpButton, 1);

        // Add divider, keep this comment!
        divider = new UIHorizontalSeparator
        {
            Left = { Pixels = 10f },
            Top = { Pixels = 108f },
            Width = { Percent = 1f, Pixels = -20f },
            Height = { Pixels = 1f }
        };
        ContentPanel.Append(divider);

        // Add main content columns, keep this comment!
        columnsRoot = new UIElement
        {
            Left = { Pixels = 10f },
            Top = { Pixels = 124f },
            Width = { Percent = 1f, Pixels = -20f },
            Height = { Percent = 1f, Pixels = -135f }
        };
        ContentPanel.Append(columnsRoot);

        // Add left column, keep this comment!
        leftColumn = new UIElement
        {
            Width = { Percent = 0.5f, Pixels = -4f },
            Height = { Percent = 1f, Pixels = 0f }
        };
        columnsRoot.Append(leftColumn);

        // Add right column, keep this comment!
        rightColumn = new UIElement
        {
            Left = { Percent = 0.5f, Pixels = 4f },
            Width = { Percent = 0.5f, Pixels = -4f },
            Height = { Percent = 1f, Pixels = 0f }
        };
        columnsRoot.Append(rightColumn);

        // Add left-side buttons, keep this comment!
        frameSkipButton = new CompactTextPanel<string>("", 0.8f, false, leftClick: () => CycleFrameSkip(1), rightClick: () => CycleFrameSkip(-1))
            .WithFullWidth(35f, 0f);
        leftColumn.Append(frameSkipButton);

        showHideElapsedButton = new CompactTextPanel<string>("", 0.8f, false, leftClick: ToggleShowElapsed)
            .WithFullWidth(35f, 39f);
        leftColumn.Append(showHideElapsedButton);

        showHideGameUpdateCountButton = new CompactTextPanel<string>("", 0.8f, false, leftClick: ToggleShowGameUpdateCount)
            .WithFullWidth(35f, 78f);
        leftColumn.Append(showHideGameUpdateCountButton);

        // Add info panel, keep this comment!
        infoPanel = new CompactTextPanel<string>(
            "",
            0.8f,
            false,
            highlightOnHover: false,
            textHAlign: 0f,
            textVAlign: 0f,
            textOffset: new Vector2(4f, 3f))
        {
            Top = { Pixels = 0f },
            Width = { Percent = 1f, Pixels = 0f }
        };
        rightColumn.Append(infoPanel);

        RefreshVisualState();
    }

    private void AppendTopRightButton(IconActionButton button, int index)
    {
        const float gap = 12f;
        float width = button.GetOuterWidth();
        float centerOffset = (index - 0.5f) * (width + gap);

        button.HAlign = 0.5f;
        button.Left.Set(centerOffset, 0f);
        button.Top.Set(7f, 0f);

        topRightColumn.Append(button);
    }

    private void AppendPlaybackButton(IconActionButton button, int index)
    {
        const float gap = 12f;
        float width = button.GetOuterWidth();
        float centerOffset = (index - 1.5f) * (width + gap);

        button.HAlign = 0.5f;
        button.Left.Set(centerOffset, 0f);
        button.Top.Set(7f, 0f);

        playbackButtonRow.Append(button);
    }

    private void ToggleShowElapsed()
    {
        showElapsed = !showElapsed;
        RefreshVisualState();
    }

    private void ToggleShowGameUpdateCount()
    {
        showGameUpdateCount = !showGameUpdateCount;
        RefreshVisualState();
    }

    private void SetTimeScale(float value)
    {
        TimeScaleSystem system = ModContent.GetInstance<TimeScaleSystem>();
        system.SetTimeScale(value);
        timeScaleSlider.SetValue(system.TimeScale);
        RefreshVisualState();
    }

    private void Resume()
    {
        SetTimeScale(1f);
    }

    private void Pause()
    {
        SetTimeScale(0f);
    }

    private void StepOneFrame()
    {
        ModContent.GetInstance<TimeScaleSystem>().StepOneFrame();
        RefreshVisualState();
    }

    private void StepTimeScaleUp()
    {
        float[] values = TimeScaleSystem.SnapValues;
        float current = ModContent.GetInstance<TimeScaleSystem>().TimeScale;

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] > current)
            {
                SetTimeScale(values[i]);
                return;
            }
        }

        SetTimeScale(values[^1]);
    }

    private void StepTimeScaleDown()
    {
        float[] values = TimeScaleSystem.SnapValues;
        float current = ModContent.GetInstance<TimeScaleSystem>().TimeScale;

        for (int i = values.Length - 1; i >= 0; i--)
        {
            if (values[i] < current)
            {
                SetTimeScale(values[i]);
                return;
            }
        }

        SetTimeScale(values[0]);
    }

    private void CycleFrameSkip(int direction)
    {
        FrameSkipMode[] values = [FrameSkipMode.Off, FrameSkipMode.On, FrameSkipMode.Subtle];
        int currentIndex = Array.IndexOf(values, Main.FrameSkipMode);

        if (currentIndex < 0)
            currentIndex = 0;

        int nextIndex = (currentIndex + direction + values.Length) % values.Length;
        Main.FrameSkipMode = values[nextIndex];

        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        TimeScaleSystem system = ModContent.GetInstance<TimeScaleSystem>();
        LocalWorldSessionSystem session = ModContent.GetInstance<LocalWorldSessionSystem>();

        float scale = system.TimeScale;
        bool paused = scale <= 0f;

        timeScaleSlider.SetValue(scale);

        playButton.SetSelected(false);
        pauseButton.SetSelected(false);
        nextFrameButton.SetSelected(false);
        stopButton.SetSelected(false);

        frameSkipButton.SetText($"Frame Skip: {Main.FrameSkipMode}");
        showHideElapsedButton.SetText(showElapsed ? "Hide Elapsed" : "Show Elapsed");
        showHideGameUpdateCountButton.SetText(showGameUpdateCount ? "Hide GameUpdateCount" : "Show GameUpdateCount");

        string elapsedText = LocalWorldSessionSystem.FormatElapsedPrecise(session.TimeInWorld);
        string gameUpdateCount = Main.GameUpdateCount.ToString();

        string infoText = $"Speed: {scale:0.###}x\nPaused: {paused}";
        int rowCount = 2;

        if (showElapsed)
        {
            infoText += $"\nElapsed: {elapsedText}";
            rowCount++;
        }

        if (showGameUpdateCount)
        {
            infoText += $"\nGameUpdateCount: {gameUpdateCount}";
            rowCount++;
        }

        UpdateInfoPanelHeight(rowCount);
        infoPanel.SetText(infoText);
        UpdateCompactPanelVisibility();
    }

    private void UpdateInfoPanelHeight(int rowCount)
    {
        float rowHeight = 28 * infoPanel.TextScale;
        float verticalPadding = infoPanel.PaddingTop + infoPanel.PaddingBottom;
        float lineSpacing = 2f * (rowCount - 1);

        infoPanel.Height.Set(rowHeight * rowCount + verticalPadding + lineSpacing, 0f);
    }

    private void UpdateCompactPanelVisibility()
    {
        Rectangle contentRect = ContentPanel.GetDimensions().ToRectangle();
        contentRect.Y += 4;
        contentRect.Height -= 16;

        UpdateCompactPanelVisibility(frameSkipButton, contentRect);
        UpdateCompactPanelVisibility(showHideElapsedButton, contentRect);
        UpdateCompactPanelVisibility(showHideGameUpdateCountButton, contentRect);
        UpdateCompactPanelVisibility(infoPanel, contentRect);
    }

    private void UpdateCompactPanelVisibility(CompactTextPanel<string> panel, Rectangle visibleRect)
    {
        Rectangle rect = panel.GetDimensions().ToRectangle();
        bool fullyVisible = rect.Top >= visibleRect.Top && rect.Bottom <= visibleRect.Bottom;

        panel.DrawPanel = fullyVisible;
        panel.IgnoresMouseInteraction = !fullyVisible;
        panel.TextColor = fullyVisible ? Color.White : Color.Transparent;
    }
}