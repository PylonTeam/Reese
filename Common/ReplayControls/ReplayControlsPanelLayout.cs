using System;

namespace Reese.Common.ReplayControls;

internal static class ReplayControlsPanelLayout
{
    internal static float PanelWidth;
    internal static float PanelHeight;
    internal static float InitialHAlign;
    internal static float InitialVAlign;
    internal static float HeaderHeight;
    internal static float ContentHeight;
    internal static float PositionLabelLeft;
    internal static float PositionLabelTop;
    internal static float PositionSliderLeft;
    internal static float PositionSliderTop;
    internal static float PositionSliderRightPadding;
    internal static float PositionSliderHeight;
    internal static float HorizontalRuleLeft;
    internal static float HorizontalRuleTop;
    internal static float HorizontalRuleRightPadding;
    internal static float HorizontalRuleHeight;
    internal static float SpeedLabelLeft;
    internal static float SpeedLabelTop;
    internal static float SpeedButtonLeft;
    internal static float SpeedButtonTop;
    internal static float SpeedButtonWidth;
    internal static float SpeedButtonHeight;
    internal static float SpeedButtonStride;
    internal static float DividerLeft;
    internal static float DividerTop;
    internal static float DividerWidth;
    internal static float DividerHeight;
    internal static float TransportLeft;
    internal static float TransportTop;
    internal static float TransportGap;
    internal static float TransportButtonWidth;
    internal static float TransportPlayButtonWidth;

    internal static bool Update()
    {
        bool changed = false;

        Set(ref PanelWidth, 760f, ref changed);
        Set(ref PanelHeight, 152f, ref changed);
        Set(ref InitialHAlign, 0.5f, ref changed);
        Set(ref InitialVAlign, 0.92f, ref changed);
        Set(ref HeaderHeight, 32f, ref changed);
        Set(ref ContentHeight, PanelHeight - HeaderHeight, ref changed);
        Set(ref PositionLabelLeft, 12f, ref changed);
        Set(ref PositionLabelTop, 10f, ref changed);
        Set(ref PositionSliderLeft, 168f, ref changed);
        Set(ref PositionSliderTop, 11f, ref changed);
        Set(ref PositionSliderRightPadding, 12f, ref changed);
        Set(ref PositionSliderHeight, 20f, ref changed);
        Set(ref HorizontalRuleLeft, 12f, ref changed);
        Set(ref HorizontalRuleTop, 38f, ref changed);
        Set(ref HorizontalRuleRightPadding, 12f, ref changed);
        Set(ref HorizontalRuleHeight, 2f, ref changed);
        Set(ref SpeedLabelLeft, 12f, ref changed);
        Set(ref SpeedLabelTop, 50f, ref changed);
        Set(ref SpeedButtonLeft, 14f, ref changed);
        Set(ref SpeedButtonTop, 78f, ref changed);
        Set(ref SpeedButtonWidth, 46f, ref changed);
        Set(ref SpeedButtonHeight, 24f, ref changed);
        Set(ref SpeedButtonStride, 58f, ref changed);
        Set(ref DividerLeft, 312f, ref changed);
        Set(ref DividerTop, 42f, ref changed);
        Set(ref DividerWidth, 2f, ref changed);
        Set(ref DividerHeight, 70f, ref changed);
        Set(ref TransportLeft, 334f, ref changed);
        Set(ref TransportTop, 55f, ref changed);
        Set(ref TransportGap, 14f, ref changed);
        Set(ref TransportButtonWidth, 42f, ref changed);
        Set(ref TransportPlayButtonWidth, 48f, ref changed);

        return changed;
    }

    private static void Set(ref float target, float value, ref bool changed)
    {
        if (Math.Abs(target - value) <= 0.01f)
            return;

        target = value;
        changed = true;
    }
}
