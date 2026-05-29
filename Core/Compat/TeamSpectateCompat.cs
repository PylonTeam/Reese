using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Core.Compat;

/// <summary>
/// Maybe remove the team spectate hud during replays?
/// </summary>
internal class TeamSpectateCompat
{
    public static bool IsPvPAdventureLoaded => ModLoader.TryGetMod("PvPAdventure", out _);
}
