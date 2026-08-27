using Reese.Common.Replay;
using Reese.Common.Replayer;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ID;

namespace Reese.Common.MainMenu;

/// <summary>
/// Actions for <see cref="ReplayListItem"/>
/// </summary>
internal static class ReplayActions
{
    public static void EnterReplay(ReplayFile replay)
    {
        Main.LoadPlayers();

        var player = Main.PlayerList.FirstOrDefault();
        if (player == null)
        {
            Log.Chat("Could not enter replay: no player found.");
            ModContent.GetInstance<MainMenuSystem>().CancelReplayLaunch();
            return;
        }

        Main.SelectPlayer(player);
        Playback.Start(replay);
    }

    public static void Delete(string path, Action onDeleted = null)
    {
        // if (ReplayFlags.IsFavorite(path))
        // {
        //     SoundEngine.PlaySound(SoundID.MenuTick);
        //     return;
        // }

        SoundEngine.PlaySound(SoundID.MenuOpen);

        ModContent.GetInstance<MainMenuSystem>().OpenConfirmDelete(Path.GetFileName(path), () =>
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                // ReplayFlags.Delete(path);
                onDeleted?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to delete replay '{path}': {e}");
            }
        });
    }

    public static void Rename(string path, Action onRenamed = null)
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);

        string currentName = Path.GetFileNameWithoutExtension(path);
        ModContent.GetInstance<MainMenuSystem>().OpenRename(currentName, name => FinishRename(path, name, onRenamed));
    }

    private static void FinishRename(string path, string name, Action onRenamed)
    {
        try
        {
            string newName = SanitizeFileName(Path.GetFileNameWithoutExtension(name));
            if (string.IsNullOrWhiteSpace(newName))
                return;

            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                return;

            if (string.Equals(Path.GetFileNameWithoutExtension(path), newName, StringComparison.OrdinalIgnoreCase))
                return;

            string destination = GetAvailableReplayPath(directory, newName);
            File.Move(path, destination);

            // ReplayFlags.Move(path, destination);

            onRenamed?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to rename replay '{path}': {e}");
        }
    }

    private static string GetAvailableReplayPath(string directory, string name)
    {
        string path = Path.Combine(directory, name + ".reese");
        int suffix = 2;

        while (File.Exists(path))
            path = Path.Combine(directory, $"{name}_{suffix++}.reese");

        return path;
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            name = name.Replace(invalidChar, '_');

        return name.Trim();
    }

    public static void Favorite()
    {
        // TODO: Favorite to file
        SoundEngine.PlaySound(SoundID.MenuTick);
    }
}
