using System.IO;

namespace Reese.Common.MainMenu;

internal static class ReplayFavorites
{
    public static bool IsFavorite(string replayPath)
    {
        return File.Exists(GetFavoritePath(replayPath));
    }

    public static void Toggle(string replayPath)
    {
        string favoritePath = GetFavoritePath(replayPath);

        if (File.Exists(favoritePath))
        {
            File.Delete(favoritePath);
            return;
        }

        File.WriteAllText(favoritePath, "");
    }

    public static void Delete(string replayPath)
    {
        string favoritePath = GetFavoritePath(replayPath);

        if (File.Exists(favoritePath))
            File.Delete(favoritePath);
    }

    public static void Move(string oldReplayPath, string newReplayPath)
    {
        string oldFavoritePath = GetFavoritePath(oldReplayPath);
        if (!File.Exists(oldFavoritePath))
            return;

        string newFavoritePath = GetFavoritePath(newReplayPath);

        if (File.Exists(newFavoritePath))
            File.Delete(newFavoritePath);

        File.Move(oldFavoritePath, newFavoritePath);
    }

    private static string GetFavoritePath(string replayPath)
    {
        return replayPath + ".favorite";
    }
}