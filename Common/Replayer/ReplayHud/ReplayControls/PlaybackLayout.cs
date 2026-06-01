using Reese.Common.Replayer.ReplayHud.Shared.UI;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplayControls;

/// <summary>
/// Hot reload layout for <see cref="PlaybackHud"/>.
/// </summary>
internal static class PlaybackLayout
{
    internal static float PanelHAlign;
    internal static float PanelVAlign;
    internal static float PanelMinWidth;
    internal static float PanelMinHeight;
    internal static float PanelWidth;
    internal static float PanelHeight;

    internal static float Padding;
    internal static float ColumnGap;
    internal static float SectionGap;

    internal static float SpeedColumnWidth;
    internal static float SpeedLabelHeight;
    internal static float SpeedLabelButtonGap;
    internal static int SpeedButtonColumns;
    internal static float SpeedButtonWidth;
    internal static float SpeedButtonHeight;
    internal static float SpeedButtonGapX;
    internal static float SpeedButtonGapY;
    internal static float SpeedSectionHeight;

    internal static float RightColumnWidth;
    internal static bool RightColumnAutoFitControls;

    internal static float SeekbarSectionHeight;
    internal static float PositionLabelHeight;
    internal static float PositionLabelSliderGap;
    internal static float EventIconLaneHeight;
    internal static float SliderHeight;

    internal static float ControlsSectionHeight;
    internal static float TransportStatusHeight;
    internal static float TransportStatusButtonGap;
    internal static float TransportButtonHeight;
    internal static float TransportButtonGap;
    internal static float[] TransportButtonWidths;

    internal static float RuleThickness;
    internal static float RuleAlpha;

    internal static bool ShowSpeed;
    internal static bool ShowSeekbar;
    internal static bool ShowEvents;
    internal static bool ShowControls;
    internal static bool ShowRightColumn;

    internal static float InnerWidth;
    internal static float InnerHeight;
    internal static float RightLeft;
    internal static float ControlsTop;
    internal static float TransportButtonTop;
    internal static float TransportButtonRowWidth;

    internal static float TransportStatusTickGap;

    internal static PlaybackLayoutBox SpeedLabelBox;
    internal static PlaybackLayoutBox[] SpeedButtonBoxes;
    internal static PlaybackLayoutBox VerticalRuleBox;

    internal static PlaybackLayoutBox PositionLabelBox;
    internal static PlaybackLayoutBox PositionSliderBox;
    internal static PlaybackLayoutBox EventMarkerBox;
    internal static PlaybackLayoutBox HorizontalRuleBox;

    internal static PlaybackLayoutBox TransportStatusBox;
    internal static PlaybackLayoutBox TransportTickBox;
    internal static PlaybackLayoutBox[] TransportButtonBoxes;

