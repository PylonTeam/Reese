namespace Reese.Core.Compat;

internal static class ErkySSCIntegration
{
    public static bool IsPlayerErkySSCAdmin(Player player)
    {
        if (player == null || player.whoAmI < 0 || player.whoAmI >= Main.maxPlayers)
            return false;

        if (!ModLoader.TryGetMod("ErkySSC", out Mod erkySSC))
            return false;

        object result = erkySSC.Call("IsAdmin", player.whoAmI);

        if (result is bool isAdmin)
            return isAdmin;

        return false;
    }
}
