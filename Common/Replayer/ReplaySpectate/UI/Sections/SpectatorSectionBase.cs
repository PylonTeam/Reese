using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Reese.Common.Replayer.ReplaySpectate.UI.Sections;

internal abstract class SpectatorSectionBase
{
    public abstract string HeaderText { get; }
    public abstract float Height { get; }
    public virtual bool UsesCommonRowTooltips => false;
    public virtual bool UsesOptionRowStyle => false;

    public abstract IReadOnlyList<SpectatorSectionRow> GetRows();
}

internal readonly struct SpectatorSectionRow
{
    public readonly string Label;
    public readonly Func<string> GetText;
    public readonly Func<Texture2D> GetIcon;
    public readonly Func<Color> GetTextColor;
    public readonly Action OnLeftClick;
    public readonly Action OnRightClick;
    public readonly string Tooltip;
    public readonly float IconScale;
    public readonly bool? IsOptionOverride;

    public SpectatorSectionRow(string label, Func<string> getText, Func<Texture2D> getIcon = null, Func<Color> getTextColor = null, Action onLeftClick = null, Action onRightClick = null, string tooltip = null, float iconScale = 1f, bool? isOptionOverride = null)
    {
        Label = label;
        GetText = getText;
        GetIcon = getIcon;
        GetTextColor = getTextColor;
        OnLeftClick = onLeftClick;
        OnRightClick = onRightClick;
        Tooltip = tooltip;
        IconScale = iconScale;
        IsOptionOverride = isOptionOverride;
    }
}

internal abstract class SettingsSection : SpectatorSectionBase
{
    public override bool UsesOptionRowStyle => true;

    protected static string OnOff(bool value)
    {
        return value ? "On" : "Off";
    }
}

internal abstract class InfoSection : SpectatorSectionBase
{
    public override bool UsesCommonRowTooltips => true;
    public override bool UsesOptionRowStyle => false;
}