//using Microsoft.Xna.Framework.Graphics;
//using Reese.Core.Debug;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Reese.Common.MainMenu;

//internal static class ReplayImages
//{
//    private static readonly Dictionary<string, Texture2D> cachedTextures = [];

//    public static string GetPreviewPath(string replayPath)
//    {
//        foreach (string extension in new[] { ".png", ".jpg", ".jpeg" })
//        {
//            string path = GetPreviewPath(replayPath, extension);
//            if (File.Exists(path))
//                return path;
//        }

//        return null;
//    }

//    public static bool ChooseAndSavePreview(string replayPath)
//    {
//        string sourcePath = FileUploadHelper.OpenFileDialog();
//        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
//            return false;

//        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
//        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
//            return false;

//        DeletePreview(replayPath);

//        string destinationPath = GetPreviewPath(replayPath, extension);
//        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
//        File.Copy(sourcePath, destinationPath, true);

//        ClearCachedTexture(destinationPath);
//        return true;
//    }

//    public static void DeletePreview(string replayPath)
//    {
//        foreach (string extension in new[] { ".png", ".jpg", ".jpeg" })
//        {
//            string path = GetPreviewPath(replayPath, extension);

//            ClearCachedTexture(path);

//            if (File.Exists(path))
//                File.Delete(path);
//        }
//    }

//    public static void MovePreview(string oldReplayPath, string newReplayPath)
//    {
//        foreach (string extension in new[] { ".png", ".jpg", ".jpeg" })
//        {
//            string oldPath = GetPreviewPath(oldReplayPath, extension);
//            if (!File.Exists(oldPath))
//                continue;

//            string newPath = GetPreviewPath(newReplayPath, extension);
//            ClearCachedTexture(oldPath);
//            ClearCachedTexture(newPath);

//            if (File.Exists(newPath))
//                File.Delete(newPath);

//            File.Move(oldPath, newPath);
//        }
//    }

//    public static Texture2D GetTexture(string previewPath)
//    {
//        if (string.IsNullOrWhiteSpace(previewPath) || !File.Exists(previewPath))
//            return null;

//        if (cachedTextures.TryGetValue(previewPath, out Texture2D cached) && cached != null && !cached.IsDisposed)
//            return cached;

//        try
//        {
//            using var stream = File.OpenRead(previewPath);
//            Texture2D texture = Texture2D.FromStream(Main.graphics.GraphicsDevice, stream);
//            texture.Name = previewPath;
//            cachedTextures[previewPath] = texture;
//            return texture;
//        }
//        catch (Exception e)
//        {
//            Log.Warn($"Failed to load replay preview image '{Path.GetFileName(previewPath)}': {e.Message}");
//            return null;
//        }
//    }

//    private static string GetPreviewPath(string replayPath, string extension)
//    {
//        string directory = Path.GetDirectoryName(replayPath);
//        string name = Path.GetFileNameWithoutExtension(replayPath);
//        //return Path.Combine(directory, name + ".preview" + extension);
//        return Path.Combine(directory, name + extension);
//    }

//    private static void ClearCachedTexture(string path)
//    {
//        if (string.IsNullOrWhiteSpace(path))
//            return;

//        if (!cachedTextures.Remove(path, out Texture2D texture))
//            return;

//        texture?.Dispose();
//    }
//}
