using System.IO;

namespace Reese.Common;

internal static class ReplayPaths
{
    public static string GetFolder() => Path.Combine(Main.SavePath, "ReeseReplays");
}
