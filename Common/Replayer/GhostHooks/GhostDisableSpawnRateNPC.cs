//using GhostSpectating.Common.SpectatorMode;

//namespace GhostSpectating.Common.Hooks;

///// <summary>
///// Disables spawn rate for players who are spectators
///// </summary>
//internal class DisableSpawnRateNPC : GlobalNPC
//{
//    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
//    {
//        if (!SpectatorTargetSystem.IsInSpectateMode(player))
//            return;

//        spawnRate = int.MaxValue;
//        maxSpawns = 0;
//    }
//}
