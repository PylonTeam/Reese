using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer;
using Reese.Common.Replayer.ReplaySpectate.ReplayControls.TimeScale;
using Reese.Core.Configs;
using Reese.Core.Utilities;
using Reese.UI;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplaySpectate.ReplayControls;

public sealed class ReplayControlsPanel : DraggablePanel
{
    private static readonly float[] SpeedPresets = [0.25f, 0.5f, 1f, 2f, 4f];

    private readonly Slider positionSlider;
    private readonly UIText positionLabel;
    private readonly UIText speedLabel;
    private readonly HorizontalRule horizontalRule;
    private readonly VerticalRule verticalRule;
    private readonly CompactTextPanel<string>[] speedButtons;
    private readonly UIText transportStatusLabel;
    private readonly IconActionButton goToStartButton;
    private readonly IconActionButton playButton;
    private readonly IconActionButton pauseButton;
    private readonly IconActionButton stopButton;
    private readonly IconActionButton nextFrameButton;
    private readonly IconActionButton goToEndButton;

    protected override bool HasResizeButton() => false;

    protected override void OnClosePanelLeftClick()
    {
        Remove();
    }

    protected override void OnRefreshPanelLeftClick()
    {
        ReplayControlsPanelLayout.Update();
        ApplyLayout();
        Recalculate();
        RefreshVisualState();
    }

