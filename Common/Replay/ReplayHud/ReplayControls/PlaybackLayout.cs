using Reese.Common.Replay.ReplayHud.Shared.UI;
using Terraria.UI;

namespace Reese.Common.Replay.ReplayHud.ReplayControls;

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
    internal static float EventIconLaneHeight;
    internal static float SliderHeight;

    internal static float ControlsSectionHeight;
    internal static float TransportStatusHeight;
    internal static float TransportStatusButtonGap;
    internal static float TransportStatusRowGap;
    internal static float TransportStatusColumnWidth;
    internal static float TransportButtonHeight;
    internal static float TransportButtonGap;
    internal static float[] TransportButtonWidths;

    internal static float RuleThickness;
    internal static float RuleAlpha;

    internal static bool ShowSpeed;
    internal static bool ShowSeekbar;
    internal static bool ShowControls;
    internal static bool ShowRightColumn;

    internal static float InnerWidth;
    internal static float InnerHeight;
    internal static float RightLeft;
    internal static float ControlsTop;
    internal static float TransportButtonTop;
    internal static float TransportButtonRowWidth;
    internal static float StatusColumnLeft;

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
        Set(ref ColumnGap, 18f, ref changed);
        Set(ref SectionGap, 12f, ref changed);

        Set(ref SpeedColumnWidth, 220f, ref changed);
        Set(ref SpeedLabelHeight, 22f, ref changed);
        Set(ref SpeedLabelButtonGap, 6f, ref changed);
        Set(ref SpeedButtonColumns, 5, ref changed);
        Set(ref SpeedButtonWidth, 40f, ref changed);
        Set(ref SpeedButtonHeight, 28f, ref changed);
        Set(ref SpeedButtonGapX, 5f, ref changed);
        Set(ref SpeedButtonGapY, 4f, ref changed);

        Set(ref RightColumnWidth, 386f, ref changed);
        Set(ref RightColumnAutoFitControls, true, ref changed);

        Set(ref EventIconLaneHeight, 36f, ref changed);
        Set(ref SliderHeight, 18f, ref changed);

        Set(ref TransportStatusHeight, 22f, ref changed);
        Set(ref TransportStatusButtonGap, 0f, ref changed);
        Set(ref TransportStatusRowGap, 5f, ref changed);
        Set(ref TransportStatusColumnWidth, 200f, ref changed);
        Set(ref TransportButtonHeight, 36f, ref changed);
        Set(ref TransportButtonGap, 6f, ref changed);

        Set(ref RuleThickness, 2f, ref changed);
        Set(ref RuleAlpha, 0.18f, ref changed);

        SetArray(ref TransportButtonWidths, [44f, 36f, 44f, 60f, 36f, 44f, 36f, 44f], ref changed);

        Set(ref ShowSpeed, ReplayClientSettings.ShowReplayHudSpeed, ref changed);
        Set(ref ShowSeekbar, ReplayClientSettings.ShowReplayHudSeekbar, ref changed);
        Set(ref ShowControls, ReplayClientSettings.ShowReplayHudPlaybackControls, ref changed);
        Set(ref ShowRightColumn, ShowSeekbar || ShowControls, ref changed);

        int speedColumns = System.Math.Max(1, SpeedButtonColumns);
        int firstSpeedRowCount = GetFirstSpeedRowCount(speedButtonCount, speedColumns);
        int secondSpeedRowCount = System.Math.Max(0, speedButtonCount - firstSpeedRowCount);
        int maxSpeedRowCount = System.Math.Max(firstSpeedRowCount, secondSpeedRowCount);
        int speedRows = speedButtonCount <= 0 ? 0 : secondSpeedRowCount > 0 ? 2 : 1;

        float speedButtonGridWidth = maxSpeedRowCount * SpeedButtonWidth + System.Math.Max(0, maxSpeedRowCount - 1) * SpeedButtonGapX;
        float speedButtonGridHeight = speedRows * SpeedButtonHeight + System.Math.Max(0, speedRows - 1) * SpeedButtonGapY;
        float speedSectionWidth = System.Math.Max(SpeedColumnWidth, speedButtonGridWidth);
        float speedSectionHeight = SpeedLabelHeight + SpeedLabelButtonGap + speedButtonGridHeight;

        Set(ref SpeedSectionHeight, speedSectionHeight, ref changed);

        float seekbarSectionHeight = EventIconLaneHeight + SliderHeight;
        Set(ref SeekbarSectionHeight, seekbarSectionHeight, ref changed);

        float statusSectionHeight = TransportStatusHeight * 3f + TransportStatusRowGap * 2f;

        float transportButtonRowWidth = GetTransportButtonRowWidth(transportButtonCount);
        Set(ref TransportButtonRowWidth, transportButtonRowWidth, ref changed);

        float requestedPanelWidth = ReplayClientSettings.GetPlaybackHudWidth();
        float requestedInnerWidth = System.Math.Max(0f, requestedPanelWidth - Padding * 2f);

        float controlsSectionHeight = 0f;
        if (ShowSpeed)
            controlsSectionHeight = System.Math.Max(controlsSectionHeight, speedSectionHeight);

        if (ShowControls)
        {
            controlsSectionHeight = System.Math.Max(controlsSectionHeight, TransportButtonHeight);
            controlsSectionHeight = System.Math.Max(controlsSectionHeight, statusSectionHeight);
        }

        Set(ref ControlsSectionHeight, controlsSectionHeight, ref changed);

        float controlsSectionWidth = 0f;
        bool hasControlsContent = false;

        if (ShowSpeed)
        {
            controlsSectionWidth += speedSectionWidth;
            hasControlsContent = true;
        }

        if (ShowControls)
        {
            if (hasControlsContent)
                controlsSectionWidth += ColumnGap;

            controlsSectionWidth += transportButtonRowWidth;
            controlsSectionWidth += ColumnGap;
            controlsSectionWidth += TransportStatusColumnWidth;
            hasControlsContent = true;
        }

        float topSectionWidth = ShowSeekbar ? RightColumnWidth : 0f;
        float innerWidth = System.Math.Max(System.Math.Max(requestedInnerWidth, controlsSectionWidth), topSectionWidth);

        float innerHeight = 0f;
        if (ShowSeekbar)
            innerHeight += SeekbarSectionHeight;

        if (ShowSeekbar && hasControlsContent)
            innerHeight += SectionGap;

        if (hasControlsContent)
            innerHeight += controlsSectionHeight;

        Set(ref InnerWidth, innerWidth, ref changed);
        Set(ref InnerHeight, innerHeight, ref changed);
        Set(ref PanelWidth, System.Math.Max(PanelMinWidth, innerWidth + Padding * 2f), ref changed);
        Set(ref PanelHeight, System.Math.Max(PanelMinHeight, innerHeight + Padding * 2f), ref changed);

        float controlsTop = Padding + (ShowSeekbar ? SeekbarSectionHeight + (hasControlsContent ? SectionGap : 0f) : 0f);
        Set(ref ControlsTop, controlsTop, ref changed);

        float speedLeft = ShowSpeed && ShowControls
            ? Padding
            : Padding + (innerWidth - speedSectionWidth) * 0.5f;
        float transportLeft = ShowSpeed && ShowControls
            ? Padding + (innerWidth - transportButtonRowWidth) * 0.5f
            : Padding;
        float statusLeft = ShowControls
            ? Padding + innerWidth - TransportStatusColumnWidth
            : Padding;

        Set(ref RightLeft, Padding, ref changed);
        Set(ref StatusColumnLeft, statusLeft, ref changed);

        Set(ref SpeedLabelBox, new PlaybackLayoutBox(speedLeft, ControlsTop, speedSectionWidth, SpeedLabelHeight), ref changed);
        SetBoxes(ref SpeedButtonBoxes, BuildSpeedButtonBoxes(speedButtonCount, speedLeft, ControlsTop, speedSectionWidth), ref changed);

        Set(ref VerticalRuleBox, new PlaybackLayoutBox(0f, 0f, 0f, 0f), ref changed);

        float sliderTop = Padding + EventIconLaneHeight;
        Set(ref PositionSliderBox, new PlaybackLayoutBox(Padding, sliderTop, innerWidth, SliderHeight), ref changed);
        Set(ref EventMarkerBox, new PlaybackLayoutBox(Padding, Padding, innerWidth, EventIconLaneHeight + SliderHeight), ref changed);

        float horizontalRuleTop = controlsTop - SectionGap * 0.5f - RuleThickness * 0.5f;
        Set(ref HorizontalRuleBox, new PlaybackLayoutBox(Padding, horizontalRuleTop, innerWidth, RuleThickness), ref changed);

        float statusTop = ControlsTop + (controlsSectionHeight - statusSectionHeight) * 0.5f;
        Set(ref TransportStatusBox, new PlaybackLayoutBox(StatusColumnLeft, statusTop, TransportStatusColumnWidth, TransportStatusHeight), ref changed);
        Set(ref PositionLabelBox, new PlaybackLayoutBox(StatusColumnLeft, statusTop + TransportStatusHeight + TransportStatusRowGap, TransportStatusColumnWidth, TransportStatusHeight), ref changed);
        Set(ref TransportTickBox, new PlaybackLayoutBox(StatusColumnLeft, statusTop + (TransportStatusHeight + TransportStatusRowGap) * 2f, TransportStatusColumnWidth, TransportStatusHeight), ref changed);

        float transportButtonTop = ControlsTop + (controlsSectionHeight - TransportButtonHeight) * 0.5f;
        Set(ref TransportButtonTop, transportButtonTop, ref changed);
        SetBoxes(ref TransportButtonBoxes, BuildTransportButtonBoxes(transportButtonCount, transportLeft), ref changed);

        return changed;
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

    private static PlaybackLayoutBox[] BuildSpeedButtonBoxes(int count, float sectionLeft, float sectionTop, float sectionWidth)
    {
        PlaybackLayoutBox[] boxes = new PlaybackLayoutBox[count];
        int firstRowCount = GetFirstSpeedRowCount(count, SpeedButtonColumns);
        float firstRowWidth = GetSpeedButtonRowWidth(firstRowCount);
        int secondRowCount = System.Math.Max(0, count - firstRowCount);
        float secondRowWidth = GetSpeedButtonRowWidth(secondRowCount);

        float startTop = sectionTop + SpeedLabelHeight + SpeedLabelButtonGap;

        for (int i = 0; i < count; i++)
        {
            bool firstRow = i < firstRowCount;
            int column = firstRow ? i : i - firstRowCount;
            float rowWidth = firstRow ? firstRowWidth : secondRowWidth;

            float left = sectionLeft + (sectionWidth - rowWidth) * 0.5f + column * (SpeedButtonWidth + SpeedButtonGapX);
            float top = startTop + (firstRow ? 0f : SpeedButtonHeight + SpeedButtonGapY);

            boxes[i] = new PlaybackLayoutBox(left, top, SpeedButtonWidth, SpeedButtonHeight);
        }

        return boxes;
    }

    private static int GetFirstSpeedRowCount(int count, int columns)
    {
        if (count <= columns)
            return count;

        return System.Math.Max(1, count - columns);
    }

    private static float GetSpeedButtonRowWidth(int count)
    {
        if (count <= 0)
            return 0f;

        return count * SpeedButtonWidth + (count - 1) * SpeedButtonGapX;
    }

    private static PlaybackLayoutBox[] BuildTransportButtonBoxes(int count, float left)
    {
        PlaybackLayoutBox[] boxes = new PlaybackLayoutBox[count];

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
