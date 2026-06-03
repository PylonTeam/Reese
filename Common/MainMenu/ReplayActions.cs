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
    public static void EnterReplay(string replayPath)
    {
        Log.Info($"EnterReplay requested for {Path.GetFileName(replayPath)}.");

        if (!ReplayModSetManager.PrepareReplayModsOrContinue(replayPath))
        {
            Log.Info("Replay playback is waiting for bundled mod preparation/reload.");
            return;
        }

        EnterReplayPrepared(replayPath);
    }

    internal static void EnterReplayPrepared(string replayPath)
    {
        Log.Info($"Starting replay playback with prepared mods: {Path.GetFileName(replayPath)}.");
        SoundEngine.PlaySound(SoundID.MenuOpen);
        Main.QueueMainThreadAction(() =>
        {
            string fileName = Path.GetFileName(replayPath);

            // Owns the lifetime of this launch attempt
            ReplayLaunchSession session = ModContent.GetInstance<MainMenuSystem>().BeginReplayLaunch();
            ReplayPlayback.IsLaunchCancelled = () => session.IsCancelled;

            Log.Info($"Loading {fileName}...");
            Main.LoadPlayers();

            var player = Main.PlayerList.FirstOrDefault();
            if (player == null)
            {
                Log.Chat("Could not enter replay: no player found.");
                ModContent.GetInstance<MainMenuSystem>().CancelReplayLaunch();
                return;
            }

            Main.SelectPlayer(player);
            Log.Info("Selected player: " + player.Name + " for replay");

            if (!File.Exists(replayPath))
            {
                Log.Error("Error: No replay file found at: " + replayPath);
                ModContent.GetInstance<MainMenuSystem>().CancelReplayLaunch();
                return;
            }

            Log.Info($"Entering menuMode 14...");
            Main.menuMode = 14; // status text only loading screen is 10. maybe 14 is better to allow for cancellation?

            Task.Run(() =>
            {
                try
                {
                    Main.statusText = $"Reading {fileName}..."; // file scan + baseline index takes a few seconds.
                    ReplayPlayback.BeginPlayback(replayPath);
                }
                catch (Exception e)
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        if (session.IsCancelled)
                        {
                            ReplayPlayback.End("replay launch cancelled before connection");
                            return;
                        }
                        Log.Error("Failed to start replay: " + e);
                        Main.statusText = "Failed to start replay";
                        ReplayPlayback.End("playback launch failed");
                        ModContent.GetInstance<MainMenuSystem>().CancelReplayLaunch();
                    });
                    return;
                }

                Main.QueueMainThreadAction(() =>
                {
                    if (session.IsCancelled)
                    {
                        ReplayPlayback.End("replay launch cancelled before connection");
                        return;
                    }

                    try
                    {
                        if (session.IsCancelled)
                        {
                            ReplayPlayback.End("replay launch cancelled before connection");
                            ReplayPlayback.IsLaunchCancelled = null;
                            return;
                        }

                        // Connect to magic ip
                        Main.statusText = "Connecting...";
                        ReplayFlags.MarkWatched(replayPath);
                        Netplay.SetRemoteIP("10.2.3.4");
                        Main.autoPass = true;
                        Netplay.StartTcpClient();
                        Main.menuMode = 10;
                    }
                    catch (Exception e)
                    {
                        Log.Error("Failed to start replay: " + e);
                        Main.statusText = "Failed to start replay";
                        ReplayPlayback.End("playback launch failed");
                        ModContent.GetInstance<MainMenuSystem>().CancelReplayLaunch();
                    }
                });
            }, session.Token);
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

    public static void Favorite(string path, Action<ReplayFileFlags> onChanged = null)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);

        try
        {
            if (!ReplayFlags.TryToggleFavorite(path, out ReplayFileFlags flags))
            {
                Log.Warn($"Failed to toggle replay favorite '{path}'");
                return;
            }

            onChanged?.Invoke(flags);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to toggle replay favorite '{path}': {e}");
        }
    }
}
