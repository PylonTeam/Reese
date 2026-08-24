using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replay.ReplayHud;
using System;
using System.Collections.Generic;
using Terraria.Localization;

namespace Reese.Common.Replay.ReplayHud.Shared.Sections;

internal readonly struct SliderRowConfig
{
    public readonly Func<float> GetRatio;
    public readonly Action<float> SetRatio;

    public SliderRowConfig(Func<float> getRatio, Action<float> setRatio)
    {
        GetRatio = getRatio;
        SetRatio = setRatio;
    }
}

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
    public readonly SliderRowConfig? Slider;

    public SpectatorSectionRow(string label, Func<string> getText, Func<Texture2D> getIcon = null, Func<Color> getTextColor = null, Action onLeftClick = null, Action onRightClick = null, string tooltip = null, float iconScale = 1f, bool? isOptionOverride = null, SliderRowConfig? slider = null)
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
        Slider = slider;
    }
}

internal abstract class SettingsSection : SpectatorSectionBase
{
    public override bool UsesOptionRowStyle => true;

    protected static string OnOff(bool value) => Language.GetTextValue(value ? "LegacyInterface.72" : "LegacyInterface.73");
}

internal abstract class InfoSection : SpectatorSectionBase
{
    public override bool UsesCommonRowTooltips => true;
    public override bool UsesOptionRowStyle => false;
}
