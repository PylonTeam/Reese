using Reese.Core.Debug;
using System;
using System.IO;
using System.Linq;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reese.Common.MainMenu;

/// <summary>
/// Actions for <see cref="ReplayListItem"/>
/// </summary>
internal static class ReplayActions
{
    public static void EnterReplay(string replayPath)
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);

        Main.QueueMainThreadAction(() =>
        {
            ModContent.GetInstance<MainMenuSystem>().CloseForReplayLaunch();

            Main.LoadPlayers();
            var player = Main.PlayerList.FirstOrDefault();

            if (player == null)
            {
                Log.Chat("Could not enter replay: no player found.");
                Main.menuMode = 0;
                return;
            }

            Main.SelectPlayer(player);
            Log.Debug($"Successfully selected {player.Player.name} for replay");

            if (!File.Exists(replayPath))
            {
                Log.Error("Error: No replay file found at: " + replayPath);
                Main.menuMode = 0;
                return;
            }

            long replayMegaBytes = new FileInfo(replayPath).Length / (1024 * 1024);
            Log.Debug("Successfully found replay file, size: " + replayMegaBytes + " MB");

            try
            {
                ReplayPlayback.BeginPlayback(replayPath);
                ReplayFlags.MarkViewed(replayPath);
                ReplayFlags.MarkPlayed(replayPath);

                Netplay.SetRemoteIP("10.2.3.4");
                Main.autoPass = true;
                Netplay.StartTcpClient();
                Main.menuMode = 10;
            }
            catch (Exception e)
            {
                Log.Error("[ReplayBrowser] Failed to start replay: " + e);
                Main.statusText = "Failed to start replay";
                ReplayPlayback.End("playback launch failed");
                Main.menuMode = 0;
            }
        });
    }

    public static void Delete(string path, Action onDeleted = null)
    {
        if (ReplayFlags.IsFavorite(path))
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            return;
        }

        SoundEngine.PlaySound(SoundID.MenuOpen);

        ModContent.GetInstance<MainMenuSystem>().OpenConfirmDelete(Path.GetFileName(path), () =>
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                ReplayFlags.Delete(path);
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

            ReplayFlags.Move(path, destination);

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

    public static void Favorite(string path, Action onChanged = null)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);

        try
        {
            ReplayFlags.ToggleFavorite(path);
            onChanged?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to toggle replay favorite '{path}': {e}");
        }
    }
}
