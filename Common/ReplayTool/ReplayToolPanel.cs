using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer;
using Reese.Common.TimeScaleTool;
using Reese.Core.Utilities;
using Reese.UI;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace Reese.Common.ReplayTool;

public sealed class ReplayToolPanel : DraggablePanel
{
    private static readonly float[] SpeedPresets = [0.25f, 0.5f, 1f, 2f, 4f];

    private readonly Slider positionSlider;
    private readonly UIText positionLabel;
    private readonly UIText speedLabel;
    private readonly HorizontalRule horizontalRule;
    private readonly VerticalRule verticalRule;
    private readonly CompactTextPanel<string>[] speedButtons;
    private readonly IconActionButton speedDownButton;
    private readonly IconActionButton playButton;
    private readonly IconActionButton pauseButton;
    private readonly IconActionButton stopButton;
    private readonly IconActionButton nextFrameButton;
    private readonly IconActionButton speedUpButton;

    protected override bool HasResizeButton() => false;

    protected override void OnClosePanelLeftClick()
    {
        Remove();
    }

    protected override void OnRefreshPanelLeftClick()
    {
        ReplayToolPanelLayout.Update();
        ApplyLayout();
        Recalculate();
        RefreshVisualState();
    }

    public ReplayToolPanel() : base("Replay")
    {
        ReplayToolPanelLayout.Update();

        Width.Set(ReplayToolPanelLayout.PanelWidth, 0f);
        Height.Set(ReplayToolPanelLayout.PanelHeight, 0f);
        HAlign = ReplayToolPanelLayout.InitialHAlign;
        VAlign = ReplayToolPanelLayout.InitialVAlign;

        positionLabel = new UIText("", 0.86f)
        {
            TextOriginX = 0f,
            TextOriginY = 0f,
            TextColor = Color.White
        };
        ContentPanel.Append(positionLabel);

        positionSlider = new Slider
        {
        };
        positionSlider.OnDrag += ratio =>
        {
            uint duration = GetDurationTicks();
            Replayer.Replayer.SeekToTick((uint)Math.Round(ratio * duration));
        };
        ContentPanel.Append(positionSlider);

        horizontalRule = new HorizontalRule();
        ContentPanel.Append(horizontalRule);

        speedLabel = new UIText("", 0.86f)
        {
            TextOriginX = 0f,
            TextOriginY = 0f,
            TextColor = Color.White
        };
        ContentPanel.Append(speedLabel);

        speedButtons = new CompactTextPanel<string>[SpeedPresets.Length];
        for (int i = 0; i < SpeedPresets.Length; i++)
        {
            float speed = SpeedPresets[i];
            var button = new CompactTextPanel<string>(
                FormatSpeedButton(speed),
                0.78f,
                false,
                leftClick: () => SetSpeed(speed),
                backgroundColor: new Color(44, 57, 105),
                padding: 3f)
            {
            };
            speedButtons[i] = button;
            ContentPanel.Append(button);
        }

        verticalRule = new VerticalRule();
        ContentPanel.Append(verticalRule);

        speedDownButton = CreateTransportButton(Ass.Icon_SpeedDown, "Go to start", GoToStart, 0);
        nextFrameButton = CreateTransportButton(Ass.Icon_NextFrame, "Next Frame", StepOneFrame, 1);
        playButton = CreateTransportButton(Ass.Icon_Play, "Play", Resume, 2, large: true);
        pauseButton = CreateTransportButton(Ass.Icon_Pause, "Pause", Pause, 3);
        stopButton = CreateTransportButton(Ass.Icon_Stop, "Stop Replay", () => Replayer.Replayer.StopPlayback(), 4);
        speedUpButton = CreateTransportButton(Ass.Icon_SpeedUp, "Go to end", GoToEnd, 5);

        ApplyLayout();
        RefreshVisualState();
    }

