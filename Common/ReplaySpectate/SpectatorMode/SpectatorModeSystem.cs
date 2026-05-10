using Reese.Common.Replayer;
using Reese.Common.ReplaySpectate.UI;
using Reese.Core.Debug;
using System.Collections.Generic;
using System.Text;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;

namespace Reese.Common.ReplaySpectate.SpectatorMode;

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

    public static SpectateMode GetMode(int slot) => Modes.TryGetValue(slot, out SpectateMode mode) ? mode : SpectateMode.Player;

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

    internal static SpectateMode GetJoinDefaultMode() => ReplaySession.IsReplayPlayback ? SpectateMode.Spectator : SpectateMode.Player;

    public static void ToggleSpectateMode(int slot)
    {
        if (ReplaySession.IsReplayPlayback)
        {
            ForceLocalReplaySpectator();
            return;
        }

        if (slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active)
            return;

        SpectateMode currentMode = GetMode(slot);
        SpectateMode newMode = currentMode == SpectateMode.Player ? SpectateMode.Spectator : SpectateMode.Player;
        RequestSetLocalMode(newMode);
    }

    public static void RequestSetLocalMode(SpectateMode mode)
    {
        if (Main.myPlayer is < 0 or >= Main.maxPlayers)
            return;

        if (ReplaySession.IsReplayPlayback)
        {
            SetModeLocal(Main.myPlayer, SpectateMode.Spectator);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            SpectatorModeNetHandler.SendRequestSetMode(Main.myPlayer, mode);
            return;
        }

        SetModeLocal(Main.myPlayer, mode);
    }

    public static void SetModeServer(int slot, SpectateMode mode)
    {
        if (Main.netMode != NetmodeID.Server || slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active)
            return;

        Modes[slot] = mode;
        SpectatorModeNetHandler.SendSyncModes();
    }

    public static void SetModeLocal(int playerId, SpectateMode mode)
    {
        if (playerId is < 0 or >= Main.maxPlayers)
            return;

        if (ReplaySession.IsReplayPlayback && playerId == Main.myPlayer)
            mode = SpectateMode.Spectator;

        SpectateMode oldMode = GetMode(playerId);
        Modes[playerId] = mode;

        if (playerId != Main.myPlayer || Main.netMode == NetmodeID.Server)
            return;

        Main.LocalPlayer.ghost = mode == SpectateMode.Spectator;

        if (oldMode != mode)
        {
            //Log.Chat($"Player {playerId} received mode: {mode}, player ghost now set to: {Main.LocalPlayer.ghost}");

            string newMode = $"You are now a {mode.ToString().ToLower()}.";
            Main.NewText(newMode, Color.Yellow);

            var specSystem = ModContent.GetInstance<SpectatorUISystem>();

            if (specSystem != null)
                specSystem.OnLocalModeAccepted(mode);
            else
                Log.Warn("SpectatorUISystem is null when trying to call OnLocalModeAccepted");
        }

        if (mode == SpectateMode.Spectator)
            Main.playerInventory = false;
        else
            SpectatorTargetSystem.ClearTarget();
    }

    public static bool EnsureServerModes()
    {
        if (Main.netMode != NetmodeID.Server)
            return false;

        bool changed = false;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (!Main.player[i].active)
            {
                changed |= Modes.Remove(i);
                continue;
            }

            if (Modes.ContainsKey(i))
                continue;

            Modes[i] = GetJoinDefaultMode();
            changed = true;
        }

        return changed;
    }

    public override void PreUpdatePlayers()
    {
        if (ReplaySession.IsReplayPlayback)
        {
            ForceLocalReplaySpectator();
            return;
        }

        if (Main.netMode == NetmodeID.Server)
        {
            if (EnsureServerModes())
                SpectatorModeNetHandler.SendSyncModes();

            return;
        }

        Player local = Main.LocalPlayer;

        if (local?.active == true && GetMode(local.whoAmI) == SpectateMode.Spectator && !local.ghost)
            SetModeLocal(local.whoAmI, SpectateMode.Player);
    }

    public override void OnWorldLoad() => Reset();

    public override void OnWorldUnload() => Reset();

    private static void Reset()
    {
        Modes.Clear();
    }

    internal static void ForceLocalReplaySpectator()
    {
        if (!ReplaySession.IsReplayPlayback || Main.myPlayer is < 0 or >= Main.maxPlayers)
            return;

        SetModeLocal(Main.myPlayer, SpectateMode.Spectator);
    }
}
