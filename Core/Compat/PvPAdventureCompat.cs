using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese.Core.Compat;

public static class PvPAdventureCompat
{
    public static bool IsPvPAdventureLoaded => ModLoader.TryGetMod("PvPAdventure", out _);
}
