using System;

namespace Reese.Common.MainMenu;

/// <summary>
/// Hot reload layout for <see cref="ReplayBrowserPanel"/>
/// </summary>
internal static class ReplayLayout
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
    internal static float LengthColumnWidth;
    internal static float ModsColumnWidth;
    internal static float SizeColumnWidth;
    internal static float ActionColumnWidth;
    internal static float TableWidth;
    internal static float DateLeft;
    internal static float LengthLeft;
    internal static float ModsLeft;
    internal static float SizeLeft;
    internal static float StatColumnPadding;
    internal static float PreviewColumnWidth;

    internal static bool Update()
    {
        bool changed = false;

        Set(ref PreviewColumnWidth, 60f, ref changed);
        Set(ref StatColumnPadding, 6f, ref changed);
        Set(ref ReplayItemHeight, 58f, ref changed);
        Set(ref ReplayItemActionHeight, 32f, ref changed);
        Set(ref ReplayItemTotalHeight, ReplayItemHeight + ReplayItemActionHeight, ref changed);
        Set(ref ActionButtonSize, 22f, ref changed);
        Set(ref ActionButtonGap, 3f, ref changed);
        Set(ref ActionButtonRightPadding, 4f, ref changed);
        Set(ref ActionLabelGap, 8f, ref changed);
        Set(ref ActionLabelWidth, 180f, ref changed);
        Set(ref ScrollbarWidth, 20f, ref changed);
        Set(ref ContentPadding, 6f, ref changed);
        Set(ref TableColumnHeight, 28f, ref changed);
        Set(ref ListTop, TableColumnHeight + 4f, ref changed);
        Set(ref ActionColumnWidth, 48f, ref changed);
        Set(ref TableWidth, ReplayBrowser.PanelWidth - ContentPadding * 2f - ScrollbarWidth - 4f, ref changed);

        const float baseNameColumnWidth = 150f;
        const float baseDateColumnWidth = 108f;
        const float baseDurationColumnWidth = 72f;
        const float baseModsColumnWidth = 68f;
        const float baseSizeColumnWidth = 92f;

        float baseTotalWidth = baseNameColumnWidth + baseDateColumnWidth + baseDurationColumnWidth + baseModsColumnWidth + baseSizeColumnWidth;
        float fittedNameColumnWidth = MathF.Round(baseNameColumnWidth / baseTotalWidth * TableWidth);
        float fittedDateColumnWidth = MathF.Round(baseDateColumnWidth / baseTotalWidth * TableWidth);
        float fittedDurationColumnWidth = MathF.Round(baseDurationColumnWidth / baseTotalWidth * TableWidth);
        float fittedModsColumnWidth = MathF.Round(baseModsColumnWidth / baseTotalWidth * TableWidth);
        float fittedSizeColumnWidth = TableWidth - fittedNameColumnWidth - fittedDateColumnWidth - fittedDurationColumnWidth - fittedModsColumnWidth;

        Set(ref NameColumnWidth, fittedNameColumnWidth, ref changed);
        Set(ref DateColumnWidth, fittedDateColumnWidth, ref changed);
        Set(ref LengthColumnWidth, fittedDurationColumnWidth, ref changed);
        Set(ref ModsColumnWidth, fittedModsColumnWidth, ref changed);
        Set(ref SizeColumnWidth, fittedSizeColumnWidth, ref changed);

        Set(ref DateLeft, NameColumnWidth, ref changed);
        Set(ref LengthLeft, DateLeft + DateColumnWidth, ref changed);
        Set(ref ModsLeft, LengthLeft + LengthColumnWidth, ref changed);
        Set(ref SizeLeft, ModsLeft + ModsColumnWidth, ref changed);

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