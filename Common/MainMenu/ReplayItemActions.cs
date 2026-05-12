using Reese.Core.Debug;
using System;
using System.IO;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reese.Common.MainMenu;

internal static class ReplayItemActions
{
    public static void Play(string path)
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);
        ReplayBrowser.EnterReplay(path);
    }

    public static void Delete(string path, Action onDeleted = null)
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);
        //MainMenuActions.OpenConfirmDelete(Path.GetFileName(path), () =>
        //{
        //    try
        //    {
        //        if (File.Exists(path))
        //            File.Delete(path);

        //        ReplayFavorites.Delete(path);
        //        //ReplayImages.DeletePreview(path);

        //        onDeleted?.Invoke();
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Error($"Failed to delete replay '{path}': {e}");
        //    }
        //});
    }

    public static void Rename(string path, Action onRenamed = null)
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);
        string currentName = Path.GetFileNameWithoutExtension(path);
        //MainMenuActions.OpenRename(currentName, name => FinishRename(path, name, onRenamed));
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

            ReplayFavorites.Move(path, destination);
            //ReplayImages.MovePreview(path, destination);

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

    //public static void ChoosePreviewImage(string path, Action onChanged = null)
    //{
    //    SoundEngine.PlaySound(SoundID.MenuOpen);

    //    try
    //    {
    //        if (ReplayImages.ChooseAndSavePreview(path))
    //            onChanged?.Invoke();
    //    }
    //    catch (Exception e)
    //    {
    //        Log.Error($"Failed to set replay preview image '{path}': {e}");
    //    }
    //}

    public static void Favorite(string path, Action onChanged = null)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);

        try
        {
            ReplayFavorites.Toggle(path);
            onChanged?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to toggle replay favorite '{path}': {e}");
        }
    }
}
