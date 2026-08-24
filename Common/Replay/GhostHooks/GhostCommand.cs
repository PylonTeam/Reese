using Reese.Core.Compat;
using Reese.Common.Spectator;
using System;
using System.Collections.Generic;

namespace Reese.Common.Replay.GhostHooks;

internal class GhostCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat | CommandType.World | CommandType.Server;
    public override string Command => "ghost";
    public override string Description => "Change ghost mode for yourself or other players.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        Player callerPlayer = caller.Player;
        Player target = callerPlayer;
        bool isSelf = true;

        if (args.Length > 0)
        {
            string name = string.Join(" ", args);
            if (!TryFindPlayer(name, out target, out string error))
            {
                caller.Reply(error, Color.OrangeRed);
                return;
            }
            isSelf = callerPlayer?.active == true && target.whoAmI == callerPlayer.whoAmI;
        }
        else if (target?.active != true)
        {
            caller.Reply("Usage: /ghost <player name>", Color.OrangeRed);
            return;
        }

        if (!SpectatorModeSystem.CanAdminSetMode(caller, target, out string errorMessage))
        {
            caller.Reply(errorMessage, Color.OrangeRed);
            return;
        }

        TogglePlayer(caller, target, self: isSelf);
    }

    private static void TogglePlayer(CommandCaller caller, Player player, bool self)
    {
        if (player?.active != true)
        {
            caller.Reply("Player not found.", Color.OrangeRed);
            return;
        }

        SpectateMode oldMode = SpectatorModeSystem.GetMode(player.whoAmI);
        SpectateMode newMode = oldMode == SpectateMode.Player ? SpectateMode.Spectator : SpectateMode.Player;

        if (Main.netMode == Terraria.ID.NetmodeID.Server)
            SpectatorModeSystem.SetModeServer(player.whoAmI, newMode);
        else
            SpectatorModeSystem.RequestSetMode(player.whoAmI, newMode);

        string targetText = self ? "your" : $"{player.name}'s";
        caller.Reply($"Set {targetText} ghost mode to {newMode == SpectateMode.Spectator}.", Color.GreenYellow);
    }

    private static bool TryFindPlayer(string name, out Player player, out string error)
    {
        player = null;
        error = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Usage: /ghost or /ghost <player name>";
            return false;
        }

        Player exactMatch = null;
        List<Player> partialMatches = [];

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player candidate = Main.player[i];

            if (candidate?.active != true)
                continue;

            if (string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase))
            {
                exactMatch = candidate;
                break;
            }

            if (candidate.name.Contains(name, StringComparison.OrdinalIgnoreCase))
                partialMatches.Add(candidate);
        }

        if (exactMatch is not null)
        {
            player = exactMatch;
            return true;
        }

        if (partialMatches.Count == 1)
        {
            player = partialMatches[0];
            return true;
        }

        if (partialMatches.Count > 1)
        {
            error = "Multiple players matched that name. Use the full player name.";
            return false;
        }

        error = $"No active player found matching \"{name}\".";
        return false;
    }
}
