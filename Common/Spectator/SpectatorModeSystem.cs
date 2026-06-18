using Reese.Common.Replayer.ReplayHud.Ghost;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate;
using Reese.Core.Compat;
using Reese.Core.Configs;
using System.Collections.Generic;
using Terraria.ID;

namespace Reese.Common.Spectator;

/// <summary>
/// Allows players to be in either player mode or spectator mode, where they can spectate other players and have a free camera.
/// </summary>
public enum SpectateMode : byte
{
    Player,
    Spectator
}

/// <summary>
/// Handles the spectator mode system, including setting the player to be a spectator or 
/// </summary>
[Autoload(Side = ModSide.Both)]
internal sealed class SpectatorModeSystem : ModSystem
{
    internal static readonly Dictionary<int, SpectateMode> Modes = [];
    private static readonly HashSet<int> pendingJoinChoices = [];

    public static SpectateMode GetMode(int slot) => Modes.TryGetValue(slot, out SpectateMode mode) ? mode : SpectateMode.Player;

    public static bool IsLocalPlayerInSpectateMode => IsInSpectateMode(Main.LocalPlayer);

    public static bool IsInSpectateMode(Player player) => player?.active == true && GetMode(player.whoAmI) == SpectateMode.Spectator;

    public static bool IsInPlayerMode(Player player) => player?.active == true && GetMode(player.whoAmI) == SpectateMode.Player;

