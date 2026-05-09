using Reese.Common.MainMenu.State;
using Reese.Core.Debug;
using System;
using System.IO;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reese.Common.MainMenu;

internal static class ReplayMenuActions
{
    public static void Play(string path)
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);
        ReplayBrowser.EnterReplay(path);
    }

    public static void Delete(string path, Action onDeleted = null)
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);
        ModContent.GetInstance<ExtraStateMainMenuSystem>().OpenConfirmDelete(Path.GetFileName(path), () =>
        {
            try
            {
                Log.Chat("heya");

                if (File.Exists(path))
                    File.Delete(path);

                onDeleted?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to delete replay '{path}': {e}");
            }
        });
    }
}