    internal static bool Update(int speedButtonCount = 8, int transportButtonCount = 6)
    {
        bool changed = false;

        Set(ref PanelHAlign, 0.5f, ref changed);
        Set(ref PanelVAlign, 0.92f, ref changed);
        Set(ref PanelMinWidth, 180f, ref changed);
        Set(ref PanelMinHeight, 48f, ref changed);

        Set(ref Padding, 12f, ref changed);
        Set(ref ColumnGap, 16f, ref changed);
        Set(ref SectionGap, 14f, ref changed);

        Set(ref SpeedColumnWidth, 100f, ref changed);
        Set(ref SpeedLabelHeight, 22f, ref changed);
        Set(ref SpeedLabelButtonGap, 6f, ref changed);
        Set(ref SpeedButtonColumns, 2, ref changed);
        Set(ref SpeedButtonWidth, 44f, ref changed);
        Set(ref SpeedButtonHeight, 28f, ref changed);
        Set(ref SpeedButtonGapX, 6f, ref changed);
        Set(ref SpeedButtonGapY, 4f, ref changed);

        Set(ref RightColumnWidth, 290f, ref changed);
        Set(ref RightColumnAutoFitControls, true, ref changed);

        Set(ref PositionLabelHeight, 22f, ref changed);
        Set(ref PositionLabelSliderGap, 6f, ref changed);
        Set(ref EventIconLaneHeight, 36f, ref changed);
        Set(ref SliderHeight, 18f, ref changed);

        Set(ref TransportStatusHeight, 22f, ref changed);
        Set(ref TransportStatusButtonGap, 14f, ref changed);
        Set(ref TransportStatusTickGap, 12f, ref changed);
        Set(ref TransportButtonHeight, 36f, ref changed);
        Set(ref TransportButtonGap, 6f, ref changed);

        Set(ref RuleThickness, 2f, ref changed);
        Set(ref RuleAlpha, 0.18f, ref changed);

        SetArray(ref TransportButtonWidths, [44f, 36f, 44f, 60f, 36f, 44f, 36f, 44f], ref changed);

        Set(ref ShowSpeed, ReplayClientSettings.ShowReplayHudSpeed, ref changed);
        Set(ref ShowSeekbar, ReplayClientSettings.ShowReplayHudSeekbar, ref changed);
        Set(ref ShowEvents, ReplayClientSettings.ShowEvents && ReplayClientSettings.ShowReplayHudSeekbar, ref changed);
        Set(ref ShowControls, ReplayClientSettings.ShowReplayHudPlaybackControls, ref changed);
        Set(ref ShowRightColumn, ShowSeekbar || ShowControls, ref changed);

        int speedColumns = System.Math.Max(1, SpeedButtonColumns);
        int speedRows = speedButtonCount <= 0 ? 0 : (speedButtonCount + speedColumns - 1) / speedColumns;

        float speedButtonGridWidth = speedColumns * SpeedButtonWidth + (speedColumns - 1) * SpeedButtonGapX;
        float speedButtonGridHeight = speedRows * SpeedButtonHeight + System.Math.Max(0, speedRows - 1) * SpeedButtonGapY;
        float speedSectionWidth = System.Math.Max(SpeedColumnWidth, speedButtonGridWidth);
        float speedSectionHeight = SpeedLabelHeight + SpeedLabelButtonGap + speedButtonGridHeight;

        Set(ref SpeedSectionHeight, speedSectionHeight, ref changed);

        float seekbarSectionHeight = PositionLabelHeight + PositionLabelSliderGap + (ShowEvents ? EventIconLaneHeight : 0f) + SliderHeight;
        Set(ref SeekbarSectionHeight, seekbarSectionHeight, ref changed);

        float controlsSectionHeight = TransportStatusHeight + TransportStatusButtonGap + TransportButtonHeight;
        Set(ref ControlsSectionHeight, controlsSectionHeight, ref changed);

        float transportButtonRowWidth = GetTransportButtonRowWidth(transportButtonCount);
        Set(ref TransportButtonRowWidth, transportButtonRowWidth, ref changed);

        float targetRightColumnWidth = ShowEvents ? GetEventRightColumnWidth(speedSectionWidth) : RightColumnWidth;
        float rightColumnWidth = RightColumnAutoFitControls ? System.Math.Max(targetRightColumnWidth, transportButtonRowWidth) : targetRightColumnWidth;

        float innerWidth = 0f;
        if (ShowSpeed)
            innerWidth += speedSectionWidth;

        if (ShowSpeed && ShowRightColumn)
            innerWidth += ColumnGap;

        if (ShowRightColumn)
            innerWidth += rightColumnWidth;

        float rightHeight = 0f;
        if (ShowSeekbar)
            rightHeight += SeekbarSectionHeight;

        if (ShowSeekbar && ShowControls)
            rightHeight += SectionGap;

        if (ShowControls)
            rightHeight += ControlsSectionHeight;

        float innerHeight = System.Math.Max(ShowSpeed ? SpeedSectionHeight : 0f, rightHeight);

        Set(ref InnerWidth, innerWidth, ref changed);
        Set(ref InnerHeight, innerHeight, ref changed);
        Set(ref PanelWidth, System.Math.Max(PanelMinWidth, innerWidth + Padding * 2f), ref changed);
        Set(ref PanelHeight, System.Math.Max(PanelMinHeight, innerHeight + Padding * 2f), ref changed);

        float rightLeft = Padding + (ShowSpeed ? speedSectionWidth + (ShowRightColumn ? ColumnGap : 0f) : 0f);
        Set(ref RightLeft, rightLeft, ref changed);

        Set(ref SpeedLabelBox, new PlaybackLayoutBox(Padding, Padding, speedSectionWidth, SpeedLabelHeight), ref changed);
        SetBoxes(ref SpeedButtonBoxes, BuildSpeedButtonBoxes(speedButtonCount, speedColumns), ref changed);

        float verticalRuleLeft = Padding + speedSectionWidth + ColumnGap * 0.5f - RuleThickness * 0.5f;
        Set(ref VerticalRuleBox, new PlaybackLayoutBox(verticalRuleLeft, Padding, RuleThickness, InnerHeight), ref changed);

        Set(ref PositionLabelBox, new PlaybackLayoutBox(RightLeft, Padding, rightColumnWidth, PositionLabelHeight), ref changed);

        float sliderTop = Padding + PositionLabelHeight + PositionLabelSliderGap + (ShowEvents ? EventIconLaneHeight : 0f);
        Set(ref PositionSliderBox, new PlaybackLayoutBox(RightLeft, sliderTop, rightColumnWidth, SliderHeight), ref changed);
        Set(ref EventMarkerBox, new PlaybackLayoutBox(RightLeft, sliderTop - (ShowEvents ? EventIconLaneHeight : 0f), rightColumnWidth, (ShowEvents ? EventIconLaneHeight : 0f) + SliderHeight), ref changed);

        float controlsTop = Padding + (ShowSeekbar ? SeekbarSectionHeight + SectionGap : 0f);
        Set(ref ControlsTop, controlsTop, ref changed);

        float horizontalRuleTop = controlsTop - SectionGap * 0.5f - RuleThickness * 0.5f;
        Set(ref HorizontalRuleBox, new PlaybackLayoutBox(RightLeft, horizontalRuleTop, rightColumnWidth, RuleThickness), ref changed);

        float statusTickWidth = System.Math.Max(0f, (rightColumnWidth - TransportStatusTickGap) * 0.5f);

        Set(ref TransportStatusBox, new PlaybackLayoutBox(RightLeft, ControlsTop, statusTickWidth, TransportStatusHeight), ref changed);
        Set(ref TransportTickBox, new PlaybackLayoutBox(RightLeft + statusTickWidth + TransportStatusTickGap, ControlsTop, statusTickWidth, TransportStatusHeight), ref changed);

        float transportButtonTop = ControlsTop + TransportStatusHeight + TransportStatusButtonGap;
        Set(ref TransportButtonTop, transportButtonTop, ref changed);
        SetBoxes(ref TransportButtonBoxes, BuildTransportButtonBoxes(transportButtonCount, rightColumnWidth), ref changed);

        return changed;
    }