    public override void Recalculate()
    {
        ApplyLayout();
        base.Recalculate();
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        ReplayToolPanelLayout.Update();

        Width.Set(ReplayToolPanelLayout.PanelWidth, 0f);
        Height.Set(ReplayToolPanelLayout.PanelHeight, 0f);

        if (TitlePanel != null)
            TitlePanel.Height.Set(ReplayToolPanelLayout.HeaderHeight, 0f);

        if (ContentPanel != null)
        {
            ContentPanel.Top.Set(ReplayToolPanelLayout.HeaderHeight, 0f);
            ContentPanel.Height.Set(ReplayToolPanelLayout.ContentHeight, 0f);
        }

        if (positionLabel != null)
        {
            positionLabel.Left.Set(ReplayToolPanelLayout.PositionLabelLeft, 0f);
            positionLabel.Top.Set(ReplayToolPanelLayout.PositionLabelTop, 0f);
        }

        if (positionSlider != null)
        {
            positionSlider.Left.Set(ReplayToolPanelLayout.PositionSliderLeft, 0f);
            positionSlider.Top.Set(ReplayToolPanelLayout.PositionSliderTop, 0f);
            positionSlider.Width.Set(-(ReplayToolPanelLayout.PositionSliderLeft + ReplayToolPanelLayout.PositionSliderRightPadding), 1f);
            positionSlider.Height.Set(ReplayToolPanelLayout.PositionSliderHeight, 0f);
        }

        if (horizontalRule != null)
        {
            horizontalRule.Left.Set(ReplayToolPanelLayout.HorizontalRuleLeft, 0f);
            horizontalRule.Top.Set(ReplayToolPanelLayout.HorizontalRuleTop, 0f);
            horizontalRule.Width.Set(-(ReplayToolPanelLayout.HorizontalRuleLeft + ReplayToolPanelLayout.HorizontalRuleRightPadding), 1f);
            horizontalRule.Height.Set(ReplayToolPanelLayout.HorizontalRuleHeight, 0f);
        }

        if (speedLabel != null)
        {
            speedLabel.Left.Set(ReplayToolPanelLayout.SpeedLabelLeft, 0f);
            speedLabel.Top.Set(ReplayToolPanelLayout.SpeedLabelTop, 0f);
        }

        if (speedButtons != null)
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                CompactTextPanel<string> button = speedButtons[i];
                if (button == null)
                    continue;

                button.Left.Set(ReplayToolPanelLayout.SpeedButtonLeft + i * ReplayToolPanelLayout.SpeedButtonStride, 0f);
                button.Top.Set(ReplayToolPanelLayout.SpeedButtonTop, 0f);
                button.Width.Set(ReplayToolPanelLayout.SpeedButtonWidth, 0f);
                button.Height.Set(ReplayToolPanelLayout.SpeedButtonHeight, 0f);
            }
        }

        if (verticalRule != null)
        {
            verticalRule.Left.Set(ReplayToolPanelLayout.DividerLeft, 0f);
            verticalRule.Top.Set(ReplayToolPanelLayout.DividerTop, 0f);
            verticalRule.Width.Set(ReplayToolPanelLayout.DividerWidth, 0f);
            verticalRule.Height.Set(ReplayToolPanelLayout.DividerHeight, 0f);
        }

        ApplyTransportButtonLayout(speedDownButton, 0);
        ApplyTransportButtonLayout(nextFrameButton, 1);
        ApplyTransportButtonLayout(playButton, 2, large: true);
        ApplyTransportButtonLayout(pauseButton, 3);
        ApplyTransportButtonLayout(stopButton, 4);
        ApplyTransportButtonLayout(speedUpButton, 5);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (ReplayToolPanelLayout.Update())
        {
            ApplyLayout();
            Recalculate();
        }

        RefreshVisualState();
    }

    private IconActionButton CreateTransportButton(ReLogic.Content.Asset<Texture2D> texture, string hoverText, Action onClick, int index, bool large = false)
    {
        IconActionButton button = new(texture, hoverText, (_, _) => onClick());
        ApplyTransportButtonLayout(button, index, large);
        ContentPanel.Append(button);
        return button;
    }

    private static void ApplyTransportButtonLayout(IconActionButton button, int index, bool large = false)
    {
        if (button == null)
            return;

        float left = ReplayToolPanelLayout.TransportLeft;

        for (int i = 0; i < index; i++)
            left += GetTransportButtonWidth(i == 2) + ReplayToolPanelLayout.TransportGap;

        button.SetOuterWidth(GetTransportButtonWidth(large));
        button.Left.Set(left, 0f);
        button.Top.Set(ReplayToolPanelLayout.TransportTop, 0f);
    }

    private static float GetTransportButtonWidth(bool large)
    {
        return large ? ReplayToolPanelLayout.TransportPlayButtonWidth : ReplayToolPanelLayout.TransportButtonWidth;
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

    private void GoToStart()
    {
        Replayer.Replayer.SeekToStart();
        RefreshVisualState();
    }

    private void GoToEnd()
    {
        Replayer.Replayer.SeekToEnd();
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        uint durationTicks = GetDurationTicks();
        uint currentTick = Math.Min(Replayer.Replayer.CurrentTick, durationTicks);
        float speed = ModContent.GetInstance<TimeScaleSystem>().TimeScale;
        bool paused = speed <= 0f;

        positionSlider.SetRatio(durationTicks == 0 ? 0f : currentTick / (float)durationTicks);
        positionLabel.SetText($"Position: {FormatTicks(currentTick)} / {FormatTicks(durationTicks)}");
        speedLabel.SetText($"Speed: {speed:0.###}x");

        for (int i = 0; i < speedButtons.Length; i++)
        {
            bool selected = Math.Abs(speed - SpeedPresets[i]) < 0.001f;
            speedButtons[i].BackgroundColor = selected ? new Color(73, 94, 171) : new Color(44, 57, 105);
            speedButtons[i].BorderColor = selected ? Color.Yellow : Color.Black;
        }

        speedDownButton.SetSelected(false);
        nextFrameButton.SetSelected(false);
        playButton.SetSelected(!paused);
        pauseButton.SetSelected(paused);
        stopButton.SetSelected(false);
        speedUpButton.SetSelected(false);
    }

    private static uint GetDurationTicks()
    {
        return Math.Max(1u, Replayer.Replayer.ActiveDurationTicks);
    }

    private static string FormatSpeedButton(float speed)
    {
        return speed == 1f ? "1x" : $"{speed:0.##}x";
    }

    private static string FormatTicks(uint ticks)
    {
        var span = TimeSpan.FromSeconds(ticks / 60d);
        return span.TotalHours >= 1d
            ? $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes:00}:{span.Seconds:00}";
    }

    private sealed class HorizontalRule : UIElement
    {
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, GetDimensions().ToRectangle(), Color.White * 0.18f);
        }
    }

    private sealed class VerticalRule : UIElement
    {
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, GetDimensions().ToRectangle(), Color.White * 0.18f);
        }
    }
}
