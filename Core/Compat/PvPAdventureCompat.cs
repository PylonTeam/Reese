using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Core.Compat;

/// <summary>
/// Move spectate hud down to make room for the pvp adventure scoreboard
/// </summary>
public static class PvPAdventureCompat
{
    public static bool IsPvPAdventureLoaded => ModLoader.TryGetMod("PvPAdventure", out _);
}