    private static float GetEventRightColumnWidth(float speedSectionWidth)
    {
        float screenWidth = Main.screenWidth > 0 ? Main.screenWidth : 1280f;
        float occupiedWidth = Padding * 2f;

        if (ShowSpeed)
            occupiedWidth += speedSectionWidth + ColumnGap;

        float availableWidth = screenWidth - 72f - occupiedWidth;
        float preferredWidth = System.Math.Min(1280f, availableWidth);

        return System.Math.Max(RightColumnWidth, preferredWidth);
    }

    internal static void Apply(
        UIElement panel,
        UIElement speedLabel,
        UIElement[] speedButtons,
        UIElement verticalRule,
        UIElement positionLabel,
        UIElement positionSlider,
        UIElement eventMarkerLayer,
        UIElement horizontalRule,
        UIElement transportStatusLabel,
        UIElement transportTickLabel,
        UIElement[] transportButtons)
    {
        Update(speedButtons.Length, transportButtons.Length);

        panel.Width.Set(PanelWidth, 0f);
        panel.Height.Set(PanelHeight, 0f);

        SpeedLabelBox.Apply(speedLabel);

        for (int i = 0; i < speedButtons.Length && i < SpeedButtonBoxes.Length; i++)
            SpeedButtonBoxes[i].Apply(speedButtons[i]);

        VerticalRuleBox.Apply(verticalRule);
        PositionLabelBox.Apply(positionLabel);
        PositionSliderBox.Apply(positionSlider);
        EventMarkerBox.Apply(eventMarkerLayer);
        HorizontalRuleBox.Apply(horizontalRule);
        TransportStatusBox.Apply(transportStatusLabel);
        TransportTickBox.Apply(transportTickLabel);

        for (int i = 0; i < transportButtons.Length && i < TransportButtonBoxes.Length; i++)
            ApplyTransportButton(transportButtons[i], TransportButtonBoxes[i]);
    }

