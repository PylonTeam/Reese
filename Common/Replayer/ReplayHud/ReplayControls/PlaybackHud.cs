using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Common.Replayer.ReplayHud.Shared.UI;
using Reese.Core.Configs;
using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplayControls;

public sealed class PlaybackHud : DraggablePanel
{
    private static readonly float[] SpeedPresets = [0.25f, 0.5f, 1f, 2f, 4f, 8f, 16f, 32f];

    private readonly Slider positionSlider;
    private readonly ReplayEventMarkerLayer eventMarkerLayer;

    private readonly CenteredHudText positionLabel;
    private readonly CenteredHudText speedLabel;
    private readonly CenteredHudText transportStatusLabel;
    private readonly CenteredHudText transportTickLabel;

    private readonly CompactTextPanel<string>[] speedButtons;
    private readonly IconActionButton[] transportButtons;
    private readonly HorizontalRule horizontalRule;
    private readonly VerticalRule verticalRule;

    private int settingsRevision = ReplayClientSettings.HudRevision;

    public PlaybackHud() : base(Loc.Get("ReplayHud.Playback.Title"))
    {
        PlaybackLayout.ApplyDefaultAnchor(this);

        speedLabel = new CenteredHudText(0.82f);
        speedButtons = new CompactTextPanel<string>[SpeedPresets.Length];

        for (int i = 0; i < SpeedPresets.Length; i++)
        {
            float speed = SpeedPresets[i];
            speedButtons[i] = new CompactTextPanel<string>(FormatSpeedButton(speed), 0.72f, false, leftClick: () => SetSpeed(speed), backgroundColor: new Color(32, 43, 92), padding: 5f);
        }

        verticalRule = new VerticalRule();

        positionLabel = new CenteredHudText(0.86f);
        positionSlider = new Slider();
        eventMarkerLayer = new ReplayEventMarkerLayer();
        positionSlider.OnDrag += ratio =>
        {
            uint targetTick = RatioToTick(ratio);
            if (targetTick < ReplayPlayback.CurrentTick)
                RefreshPositionSlider();
            else
                RefreshTimeLabel(targetTick, GetDurationTicks());
        };
        positionSlider.OnRelease += ratio =>
        {
            uint targetTick = RatioToTick(ratio);

            ReplayPlayback.SeekToTick(targetTick);

            RefreshPositionSlider();
        };

        horizontalRule = new HorizontalRule();
        transportStatusLabel = new CenteredHudText(0.86f);
        transportTickLabel = new CenteredHudText(0.86f);
        transportButtons =
        [
            CreateTransportButton(Ass.IconSpeedDown, Loc.Get("ReplayHud.Playback.GoToStart"), GoToStart),
            CreateTransportButton(Ass.IconNextFrame, Loc.Get("ReplayHud.Playback.NextFrame"), StepOneFrame),
            CreateTransportButton(Ass.IconBackArrow, Loc.Get("ReplayHud.Playback.SeekBackwardSeconds", 30), SeekBackward30, "-30"),
            CreateTransportButton(Ass.IconPlay, Language.GetTextValue("UI.Play"), Resume),
            CreateTransportButton(Ass.IconPause, Loc.Get("ReplayHud.Playback.Pause"), Pause),
            CreateTransportButton(Ass.IconForwardsArrow, Loc.Get("ReplayHud.Playback.SeekForwardSeconds", 30), SeekForward30, "+30"),
            CreateTransportButton(Ass.IconStop, Loc.Get("ReplayHud.Playback.StopReplay"), () => ReplayPlayback.End("user stopped replay", quitPlayer: true)),
            CreateTransportButton(Ass.IconSpeedUp, Loc.Get("ReplayHud.Playback.GoToEnd"), GoToEnd),
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
        RefreshLayoutIfNeeded();

        base.Update(gameTime);

        RefreshVisualState();
    }

    private void RefreshLayoutIfNeeded()
    {
        if (!PlaybackLayout.Update(speedButtons.Length, transportButtons.Length))
            return;

        ApplyLayout();
        Recalculate();
    }

    protected override bool CanStartDrag(UIElement target)
    {
        return base.CanStartDrag(target) ||
            target == speedLabel ||
            target == positionLabel ||
            target == transportStatusLabel ||
            target == transportTickLabel ||
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
            ContentPanel.Append(positionSlider);
            ContentPanel.Append(eventMarkerLayer);
        }

        if (showSeekbar && showControls)
            ContentPanel.Append(horizontalRule);

        if (showControls)
        {
            ContentPanel.Append(transportStatusLabel);
            ContentPanel.Append(positionLabel);
            ContentPanel.Append(transportTickLabel);

            for (int i = 0; i < transportButtons.Length; i++)
                ContentPanel.Append(transportButtons[i]);
        }
    }

