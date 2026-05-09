using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer;
using Reese.Common.TimeScaleTool;
using Reese.Core.Utilities;
using Reese.UI;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace Reese.Common.ReplayTool;

public sealed class ReplayToolPanel : DraggablePanel
{
    private static readonly float[] SpeedPresets = [0.25f, 0.5f, 1f, 2f, 4f];

    private readonly Slider positionSlider;
    private readonly UIText positionLabel;
    private readonly UIText speedLabel;
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
        Width.Set(760f, 0f);
        Height.Set(152f, 0f);
        Recalculate();
        RefreshVisualState();
    }

    public ReplayToolPanel() : base("Replay")
    {
        Width.Set(760f, 0f);
        Height.Set(152f, 0f);
        HAlign = 0.5f;
        VAlign = 0.08f;

        TitlePanel.Height.Set(32f, 0f);
        ContentPanel.Top.Set(32f, 0f);
        ContentPanel.Height.Set(120f, 0f);

        positionLabel = new UIText("", 0.86f)
        {
            Left = { Pixels = 12f },
            Top = { Pixels = 10f },
            TextOriginX = 0f,
            TextOriginY = 0f,
            TextColor = Color.White
        };
        ContentPanel.Append(positionLabel);

        positionSlider = new Slider
        {
            Left = { Pixels = 168f },
            Top = { Pixels = 11f },
            Width = { Percent = 1f, Pixels = -180f },
            Height = { Pixels = 20f }
        };
        positionSlider.OnDrag += ratio =>
        {
            uint duration = GetDurationTicks();
            Replayer.Replayer.SeekToTick((uint)Math.Round(ratio * duration));
        };
        ContentPanel.Append(positionSlider);

        ContentPanel.Append(new HorizontalRule
        {
            Left = { Pixels = 12f },
            Top = { Pixels = 38f },
            Width = { Percent = 1f, Pixels = -24f },
            Height = { Pixels = 2f }
        });

        speedLabel = new UIText("", 0.86f)
        {
            Left = { Pixels = 12f },
            Top = { Pixels = 50f },
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
                Left = { Pixels = 14f + i * 58f },
                Top = { Pixels = 78f }
            };
            button.Width.Set(46f, 0f);
            button.Height.Set(24f, 0f);
            speedButtons[i] = button;
            ContentPanel.Append(button);
        }

        ContentPanel.Append(new VerticalRule
        {
            Left = { Pixels = 312f },
            Top = { Pixels = 42f },
            Width = { Pixels = 2f },
            Height = { Pixels = 70f }
        });

        speedDownButton = CreateTransportButton(Ass.Icon_SpeedDown, "Speed Down", () => StepSpeed(-1), 0);
        nextFrameButton = CreateTransportButton(Ass.Icon_NextFrame, "Next Frame", StepOneFrame, 1);
        playButton = CreateTransportButton(Ass.Icon_Play, "Play", Resume, 2, large: true);
        pauseButton = CreateTransportButton(Ass.Icon_Pause, "Pause", Pause, 3);
        stopButton = CreateTransportButton(Ass.Icon_Stop, "Stop Replay", () => Replayer.Replayer.StopPlayback(), 4);
        speedUpButton = CreateTransportButton(Ass.Icon_SpeedUp, "Speed Up", () => StepSpeed(1), 5);

        RefreshVisualState();
    }

    public override void Recalculate()
    {
        base.Recalculate();

        if (TitlePanel != null)
            TitlePanel.Height.Set(32f, 0f);

        if (ContentPanel != null)
        {
            ContentPanel.Top.Set(32f, 0f);
            ContentPanel.Height.Set(120f, 0f);
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        RefreshVisualState();
    }

    private IconActionButton CreateTransportButton(ReLogic.Content.Asset<Texture2D> texture, string hoverText, Action onClick, int index, bool large = false)
    {
        const float gap = 14f;
        const float normalWidth = 42f;
        const float largeWidth = 48f;
        float left = 334f;

        for (int i = 0; i < index; i++)
            left += (i == 2 ? largeWidth : normalWidth) + gap;

        IconActionButton button = new(texture, hoverText, (_, _) => onClick());
        button.SetOuterWidth(large ? largeWidth : normalWidth);
        button.Left.Set(left, 0f);
        button.Top.Set(55f, 0f);
        ContentPanel.Append(button);
        return button;
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

    private void StepSpeed(int direction)
    {
        float current = ModContent.GetInstance<TimeScaleSystem>().TimeScale;
        int currentIndex = Array.IndexOf(SpeedPresets, TimeScaleSystem.SnapTimeScale(current));

        if (currentIndex < 0)
            currentIndex = Array.IndexOf(SpeedPresets, 1f);

        int nextIndex = Utils.Clamp(currentIndex + direction, 0, SpeedPresets.Length - 1);
        SetSpeed(SpeedPresets[nextIndex]);
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