    private static PlaybackLayoutBox[] BuildSpeedButtonBoxes(int count, int columns)
    {
        PlaybackLayoutBox[] boxes = new PlaybackLayoutBox[count];

        float startTop = Padding + SpeedLabelHeight + SpeedLabelButtonGap;

        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;

            float left = Padding + column * (SpeedButtonWidth + SpeedButtonGapX);
            float top = startTop + row * (SpeedButtonHeight + SpeedButtonGapY);

            boxes[i] = new PlaybackLayoutBox(left, top, SpeedButtonWidth, SpeedButtonHeight);
        }

        return boxes;
    }

    private static PlaybackLayoutBox[] BuildTransportButtonBoxes(int count, float rightColumnWidth)
    {
        PlaybackLayoutBox[] boxes = new PlaybackLayoutBox[count];

        float rowWidth = GetTransportButtonRowWidth(count);
        float left = RightLeft + (rightColumnWidth - rowWidth) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float width = GetTransportButtonWidth(i);
            boxes[i] = new PlaybackLayoutBox(left, TransportButtonTop, width, TransportButtonHeight);
            left += width + TransportButtonGap;
        }

        return boxes;
    }

    private static float GetTransportButtonRowWidth(int count)
    {
        if (count <= 0)
            return 0f;

        float width = 0f;

        for (int i = 0; i < count; i++)
            width += GetTransportButtonWidth(i);

        return width + (count - 1) * TransportButtonGap;
    }

    private static float GetTransportButtonWidth(int index)
    {
        if (index >= 0 && index < TransportButtonWidths.Length)
            return TransportButtonWidths[index];

        return TransportButtonHeight;
    }

    private static void ApplyTransportButton(UIElement element, PlaybackLayoutBox box)
    {
        if (element is IconActionButton button)
            button.SetOuterWidth(box.Width);
        else
            element.Width.Set(box.Width, 0f);

        element.Left.Set(box.Left, 0f);
        element.Top.Set(box.Top, 0f);
        element.Height.Set(box.Height, 0f);
    }

    internal static void ApplyDefaultAnchor(UIElement panel)
    {
        Update();

        panel.HAlign = PanelHAlign;
        panel.VAlign = PanelVAlign;
    }

    private static void Set(ref float target, float value, ref bool changed)
    {
        if (System.Math.Abs(target - value) <= 0.01f)
            return;

        target = value;
        changed = true;
    }

    private static void Set(ref int target, int value, ref bool changed)
    {
        if (target == value)
            return;

        target = value;
        changed = true;
    }

    private static void Set(ref bool target, bool value, ref bool changed)
    {
        if (target == value)
            return;

        target = value;
        changed = true;
    }

    private static void Set(ref PlaybackLayoutBox target, PlaybackLayoutBox value, ref bool changed)
    {
        if (target.AlmostEquals(value))
            return;

        target = value;
        changed = true;
    }

    private static void SetArray(ref float[] target, float[] value, ref bool changed)
    {
        if (target != null && target.Length == value.Length)
        {
            bool equal = true;

            for (int i = 0; i < target.Length; i++)
            {
                if (System.Math.Abs(target[i] - value[i]) <= 0.01f)
                    continue;

                equal = false;
                break;
            }

            if (equal)
                return;
        }

        target = value;
        changed = true;
    }

    private static void SetBoxes(ref PlaybackLayoutBox[] target, PlaybackLayoutBox[] value, ref bool changed)
    {
        if (target != null && target.Length == value.Length)
        {
            bool equal = true;

            for (int i = 0; i < target.Length; i++)
            {
                if (target[i].AlmostEquals(value[i]))
                    continue;

                equal = false;
                break;
            }

            if (equal)
                return;
        }

        target = value;
        changed = true;
    }
}

internal readonly record struct PlaybackLayoutBox(float Left, float Top, float Width, float Height)
{
    internal void Apply(UIElement element)
    {
        element.Left.Set(Left, 0f);
        element.Top.Set(Top, 0f);
        element.Width.Set(Width, 0f);
        element.Height.Set(Height, 0f);
    }

    internal bool AlmostEquals(PlaybackLayoutBox other)
    {
        return System.Math.Abs(Left - other.Left) <= 0.01f &&
            System.Math.Abs(Top - other.Top) <= 0.01f &&
            System.Math.Abs(Width - other.Width) <= 0.01f &&
            System.Math.Abs(Height - other.Height) <= 0.01f;
    }
}
