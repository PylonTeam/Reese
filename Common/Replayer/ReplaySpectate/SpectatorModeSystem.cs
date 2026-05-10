using Reese.Common.Replayer.ReplaySpectate.UI;

namespace Reese.Common.Replayer.ReplaySpectate;

public enum SpectateMode : byte
{
    Player,
    Spectator
}

[Autoload(Side = ModSide.Client)]
internal sealed class SpectatorModeSystem : ModSystem
{
    public static SpectateMode GetMode(int slot)
    {
        return ReplaySession.IsReplayPlayback && slot == Main.myPlayer ? SpectateMode.Spectator : SpectateMode.Player;
    }

    public static bool IsInSpectateMode(Player player)
    {
        return player?.active == true && GetMode(player.whoAmI) == SpectateMode.Spectator;
    }

    public static bool IsInPlayerMode(Player player)
    {
        return player?.active == true && GetMode(player.whoAmI) == SpectateMode.Player;
    }

    public static int GetPlayersOnlineCount()
    {
        int count = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (Main.player[i]?.active == true && IsInPlayerMode(Main.player[i]))
                count++;
        }

        return count;
    }

    public static int GetSpectatorCount()
    {
        int count = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (Main.player[i]?.active == true && IsInSpectateMode(Main.player[i]))
                count++;
        }

        return count;
    }

    public static void RequestSetLocalMode(SpectateMode mode)
    {
        if (ReplaySession.IsReplayPlayback)
            ForceLocalReplaySpectator();
    }

    public static void ToggleSpectateMode(int slot)
    {
        if (ReplaySession.IsReplayPlayback)
            ForceLocalReplaySpectator();
    }

    public override void PreUpdatePlayers()
    {
        if (ReplaySession.IsReplayPlayback)
            ForceLocalReplaySpectator();
    }

    internal static void ForceLocalReplaySpectator()
    {
        if (!ReplaySession.IsReplayPlayback || Main.myPlayer is < 0 or >= Main.maxPlayers)
            return;

        Player local = Main.LocalPlayer;
        if (local?.active != true)
            return;

        bool changed = !local.ghost;
        local.ghost = true;
        Main.playerInventory = false;

        if (changed)
            ModContent.GetInstance<ReplayUISystem>()?.OnLocalModeAccepted(SpectateMode.Spectator);
    }
}