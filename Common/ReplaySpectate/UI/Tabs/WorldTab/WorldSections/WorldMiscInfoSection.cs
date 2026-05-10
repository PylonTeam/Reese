using Microsoft.Xna.Framework.Graphics;
using Reese.Common.ReplaySpectate.SpectatorMode;
using Reese.Common.ReplaySpectate.UI.Tabs.WorldTab;
using System.Collections.Generic;

namespace Reese.Common.ReplaySpectate.UI.Tabs.WorldTab.WorldSections;

internal sealed class WorldMiscInfoSection : WorldSectionBase
{
    public override WorldSection Section => WorldSection.MiscInfo;
    public override string HeaderText => "Misc info";
    public override float Height => 112f;
    public override bool UsesCommonRowTooltips => true;

    public override IReadOnlyList<WorldSectionRow> GetRows()
    {
        return
        [
            new(GetPlayersOnlineText(), GetPlayersOnlineText, GetPlayersOnlineTexture, iconScale: 1f),
            new(GetSpectatorsOnlineText(), GetSpectatorsOnlineText, GetSpectatorsOnlineTexture, iconScale: 0.75f)
        ];
    }

    private static string GetPlayersOnlineText() => "Players Online: " + SpectatorModeSystem.GetPlayersOnlineCount();

    private static string GetSpectatorsOnlineText() => "Spectators Online: " + SpectatorModeSystem.GetSpectatorCount();

    private static Texture2D GetPlayersOnlineTexture() => Ass.Icon_PlayerHead.Value;

    private static Texture2D GetSpectatorsOnlineTexture() => Ass.GhostRight.Value;
}