    public static int GetPlayersOnlineCount()
    {
        int count = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i]?.active == true)
                count++;

        return count;
    }

    public static int GetSpectatorCount()
    {
        int count = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i]?.active == true && GetMode(i) == SpectateMode.Spectator)
                count++;

        return count;
    }

    private static bool IsAdmin(Player player)
    {
        return player?.active == true && (DragonLensIntegration.IsPlayerDragonLensAdmin(player) || ErkySSCIntegration.IsPlayerErkySSCAdmin(player));
    }

    private static ServerConfig.GhostSpectatingConfig GhostSpectatingConfig => ModContent.GetInstance<ServerConfig>()?.ghostSpectatingConfig;

    internal static bool IsGhostSpectatingEnabled => GhostSpectatingConfig?.IsGhostSpectatingEnabled == true;
    internal static bool ForceSpectateWhenJoining => IsGhostSpectatingEnabled && GhostSpectatingConfig?.ForceSpectateWhenJoining == true;
    internal static bool ShowSpectatorJoinPanel => IsGhostSpectatingEnabled && GhostSpectatingConfig?.ShowSpectatorJoinPanel == true;

    internal static SpectateMode GetJoinDefaultMode()
    {
        return ForceSpectateWhenJoining || ShowSpectatorJoinPanel ? SpectateMode.Spectator : SpectateMode.Player;
    }

    public static void ToggleSpectateMode(int slot)
    {
        if (slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active)
            return;

        SpectateMode currentMode = GetMode(slot);
        SpectateMode newMode = currentMode == SpectateMode.Player ? SpectateMode.Spectator : SpectateMode.Player;
        RequestSetMode(slot, newMode);
    }

    public static void RequestSetLocalMode(SpectateMode mode)
    {
        if (Main.myPlayer is < 0 or >= Main.maxPlayers)
            return;

        RequestSetMode(Main.myPlayer, mode);
    }

    public static void RequestSetLocalModeFromJoinPanel(SpectateMode mode)
    {
        if (Main.myPlayer is < 0 or >= Main.maxPlayers)
            return;

        RequestSetMode(Main.myPlayer, mode, SpectatorModeNetHandler.SetModeReason.JoinPanelChoice);
    }

    public static void RequestSetMode(int slot, SpectateMode mode, SpectatorModeNetHandler.SetModeReason reason = SpectatorModeNetHandler.SetModeReason.Default)
    {
        if (slot < 0 || slot >= Main.maxPlayers)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            SpectatorModeNetHandler.SendRequestSetMode(slot, mode, reason);
            return;
        }

        SetModeLocal(slot, mode);
    }

    public static void SetModeServer(int slot, SpectateMode mode)
    {
        if (Main.netMode != NetmodeID.Server || slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active)
            return;

        if (!IsGhostSpectatingEnabled)
            mode = SpectateMode.Player;

        Modes[slot] = mode;
        if (mode != SpectateMode.Spectator)
            pendingJoinChoices.Remove(slot);

        SpectatorModeNetHandler.SendSyncModes();
    }

    public static void SetModeLocal(int playerId, SpectateMode mode)
    {
        if (playerId is < 0 or >= Main.maxPlayers)
            return;

        if (!IsGhostSpectatingEnabled && !global::Reese.Common.Replayer.ReplayPlayback.IsReplayPlayback)
            mode = SpectateMode.Player;

        SpectateMode oldMode = GetMode(playerId);
        Modes[playerId] = mode;
        SyncGhostState(playerId, mode);

        if (playerId != Main.myPlayer || Main.netMode == NetmodeID.Server)
            return;

        if (oldMode != mode)
        {
            //Log.Chat($"Player {playerId} received mode: {mode}, player ghost now set to: {Main.LocalPlayer.ghost}");

            string newMode = $"You are now a {mode.ToString().ToLower()}.";
            Main.NewText(newMode, Color.Yellow);

            var specSystem = ModContent.GetInstance<SpectatorModeSystem>();

            if (specSystem != null)
                specSystem.OnLocalModeAccepted(mode);
            else
                Log.Warn("GhostHudSystem is null when trying to call OnLocalModeAccepted");
        }

        if (mode == SpectateMode.Spectator)
            Main.playerInventory = false;
        else
        // TODO: iS THIS ACCURATE?!
            SpectatorTargetSystem.ClearTarget();
    }

    public static bool EnsureServerModes()
    {
        if (Main.netMode != NetmodeID.Server)
            return false;

        bool changed = false;
        bool enabled = IsGhostSpectatingEnabled;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (!Main.player[i].active)
            {
                changed |= Modes.Remove(i);
                pendingJoinChoices.Remove(i);
                continue;
            }

            if (!enabled)
            {
                if (Modes.TryGetValue(i, out SpectateMode mode) && mode != SpectateMode.Player)
                    changed = true;

                Modes[i] = SpectateMode.Player;
                pendingJoinChoices.Remove(i);
                continue;
            }

            if (Modes.ContainsKey(i))
                continue;

            Modes[i] = GetJoinDefaultMode();
            if (ShowSpectatorJoinPanel)
                pendingJoinChoices.Add(i);
            else
                pendingJoinChoices.Remove(i);

            changed = true;
        }

        return changed;
    }

    public static bool TryAcceptClientModeRequest(int sender, int slot, SpectateMode mode, SpectatorModeNetHandler.SetModeReason reason, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (mode is not SpectateMode.Player and not SpectateMode.Spectator)
        {
            errorMessage = "Invalid spectator mode.";
            return false;
        }

        if (slot < 0 || slot >= Main.maxPlayers || Main.player[slot]?.active != true)
        {
            errorMessage = "Player not found.";
            return false;
        }

        if (!IsGhostSpectatingEnabled)
        {
            errorMessage = "Ghost spectating is disabled on this server.";
            return mode == SpectateMode.Player;
        }

        if (!Modes.ContainsKey(slot))
        {
            Modes[slot] = GetJoinDefaultMode();
            if (ShowSpectatorJoinPanel)
                pendingJoinChoices.Add(slot);
        }

        Player senderPlayer = sender >= 0 && sender < Main.maxPlayers ? Main.player[sender] : null;
        bool isAdmin = IsAdmin(senderPlayer);

        if (isAdmin)
            return true;

        if (slot != sender)
        {
            errorMessage = "Only admins can change another player's ghost mode.";
            return false;
        }

        if (reason == SpectatorModeNetHandler.SetModeReason.JoinPanelChoice && pendingJoinChoices.Remove(sender))
            return true;

        if (mode == SpectateMode.Spectator && ForceSpectateWhenJoining)
            return true;

        if (mode == SpectateMode.Spectator && ShowSpectatorJoinPanel && GetMode(sender) == SpectateMode.Spectator)
            return true;

        errorMessage = "Only admins can change ghost mode.";
        return false;
    }

    public static bool CanAdminSetMode(CommandCaller caller, Player target, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!IsGhostSpectatingEnabled)
        {
            errorMessage = "Ghost spectating is disabled on this server.";
            return false;
        }

        if (target?.active != true)
        {
            errorMessage = "Player not found.";
            return false;
        }

        if (caller.CommandType == CommandType.Server)
            return true;

        if (caller.Player?.active == true && (Main.netMode == NetmodeID.SinglePlayer || IsAdmin(caller.Player)))
            return true;

        errorMessage = "Only admins can use /ghost.";
        return false;
    }

    public override void PreUpdatePlayers()
    {
        if (!IsGhostSpectatingEnabled && !global::Reese.Common.Replayer.ReplayPlayback.IsReplayPlayback)
        {
            Modes.Clear();
            pendingJoinChoices.Clear();

            Player localPlayer = Main.LocalPlayer;
            if (Main.netMode != NetmodeID.Server && localPlayer?.active == true && !localPlayer.dead)
                localPlayer.ghost = false;

            return;
        }

        if (Main.netMode == NetmodeID.Server)
        {
            if (EnsureServerModes())
                SpectatorModeNetHandler.SendSyncModes();

            return;
        }

        Player local = Main.LocalPlayer;

        if (local?.active == true)
            SyncGhostState(local.whoAmI, GetMode(local.whoAmI));
    }

    private void OnLocalModeAccepted(SpectateMode mode)
    {
        GhostHudSystem hudSystem = ModContent.GetInstance<GhostHudSystem>();

        if (mode == SpectateMode.Spectator)
            hudSystem?.OpenFullHud();
        else
            hudSystem?.CloseFullHud();
    }

    public override void OnWorldLoad() => Reset();

    public override void OnWorldUnload() => Reset();

    private static void Reset()
    {
        Modes.Clear();
        pendingJoinChoices.Clear();
    }

    private static void SyncGhostState(int playerId, SpectateMode mode)
    {
        if (Main.netMode == NetmodeID.Server || playerId is < 0 or >= Main.maxPlayers)
            return;

        Player player = Main.player[playerId];

        if (player?.active != true)
            return;

        if (mode == SpectateMode.Spectator)
            player.ghost = true;
        else if (!player.dead)
            player.ghost = false;
    }
}
