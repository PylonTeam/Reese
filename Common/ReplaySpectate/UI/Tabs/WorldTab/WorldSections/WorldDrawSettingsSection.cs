using Microsoft.Xna.Framework.Graphics;
using Reese.Common.ReplaySpectate.Hooks;
using System.Collections.Generic;

namespace Reese.Common.ReplaySpectate.UI.Tabs.WorldTab.WorldSections;

internal sealed class WorldDrawSettingsSection : WorldSectionBase
{
    public override WorldSection Section => WorldSection.Settings;
    public override string HeaderText => "Draw Settings";
    public override float Height => 214f;

    public override IReadOnlyList<WorldSectionRow> GetRows()
    {
        return
        [
            new("Players:", () => $"Fullbright: {OnOff(FullbrightSpectatorSystem.Enabled)}", GetFullbrightIcon, onLeftClick: () => FullbrightSpectatorSystem.Enabled = !FullbrightSpectatorSystem.Enabled),
            new("NPCs:", () => $"Reveal Map: {OnOff(MapRevealHelper.Revealed)}", GetRevealMapIcon, onLeftClick: () => MapRevealHelper.SetRevealed(!MapRevealHelper.Revealed)),
            new("Projectiles:", () => $"Draw Players: {SpectatorClientSettings.DrawPlayersLabel}", GetDrawPlayersIcon, onLeftClick: SpectatorClientSettings.CycleDrawPlayers),
            new("Items:", () => $"Auto Director: {OnOff(AutoDirectorSystem.Enabled)}", GetAutoDirectorIcon, onLeftClick: () => AutoDirectorSystem.Enabled = !AutoDirectorSystem.Enabled)
        ];
    }

    private static string OnOff(bool value) => value ? "On" : "Off";

    private static Texture2D GetFullbrightIcon() => FullbrightSpectatorSystem.Enabled ? Ass.Icon_CandelabraOn.Value : Ass.Icon_CandelabraOff.Value;

    private static Texture2D GetRevealMapIcon() => MapRevealHelper.Revealed ? Ass.Icon_MapOn.Value : Ass.Icon_MapOff.Value;

    private static Texture2D GetDrawPlayersIcon()
    {
        return SpectatorClientSettings.DrawPlayers switch
        {
            SpectatorPlayerDrawMode.FullPlayer => Ass.Icon_Player.Value,
            _ => null
        };
    }

    private static Texture2D GetPlayerCardsIcon()
    {
        return SpectatorControlsPanel.ShownPlayerCardCount switch
        {
            1 => Ass.Icon_Card1.Value,
            2 => Ass.Icon_Card2.Value,
            3 => Ass.Icon_Card3.Value,
            _ => Ass.Icon_Card1.Value
        };
    }

    private static Texture2D GetAutoDirectorIcon() => AutoDirectorSystem.Enabled ? Ass.Icon_FilmProjectorOn.Value : Ass.Icon_FilmProjectorOff.Value;
}