    public ReplayControlsPanel() : base("Replay")
    {
        ReplayControlsPanelLayout.Update();

        Width.Set(ReplayControlsPanelLayout.PanelWidth, 0f);
        Height.Set(ReplayControlsPanelLayout.PanelHeight, 0f);
        HAlign = ReplayControlsPanelLayout.InitialHAlign;
        VAlign = ReplayControlsPanelLayout.InitialVAlign;

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
            if (!ModContent.GetInstance<ClientConfig>().IsSeekbarEnabled)
                return;

            uint duration = GetDurationTicks();
            Replayer.SeekToTick((uint)Math.Round(ratio * duration));
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

        transportStatusLabel = new UIText("", 0.86f)
        {
            TextOriginX = 0f,
            TextOriginY = 0f,
            TextColor = Color.White
        };
        ContentPanel.Append(transportStatusLabel);

        goToStartButton = CreateTransportButton(Ass.Icon_SpeedDown, "Go to start", GoToStart, 0);
        nextFrameButton = CreateTransportButton(Ass.Icon_NextFrame, "Next Frame", StepOneFrame, 1);
        playButton = CreateTransportButton(Ass.Icon_Play, "Play", Resume, 2, large: true);
        pauseButton = CreateTransportButton(Ass.Icon_Pause, "Pause", Pause, 3);
        stopButton = CreateTransportButton(Ass.Icon_Stop, "Stop Replay", () => Replayer.StopPlayback(), 4);
        goToEndButton = CreateTransportButton(Ass.Icon_SpeedUp, "Go to end", GoToEnd, 5);

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
        ReplayControlsPanelLayout.Update();

        Width.Set(ReplayControlsPanelLayout.PanelWidth, 0f);
        Height.Set(ReplayControlsPanelLayout.PanelHeight, 0f);

        if (TitlePanel != null)
            TitlePanel.Height.Set(ReplayControlsPanelLayout.HeaderHeight, 0f);

        if (ContentPanel != null)
        {
            ContentPanel.Top.Set(ReplayControlsPanelLayout.HeaderHeight, 0f);
            ContentPanel.Height.Set(ReplayControlsPanelLayout.ContentHeight, 0f);
        }

        if (positionLabel != null)
        {
            positionLabel.Left.Set(ReplayControlsPanelLayout.PositionLabelLeft, 0f);
            positionLabel.Top.Set(ReplayControlsPanelLayout.PositionLabelTop, 0f);
        }

        if (positionSlider != null)
        {
            positionSlider.Left.Set(ReplayControlsPanelLayout.PositionSliderLeft, 0f);
            positionSlider.Top.Set(ReplayControlsPanelLayout.PositionSliderTop, 0f);
            positionSlider.Width.Set(-(ReplayControlsPanelLayout.PositionSliderLeft + ReplayControlsPanelLayout.PositionSliderRightPadding), 1f);
            positionSlider.Height.Set(ReplayControlsPanelLayout.PositionSliderHeight, 0f);
        }

        if (horizontalRule != null)
        {
            horizontalRule.Left.Set(ReplayControlsPanelLayout.HorizontalRuleLeft, 0f);
            horizontalRule.Top.Set(ReplayControlsPanelLayout.HorizontalRuleTop, 0f);
            horizontalRule.Width.Set(-(ReplayControlsPanelLayout.HorizontalRuleLeft + ReplayControlsPanelLayout.HorizontalRuleRightPadding), 1f);
            horizontalRule.Height.Set(ReplayControlsPanelLayout.HorizontalRuleHeight, 0f);
        }

        if (speedLabel != null)
        {
            speedLabel.Left.Set(ReplayControlsPanelLayout.SpeedLabelLeft, 0f);
            speedLabel.Top.Set(ReplayControlsPanelLayout.SpeedLabelTop, 0f);
        }

        if (speedButtons != null)
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                CompactTextPanel<string> button = speedButtons[i];
                if (button == null)
                    continue;

                button.Left.Set(ReplayControlsPanelLayout.SpeedButtonLeft + i * ReplayControlsPanelLayout.SpeedButtonStride, 0f);
                button.Top.Set(ReplayControlsPanelLayout.SpeedButtonTop, 0f);
                button.Width.Set(ReplayControlsPanelLayout.SpeedButtonWidth, 0f);
                button.Height.Set(ReplayControlsPanelLayout.SpeedButtonHeight, 0f);
            }
        }

        if (verticalRule != null)
        {
            verticalRule.Left.Set(ReplayControlsPanelLayout.DividerLeft, 0f);
            verticalRule.Top.Set(ReplayControlsPanelLayout.DividerTop, 0f);
            verticalRule.Width.Set(ReplayControlsPanelLayout.DividerWidth, 0f);
            verticalRule.Height.Set(ReplayControlsPanelLayout.DividerHeight, 0f);
        }

        if (transportStatusLabel != null)
        {
            transportStatusLabel.Left.Set(ReplayControlsPanelLayout.TransportStatusLeft, 0f);
            transportStatusLabel.Top.Set(ReplayControlsPanelLayout.TransportStatusTop, 0f);
        }

        ApplyTransportButtonLayout(goToStartButton, 0);
        ApplyTransportButtonLayout(nextFrameButton, 1);
        ApplyTransportButtonLayout(playButton, 2, large: true);
        ApplyTransportButtonLayout(pauseButton, 3);
        ApplyTransportButtonLayout(stopButton, 4);
        ApplyTransportButtonLayout(goToEndButton, 5);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (ReplayControlsPanelLayout.Update())
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

        float left = ReplayControlsPanelLayout.TransportLeft;

        for (int i = 0; i < index; i++)
            left += GetTransportButtonWidth(i == 2) + ReplayControlsPanelLayout.TransportGap;

        button.SetOuterWidth(GetTransportButtonWidth(large));
        button.Left.Set(left, 0f);
        button.Top.Set(ReplayControlsPanelLayout.TransportTop, 0f);
    }

    private static float GetTransportButtonWidth(bool large)
    {
        return large ? ReplayControlsPanelLayout.TransportPlayButtonWidth : ReplayControlsPanelLayout.TransportButtonWidth;
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
        Replayer.SeekToStart();
        RefreshVisualState();
    }

    private void GoToEnd()
    {
        Replayer.SeekToEnd();
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        positionSlider.AllowsInput = ModContent.GetInstance<ClientConfig>().IsSeekbarEnabled;

        uint durationTicks = GetDurationTicks();
        uint currentTick = Math.Min(Replayer.CurrentTick, durationTicks);
        float speed = ModContent.GetInstance<TimeScaleSystem>().TimeScale;
        bool paused = speed <= 0f;

        positionSlider.SetRatio(durationTicks == 0 ? 0f : currentTick / (float)durationTicks);
        positionLabel.SetText($"Position: {FormatTicks(currentTick)} / {FormatTicks(durationTicks)}");
        speedLabel.SetText($"Speed: {FormatSpeedButton(speed)}");
        transportStatusLabel.SetText($"Status: {GetReplayStatus(currentTick, durationTicks, paused)}  |  Elapsed: {Main.GameUpdateCount} ticks");

        for (int i = 0; i < speedButtons.Length; i++)
        {
            bool selected = Math.Abs(speed - SpeedPresets[i]) < 0.001f;
            bool hovered = speedButtons[i].IsMouseHovering;
            speedButtons[i].BackgroundColor = selected ? new Color(73, 94, 171) : hovered ? new Color(61, 78, 141) : new Color(44, 57, 105);
            speedButtons[i].BorderColor = selected || hovered ? Color.Yellow : Color.Black;
        }

        goToStartButton.SetSelected(false);
        nextFrameButton.SetSelected(false);
        playButton.SetSelected(!paused);
        pauseButton.SetSelected(paused);
        stopButton.SetSelected(false);
        goToEndButton.SetSelected(false);
    }

    private static uint GetDurationTicks()
    {
        return Math.Max(1u, Replayer.ActiveDurationTicks);
    }

    private static string FormatSpeedButton(float speed)
    {
        if (Math.Abs(speed - 0.25f) < 0.001f)
            return "1/4x";

        if (Math.Abs(speed - 0.5f) < 0.001f)
            return "1/2x";

        return speed == 1f ? "1x" : $"{speed:0.##}x";
    }

    private static string GetReplayStatus(uint currentTick, uint durationTicks, bool paused)
    {
        if (durationTicks > 1 && currentTick >= durationTicks)
            return "End of file";

        if (currentTick == 0)
            return "At start";

        return paused ? "Paused" : "Playing";
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
