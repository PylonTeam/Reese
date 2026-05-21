using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Common.Replayer.ReplayHud.Shared.UI;
using Reese.Core.Configs;
using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplayControls;

public sealed class PlaybackHud : DraggablePanel
{
    private static readonly float[] SpeedPresets = [0.25f, 0.5f, 1f, 2f, 4f, 8f, 16f, 32f];

    private const float Padding = 12f;
    private const float SpeedColumnWidth = 100f;
    private const float RightColumnWidth = 290f;
    private const float ColumnGap = 16f;
    private const float SpeedSectionHeight = 152f;
    private const float SeekbarSectionHeight = 56f;
    private const float ControlsSectionHeight = 78f;
    private const float SectionGap = 14f;

    private readonly Slider positionSlider;
    private readonly UIText positionLabel;
    private readonly UIText speedLabel;
    private readonly UIText transportStatusLabel;
    private readonly CompactTextPanel<string>[] speedButtons;
    private readonly IconActionButton[] transportButtons;
    private readonly HorizontalRule horizontalRule;
    private readonly VerticalRule verticalRule;

    private int settingsRevision = ReplayClientSettings.HudRevision;

    public PlaybackHud() : base("Replay")
    {
        HAlign = 0.5f;
        VAlign = 0.92f;

        speedLabel = CreateLabel(0.82f);
        speedButtons = new CompactTextPanel<string>[SpeedPresets.Length];

        for (int i = 0; i < SpeedPresets.Length; i++)
        {
            float speed = SpeedPresets[i];
            speedButtons[i] = new CompactTextPanel<string>(FormatSpeedButton(speed), 0.72f, false, leftClick: () => SetSpeed(speed), backgroundColor: new Color(32, 43, 92), padding: 5f);
        }

        verticalRule = new VerticalRule();

        positionLabel = CreateLabel(0.86f);
        positionSlider = new Slider();
        positionSlider.OnDrag += ratio =>
        {
            uint targetTick = RatioToTick(ratio);
            if (targetTick < ReplayPlayback.CurrentTick && !IsBackwardsSeekingEnabled())
                RefreshPositionSlider();
            else
                positionLabel.SetText($"Time: {FormatTime(targetTick)} / {FormatTime(GetDurationTicks())}");
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

        horizontalRule = new HorizontalRule();
        transportStatusLabel = CreateLabel(0.86f);
        transportButtons =
        [
            CreateTransportButton(Ass.IconSpeedDown, "Go to start", GoToStart),
            CreateTransportButton(Ass.IconNextFrame, "Next Frame", StepOneFrame),
            CreateTransportButton(Ass.IconPlay, "Play", Resume),
            CreateTransportButton(Ass.IconPause, "Pause", Pause),
            CreateTransportButton(Ass.IconStop, "Stop Replay", () => ReplayPlayback.End("user stopped replay", quitPlayer: true)),
            CreateTransportButton(Ass.IconSpeedUp, "Go to end", GoToEnd)
        ];

        RebuildContent();
        ApplyLayout();
        RefreshVisualState();
    }

    public override void Recalculate()
    {
        ApplyLayout();
        base.Recalculate();
        ApplyLayout();
    }

    public override void Update(GameTime gameTime)
    {
        RefreshSettingsIfNeeded();
        base.Update(gameTime);
        RefreshVisualState();
    }

    protected override bool CanStartDrag(UIElement target)
    {
        return base.CanStartDrag(target) ||
            target == speedLabel ||
            target == positionLabel ||
            target == transportStatusLabel ||
            target == horizontalRule ||
            target == verticalRule;
    }

    private void RefreshSettingsIfNeeded()
    {
        if (settingsRevision == ReplayClientSettings.HudRevision)
            return;

        settingsRevision = ReplayClientSettings.HudRevision;
        RebuildContent();
        ApplyLayout();
        Recalculate();
    }

    private void RebuildContent()
    {
        ContentPanel.RemoveAllChildren();

        bool showSpeed = ReplayClientSettings.ShowReplayHudSpeed;
        bool showSeekbar = ReplayClientSettings.ShowReplayHudSeekbar;
        bool showControls = ReplayClientSettings.ShowReplayHudPlaybackControls;

        if (showSpeed)
        {
            ContentPanel.Append(speedLabel);

            for (int i = 0; i < speedButtons.Length; i++)
                ContentPanel.Append(speedButtons[i]);
        }

        if (showSpeed && (showSeekbar || showControls))
            ContentPanel.Append(verticalRule);

        if (showSeekbar)
        {
            ContentPanel.Append(positionLabel);
            ContentPanel.Append(positionSlider);
        }

        if (showSeekbar && showControls)
            ContentPanel.Append(horizontalRule);

        if (showControls)
        {
            ContentPanel.Append(transportStatusLabel);

            for (int i = 0; i < transportButtons.Length; i++)
                ContentPanel.Append(transportButtons[i]);
        }
    }

    private void ApplyLayout()
    {
        bool showSpeed = ReplayClientSettings.ShowReplayHudSpeed;
        bool showSeekbar = ReplayClientSettings.ShowReplayHudSeekbar;
        bool showControls = ReplayClientSettings.ShowReplayHudPlaybackControls;
        bool showRightColumn = showSeekbar || showControls;

        float innerWidth = 0f;
        if (showSpeed)
            innerWidth += SpeedColumnWidth;

        if (showSpeed && showRightColumn)
            innerWidth += ColumnGap;

        if (showRightColumn)
            innerWidth += RightColumnWidth;

        float rightHeight = 0f;
        if (showSeekbar)
            rightHeight += SeekbarSectionHeight;

        if (showSeekbar && showControls)
            rightHeight += SectionGap;

        if (showControls)
            rightHeight += ControlsSectionHeight;

        float innerHeight = Math.Max(showSpeed ? SpeedSectionHeight : 0f, rightHeight);
        Width.Set(Math.Max(180f, innerWidth + Padding * 2f), 0f);
        Height.Set(Math.Max(48f, innerHeight + Padding * 2f), 0f);

        float rightLeft = Padding + (showSpeed ? SpeedColumnWidth + (showRightColumn ? ColumnGap : 0f) : 0f);

        speedLabel.Left.Set(Padding, 0f);
        speedLabel.Top.Set(Padding, 0f);

        for (int i = 0; i < speedButtons.Length; i++)
        {
            int column = i % 2;
            int row = i / 2;

            speedButtons[i].Left.Set(Padding + column * 50f, 0f);
            speedButtons[i].Top.Set(Padding + 28f + row * 32f, 0f);
            speedButtons[i].Width.Set(44f, 0f);
            speedButtons[i].Height.Set(28f, 0f);
        }

        verticalRule.Left.Set(Padding + SpeedColumnWidth + ColumnGap * 0.5f - 1f, 0f);
        verticalRule.Top.Set(Padding, 0f);
        verticalRule.Width.Set(2f, 0f);
        verticalRule.Height.Set(innerHeight, 0f);

        positionLabel.Left.Set(rightLeft, 0f);
        positionLabel.Top.Set(Padding, 0f);

        positionSlider.Left.Set(rightLeft, 0f);
        positionSlider.Top.Set(Padding + 28f, 0f);
        positionSlider.Width.Set(RightColumnWidth, 0f);
        positionSlider.Height.Set(18f, 0f);

        float controlsTop = Padding + (showSeekbar ? SeekbarSectionHeight + SectionGap : 0f);
        horizontalRule.Left.Set(rightLeft, 0f);
        horizontalRule.Top.Set(controlsTop - SectionGap * 0.5f, 0f);
        horizontalRule.Width.Set(RightColumnWidth, 0f);
        horizontalRule.Height.Set(2f, 0f);

        transportStatusLabel.Left.Set(rightLeft, 0f);
        transportStatusLabel.Top.Set(controlsTop, 0f);

        float gap = 6f;
        float buttonTop = controlsTop + 36f;
        float totalWidth = 0f;

        for (int i = 0; i < transportButtons.Length; i++)
            totalWidth += GetTransportButtonWidth(i) + (i == 0 ? 0f : gap);

        float left = rightLeft + (RightColumnWidth - totalWidth) * 0.5f;

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
            0 => 44f,
            2 => 60f,
            5 => 44f,
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

    private static IconActionButton CreateTransportButton(ReLogic.Content.Asset<Texture2D> texture, string hoverText, Action onClick)
    {
        return new IconActionButton(texture, hoverText, (_, _) => onClick());
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
        uint durationTicks = GetDurationTicks();
        uint currentTick = Math.Min(ReplayPlayback.CurrentTick, durationTicks);
        uint displayTick = positionSlider.IsHeld ? RatioToTick(positionSlider.Ratio) : currentTick;
        float speed = ModContent.GetInstance<ReplayTimeScaleSystem>().TimeScale;
        bool paused = speed <= 0f;

        positionSlider.AllowsInput = ReplayClientSettings.ShowReplayHudSeekbar;

        if (ReplayClientSettings.ShowReplayHudSeekbar)
        {
            if (!positionSlider.IsHeld)
                RefreshPositionSlider(currentTick, durationTicks);

            positionSlider.HighlightColor = !IsBackwardsSeekingEnabled() && IsHoveringBackwardPosition() ? Color.Red : Main.OurFavoriteColor;
            positionLabel.SetText($"Time: {FormatTime(displayTick)} / {FormatTime(durationTicks)}");
        }

        if (ReplayClientSettings.ShowReplayHudSpeed)
        {
            speedLabel.SetText($"Speed: {FormatSpeedButton(speed)}");

            for (int i = 0; i < speedButtons.Length; i++)
            {
                bool selected = Math.Abs(speed - SpeedPresets[i]) < 0.001f;
                bool hovered = speedButtons[i].IsMouseHovering;

                speedButtons[i].BackgroundColor =
                    selected ? new Color(47, 61, 125) :
                    hovered ? new Color(42, 55, 112) :
                    new Color(32, 43, 92);

                speedButtons[i].BorderColor = selected || hovered ? Color.Yellow : Color.Black;
            }
        }

        if (!ReplayClientSettings.ShowReplayHudPlaybackControls)
            return;

        transportStatusLabel.SetText($"Status: {GetReplayStatus(currentTick, durationTicks, paused)}  |  Tick: {currentTick}");

        for (int i = 0; i < transportButtons.Length; i++)
            transportButtons[i].SetSelected(false);

        transportButtons[2].SetSelected(!paused);
        transportButtons[3].SetSelected(paused);
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
        if (ReplayPlayback.IsSeeking)
            return "Seeking";

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
