using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Common.Replayer.ReplayHud.Shared.UI;
using Reese.Core.Configs;
using ReLogic.Content;
using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplayControls;

public sealed class PlaybackHud : DraggablePanel
{
    private static readonly float[] SpeedPresets = [0.25f, 0.5f, 1f, 2f, 4f, 8f];

    private readonly Slider positionSlider;
    private readonly UIText positionLabel;
    private readonly UIText speedLabel;
    private readonly UIText transportStatusLabel;
    private readonly CompactTextPanel<string>[] speedButtons;
    private readonly IconActionButton[] transportButtons;
    private readonly HorizontalRule horizontalRule;
    private readonly VerticalRule verticalRule;

    protected override Asset<Texture2D> LeftIcon => Ass.IconReset;

    protected override void OnRightIconTitlePanelClick()
    {
        ModContent.GetInstance<ReplayHudSystem>().ClosePlaybackHud();
    }

    protected override void OnLeftIconTitlePanelClick()
    {
        //ApplyLayout();
        //Recalculate();
        //RefreshVisualState();

        // Reset position
        Log.Chat("Resetting position...");
        HAlign = 0.5f;
        VAlign = 0.92f;
        Left.Set(0f, 0f);
        Top.Set(0f, 0f);
    }

    public PlaybackHud() : base("Replay")
    {
        Width.Set(430f, 0f);
        Height.Set(200f, 0f);
        HAlign = 0.5f;
        VAlign = 0.92f;

        speedLabel = CreateLabel(0.82f);
        ContentPanel.Append(speedLabel);

        speedButtons = new CompactTextPanel<string>[SpeedPresets.Length];

        for (int i = 0; i < SpeedPresets.Length; i++)
        {
            float speed = SpeedPresets[i];
            speedButtons[i] = new CompactTextPanel<string>(FormatSpeedButton(speed), 0.72f, false, leftClick: () => SetSpeed(speed), backgroundColor: new Color(44, 57, 105), padding: 5f);
            ContentPanel.Append(speedButtons[i]);
        }

        verticalRule = new VerticalRule();
        ContentPanel.Append(verticalRule);

        positionLabel = CreateLabel(0.86f);
        ContentPanel.Append(positionLabel);

        positionSlider = new Slider();
        positionSlider.OnDrag += ratio =>
        {
            uint targetTick = RatioToTick(ratio);
            if (targetTick < ReplayPlayback.CurrentTick)
            {
                if (!IsBackwardsSeekingEnabled())
                    RefreshPositionSlider();

                return;
            }

            ReplayPlayback.SeekToTick(targetTick);
        };
        positionSlider.OnRelease += ratio =>
        {
            uint targetTick = RatioToTick(ratio);

            if (targetTick < ReplayPlayback.CurrentTick && !IsBackwardsSeekingEnabled())
                Main.NewText("You cannot go backwards in a replay. Enable Backwards Seeking in the debug config to allow it.", Color.OrangeRed);
            else
                ReplayPlayback.SeekToTick(targetTick);

            RefreshPositionSlider();
        };
        ContentPanel.Append(positionSlider);

        horizontalRule = new HorizontalRule();
        ContentPanel.Append(horizontalRule);

        transportStatusLabel = CreateLabel(0.86f);
        ContentPanel.Append(transportStatusLabel);

        transportButtons =
        [
            CreateTransportButton(Ass.IconSpeedDown, "Go to start", GoToStart),
            CreateTransportButton(Ass.IconNextFrame, "Next Frame", StepOneFrame),
            CreateTransportButton(Ass.IconPlay, "Play", Resume),
            CreateTransportButton(Ass.IconPause, "Pause", Pause),
            CreateTransportButton(Ass.IconStop, "Stop Replay", () => ReplayPlayback.End("user stopped replay", quitPlayer: true)),
            CreateTransportButton(Ass.IconSpeedUp, "Go to end", GoToEnd)
        ];

        ApplyLayout();
        RefreshVisualState();
    }

    //private void Rebuild()
    //{
    //    ApplyLayout();
    //    Recalculate();
    //    RefreshVisualState();
    //}

    public override void Recalculate()
    {
        ApplyLayout();
        base.Recalculate();
        ApplyLayout();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        RefreshVisualState();
    }

    private void ApplyLayout()
    {
        Width.Set(430f, 0f);
        Height.Set(200f, 0f);

        if (TitlePanel != null)
            TitlePanel.Height.Set(32f, 0f);

        if (ContentPanel != null)
        {
            ContentPanel.Top.Set(32f, 0f);
            ContentPanel.Height.Set(168f, 0f);
        }

        speedLabel.Left.Set(12f, 0f);
        speedLabel.Top.Set(10f, 0f);

        for (int i = 0; i < speedButtons.Length; i++)
        {
            int column = i % 2;
            int row = i / 2;

            speedButtons[i].Left.Set(12f + column * 50f, 0f);
            speedButtons[i].Top.Set(38f + row * 32f, 0f);
            speedButtons[i].Width.Set(44f, 0f);
            speedButtons[i].Height.Set(28f, 0f);
        }

        verticalRule.Left.Set(118f, 0f);
        verticalRule.Top.Set(10f, 0f);
        verticalRule.Width.Set(2f, 0f);
        verticalRule.Height.Set(138f, 0f);

        positionLabel.Left.Set(136f, 0f);
        positionLabel.Top.Set(10f, 0f);

        positionSlider.Left.Set(136f, 0f);
        positionSlider.Top.Set(38f, 0f);
        positionSlider.Width.Set(-154f, 1f);
        positionSlider.Height.Set(18f, 0f);

        horizontalRule.Left.Set(136f, 0f);
        horizontalRule.Top.Set(70f, 0f);
        horizontalRule.Width.Set(-154f, 1f);
        horizontalRule.Height.Set(2f, 0f);

        transportStatusLabel.Left.Set(136f, 0f);
        transportStatusLabel.Top.Set(82f, 0f);

        float rightColumnLeft = 136f;
        float rightColumnRight = 412f;
        float gap = 6f;
        float buttonTop = 118f;
        float totalWidth = 0f;

        for (int i = 0; i < transportButtons.Length; i++)
            totalWidth += GetTransportButtonWidth(i) + (i == 0 ? 0f : gap);

        float left = rightColumnLeft + (rightColumnRight - rightColumnLeft - totalWidth) * 0.5f;

        for (int i = 0; i < transportButtons.Length; i++)
        {
            float width = GetTransportButtonWidth(i);
            transportButtons[i].SetOuterWidth(width);
            transportButtons[i].Left.Set(left, 0f);
            transportButtons[i].Top.Set(buttonTop, 0f);
            left += width + gap;
        }
    }

