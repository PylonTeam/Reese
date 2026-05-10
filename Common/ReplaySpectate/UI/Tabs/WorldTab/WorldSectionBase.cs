using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria.ID;

namespace Reese.Common.ReplaySpectate.UI.Tabs.WorldTab;

internal abstract class WorldSectionBase
{
    public abstract WorldSection Section { get; }
    public abstract string HeaderText { get; }
    public abstract float Height { get; }
    public virtual bool UsesCommonRowTooltips => false;

    public abstract IReadOnlyList<WorldSectionRow> GetRows();

    public virtual IReadOnlyList<WorldBossEntry> GetBosses()
    {
        return [];
    }
}

internal enum WorldSection
{
    Settings,
    WorldInfo,
    BossesDefeated
}

internal readonly struct WorldSectionRow
{
    public readonly string Label;
    public readonly Func<string> GetText;
    public readonly Func<Texture2D> GetIcon;
    public readonly Func<Color> GetTextColor;
    public readonly Action OnLeftClick;
    public readonly Action OnRightClick;
    public readonly string Tooltip;
    public readonly float IconScale;

    public WorldSectionRow(string label, Func<string> getText, Func<Texture2D> getIcon = null, Func<Color> getTextColor = null, Action onLeftClick = null, Action onRightClick = null, string tooltip = null, float iconScale = 1f)
    {
        Label = label;
        GetText = getText;
        GetIcon = getIcon;
        GetTextColor = getTextColor;
        OnLeftClick = onLeftClick;
        OnRightClick = onRightClick;
        Tooltip = tooltip;
        IconScale = iconScale;
    }
}

internal readonly struct WorldBossEntry
{
    public readonly int NpcId;
    public readonly string Name;
    public readonly bool Downed;

    public WorldBossEntry(int npcId, string name, bool downed)
    {
        NpcId = npcId;
        Name = name;
        Downed = downed;
    }

    public int HeadNpcId => NpcId == NPCID.Golem ? NPCID.GolemHead : NpcId;
}
