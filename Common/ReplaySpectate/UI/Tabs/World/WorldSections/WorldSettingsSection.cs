using GhostSpectating.Common;
using Microsoft.Xna.Framework.Graphics;
using Reese.Common.ReplaySpectate.Hooks;
using Reese.Common.ReplaySpectate.UI;
using Reese.Common.ReplaySpectate.UI.Tabs.World;
using System.Collections.Generic;
using Terraria.GameContent;

namespace Reese.Common.ReplaySpectate.UI.Tabs.World.WorldSections;

internal sealed class WorldSettingsSection : WorldSectionBase
{
    public override WorldSection Section => WorldSection.Settings;
    public override string HeaderText => "Spectator Settings";
    public override float Height => 214f;

    public override IReadOnlyList<WorldSectionRow> GetRows()
    {
        return
        [
            new("Fullbright:", () => $"Fullbright: {OnOff(FullbrightSpectatorSystem.Enabled)}", GetFullbrightIcon, onLeftClick: () => FullbrightSpectatorSystem.Enabled = !FullbrightSpectatorSystem.Enabled),
            new("Reveal Map:", () => $"Reveal Map: {OnOff(MapRevealHelper.Revealed)}", GetRevealMapIcon, onLeftClick: () => MapRevealHelper.SetRevealed(!MapRevealHelper.Revealed)),
            new("Draw Players:", () => $"Draw Players: {SpectatorClientSettings.DrawPlayersLabel}", GetDrawPlayersIcon, onLeftClick: SpectatorClientSettings.CycleDrawPlayers),
            new(
                "Player Cards:",
                () => $"Player Cards: {SpectatorControlsPanel.ShownPlayerCardCount}",
                GetPlayerCardsIcon,
                onLeftClick: () => SpectatorControlsPanel.ChangeShownPlayerCards(1),
                onRightClick: () => SpectatorControlsPanel.ChangeShownPlayerCards(-1),
                tooltip: "Left click to increase number of cards shown\nRight click to decrease"),
            new("Auto Director:", () => $"Auto Director: {OnOff(AutoDirectorSystem.Enabled)}", GetAutoDirectorIcon, onLeftClick: () => AutoDirectorSystem.Enabled = !AutoDirectorSystem.Enabled)
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
            SpectatorPlayerDrawMode.PlayerHeads => Ass.Icon_PlayerHead.Value,
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