    private void ApplyLayout()
    {
        PlaybackLayout.Apply(
            this,
            speedLabel,
            speedButtons,
            verticalRule,
            positionLabel,
            positionSlider,
            eventMarkerLayer,
            horizontalRule,
            transportStatusLabel,
            transportTickLabel,
            transportButtons);
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
    private static IconActionButton CreateTransportButton(ReLogic.Content.Asset<Texture2D> texture, string hoverText, Action onClick, string label = "")
    {
        IconActionButton button = new(texture, hoverText, (_, _) => onClick());

        if (label.Length <= 0)
            return button;

        UIText text = new(label, 0.6f)
        {
            HAlign = 0.5f,
            Top = new StyleDimension(22f, 0f),
            TextOriginX = 0.5f,
            TextOriginY = 0f,
            TextColor = Color.White,
            IgnoresMouseInteraction = true
        };

        button.Append(text);

        return button;
    }

    private static void SetSpeed(float value)
    {
        ModContent.GetInstance<ReplayTimeScaleSystem>().SetTimeScale(value);
    }

    #region Actions
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

    private void SeekBackward30()
    {
        uint ticks = (uint)(30 * 60);
        uint target = ReplayPlayback.CurrentTick > ticks ? ReplayPlayback.CurrentTick - ticks : 0u;
        ReplayPlayback.SeekToTick(target);
        RefreshVisualState();
    }

    private void SeekForward30()
    {
        uint target = ReplayPlayback.CurrentTick + (uint)(30 * 60);
        ReplayPlayback.SeekToTick(target);
        RefreshVisualState();
    }
    #endregion

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

            positionSlider.HighlightColor = Main.OurFavoriteColor;
        }

        if (ReplayClientSettings.ShowReplayHudSpeed)
        {
            speedLabel.SetTextIfChanged(Loc.Get("ReplayHud.Playback.Speed", FormatSpeedButton(speed)));

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

        transportStatusLabel.SetTextIfChanged(Loc.Get("ReplayHud.Playback.Status", GetReplayStatus(currentTick, durationTicks, paused)));
        RefreshTimeLabel(displayTick, durationTicks);
        transportTickLabel.SetTextIfChanged(Loc.Get("ReplayHud.Playback.Tick", currentTick));

        for (int i = 0; i < transportButtons.Length; i++)
            transportButtons[i].SetSelected(false);

        transportButtons[3].SetSelected(!paused);  // Play
        transportButtons[4].SetSelected(paused);   // Pause
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

    private void RefreshTimeLabel(uint displayTick, uint durationTicks)
    {
        positionLabel.SetTextIfChanged(Loc.Get("ReplayHud.Playback.Time", FormatTime(displayTick), FormatTime(durationTicks)));
    }

    private static uint GetDurationTicks()
    {
        return Math.Max(1u, ReplayPlayback.DurationTicks);
    }

    private uint RatioToTick(float ratio)
    {
        return (uint)Math.Round(MathHelper.Clamp(ratio, 0f, 1f) * GetDurationTicks());
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
            return Loc.Get("ReplayHud.Playback.ReplayStatus.Seeking");

        if (durationTicks > 1 && currentTick >= durationTicks)
            return Loc.Get("ReplayHud.Playback.ReplayStatus.End");

        if (currentTick == 0)
            return Language.GetTextValue("LegacyMenu.144");

        return Loc.Get("ReplayHud.Playback." + (paused ? "ReplayStatus.Paused" : "ReplayStatus.Playing"));
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

    private sealed class CenteredHudText : UIText
    {
        private readonly float scale;
        private string currentText = "";

        public CenteredHudText(float scale) : base("", scale)
        {
            this.scale = scale;
            TextOriginX = 0.5f;
            TextOriginY = 0f;
        }

        internal void SetTextIfChanged(string text)
        {
            if (currentText == text)
                return;

            currentText = text;
            SetText(text);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            Vector2 position = new(dimensions.X + dimensions.Width * 0.5f, dimensions.Y);

            Utils.DrawBorderString(spriteBatch, currentText, position, TextColor, scale, 0.5f, 0f);
        }
    }
}
