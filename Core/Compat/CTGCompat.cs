using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Core.Compat;

/// <summary>
/// Move spectate hud down to make room for the ctg scoreboard
/// </summary>
public static class CTGCompat
{
    public static bool IsCTGLoaded => ModLoader.TryGetMod("CTG2", out _);
}
