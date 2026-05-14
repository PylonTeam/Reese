using System.IO;
using Terraria;

namespace Reese.Common.Replayer;

internal static class ReplayPaths
{
    public static string GetFolder() => Path.Combine(Main.SavePath, "ReeseReplays");
}