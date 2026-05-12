using System.IO;
using Terraria;

namespace Reese;

internal static class ReplayPaths
{
    public static string GetFolder() => Path.Combine(Main.SavePath, "ReeseReplays");
}