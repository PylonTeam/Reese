using System.IO;
using Terraria;

namespace Reese.Common.Replayer;

internal static class ReplayPaths
{
    public static string GetFolder() => Path.Combine(Main.SavePath, "ReeseReplays");
    public static string GetFile() => Path.Combine(GetFolder(), "record.bin"); // temporary/legacy for testing
}