    private static float GetTransportButtonWidth(int index)
    {
        return index switch
        {
            0 => 44f, // Go to start
            2 => 60f, // Play
            5 => 44f, // Go to end
            _ => 36f
        };
    }

    private static UIText CreateLabel(float scale)
    {
        return new UIText("", scale)
        {
            TextOriginX = 0f,
            TextOriginY = 0f,
            TextColor = Color.White
        };
    }

    private IconActionButton CreateTransportButton(ReLogic.Content.Asset<Texture2D> texture, string hoverText, Action onClick)
    {
        IconActionButton button = new(texture, hoverText, (_, _) => onClick());
        ContentPanel.Append(button);
        return button;
    }

    private static void SetSpeed(float value)
    {
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(value);
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
        ModContent.GetInstance<ReplayTimeScaleSystem>().StepOneFrame();
        RefreshVisualState();
    }

    private void GoToStart()
    {
        ReplayPlayback.SeekToStart();
        RefreshVisualState();
    }

    private void GoToEnd()
    {
        ReplayPlayback.SeekToEnd();
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        positionSlider.AllowsInput = true;

        uint durationTicks = GetDurationTicks();
        uint currentTick = Math.Min(ReplayPlayback.CurrentTick, durationTicks);
        float speed = ModContent.GetInstance<ReplayTimeScaleSystem>().TimeScale;
        bool paused = speed <= 0f;

        if (!positionSlider.IsHeld)
            RefreshPositionSlider(currentTick, durationTicks);

        positionSlider.HighlightColor = !IsBackwardsSeekingEnabled() && IsHoveringBackwardPosition() ? Color.Red : Main.OurFavoriteColor;
        positionLabel.SetText($"Time: {FormatTime(currentTick)} / {FormatTime(durationTicks)}");
        speedLabel.SetText($"Speed: {FormatSpeedButton(speed)}");
        transportStatusLabel.SetText($"Status: {GetReplayStatus(currentTick, durationTicks, paused)}  |  Tick: {currentTick}");

        for (int i = 0; i < speedButtons.Length; i++)
        {
            bool selected = Math.Abs(speed - SpeedPresets[i]) < 0.001f;
            bool hovered = speedButtons[i].IsMouseHovering;

            speedButtons[i].BackgroundColor =
                selected ? new Color(73, 94, 171) :
                hovered ? new Color(61, 78, 141) :
                new Color(44, 57, 105);

            speedButtons[i].BorderColor = selected || hovered ? Color.Yellow : Color.Black;
        }

        for (int i = 0; i < transportButtons.Length; i++)
            transportButtons[i].SetSelected(false);

        transportButtons[2].SetSelected(!paused); // Play
        transportButtons[3].SetSelected(paused);  // Pause
    }

    private void RefreshPositionSlider()
    {
        uint durationTicks = GetDurationTicks();
        uint currentTick = Math.Min(ReplayPlayback.CurrentTick, durationTicks);
        RefreshPositionSlider(currentTick, durationTicks);
    }

    private void RefreshPositionSlider(uint currentTick, uint durationTicks)
    {
        positionSlider.SetRatio(currentTick / (float)durationTicks);
    }

    private static uint GetDurationTicks()
    {
        return Math.Max(1u, ReplayPlayback.DurationTicks);
    }

    private uint RatioToTick(float ratio)
    {
        return (uint)Math.Round(MathHelper.Clamp(ratio, 0f, 1f) * GetDurationTicks());
    }

    private bool IsHoveringBackwardPosition()
    {
        if (!positionSlider.AllowsInput || (!positionSlider.IsMouseHovering && !positionSlider.IsHeld))
            return false;

        return RatioToTick(positionSlider.GetMouseRatio()) < ReplayPlayback.CurrentTick;
    }

    private static bool IsBackwardsSeekingEnabled()
    {
        return ModContent.GetInstance<ClientConfig>()?.EnableBackwardsSeeking == true;
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
            return "End";

        if (currentTick == 0)
            return "Start";

        return paused ? "Paused" : "Playing";
    }

    private static string FormatTime(uint ticks)
    {
        TimeSpan span = TimeSpan.FromSeconds(ticks / 60d);
        return span.TotalHours >= 1d ? $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}" : $"{span.Minutes:00}:{span.Seconds:00}";
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
