using System;

namespace Reese.Common.MainMenu;

internal static class ReplayBrowserLayout
{
    internal static float ReplayItemHeight;
    internal static float ReplayItemActionHeight;
    internal static float ReplayItemTotalHeight;
    internal static float ActionButtonSize;
    internal static float ActionButtonGap;
    internal static float ActionButtonRightPadding;
    internal static float ActionLabelGap;
    internal static float ActionLabelWidth;
    internal static float ScrollbarWidth;
    internal static float ContentPadding;
    internal static float TableColumnHeight;
    internal static float ListTop;
    internal static float NameColumnWidth;
    internal static float DateColumnWidth;
    internal static float DurationColumnWidth;
    internal static float ActionColumnWidth;
    internal static float TableWidth;
    internal static float DateLeft;
    internal static float DurationLeft;

    internal static bool Update()
    {
        bool changed = false;

        Set(ref ReplayItemHeight, 62f, ref changed);
        Set(ref ReplayItemActionHeight, 30f, ref changed);
        Set(ref ReplayItemTotalHeight, ReplayItemHeight + ReplayItemActionHeight, ref changed);
        Set(ref ActionButtonSize, 22f, ref changed);
        Set(ref ActionButtonGap, 4f, ref changed);
        Set(ref ActionButtonRightPadding, 4f, ref changed);
        Set(ref ActionLabelGap, 8f, ref changed);
        Set(ref ActionLabelWidth, 180f, ref changed);
        Set(ref ScrollbarWidth, 20f, ref changed);
        Set(ref ContentPadding, 6f, ref changed);
        Set(ref TableColumnHeight, 28f, ref changed);
        Set(ref ListTop, TableColumnHeight + 4f, ref changed);
        Set(ref NameColumnWidth, 246f, ref changed);
        Set(ref DateColumnWidth, 124f, ref changed);
        Set(ref ActionColumnWidth, 48f, ref changed);
        Set(ref TableWidth, ReplayBrowser.PanelWidth - ContentPadding * 2f - ScrollbarWidth - 4f, ref changed);
        Set(ref DurationColumnWidth, Math.Max(80f, TableWidth - NameColumnWidth - DateColumnWidth), ref changed);
        Set(ref DateLeft, NameColumnWidth, ref changed);
        Set(ref DurationLeft, NameColumnWidth + DateColumnWidth, ref changed);

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
