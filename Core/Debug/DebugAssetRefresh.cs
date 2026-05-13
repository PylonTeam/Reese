#if DEBUG
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.ModLoader.Assets;

namespace Reese.Core.Debug;

internal readonly struct DebugTextureAsset
{
    public readonly string Label;
    public readonly Asset<Texture2D> Asset;

    public DebugTextureAsset(string label, Asset<Texture2D> asset)
    {
        Label = label;
        Asset = asset;
    }
}

internal sealed class DebugAssetRefreshSystem : ModSystem
{
    private readonly Dictionary<string, DateTime> lastWrites = [];
    private bool hasSnapshot;

    public override void PostSetupContent()
    {
        if (!Main.dedServ)
            CaptureCurrentWriteTimes();
    }

    public override void Unload()
    {
        lastWrites.Clear();
        hasSnapshot = false;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (Main.dedServ)
            return;

        if (KeyboardHelper.Pressed(Keys.F5))
        {
            RefreshAssets();
            return;
        }
    }

    private void CaptureCurrentWriteTimes()
    {
        Reese mod = ModContent.GetInstance<Reese>();

        if (!CanReloadFromSource(mod))
            return;

        _ = Ass.Initialized;

        foreach (DebugTextureAsset entry in CollectTextureAssets())
        {
            string path = FindTexturePath(mod, entry.Asset);

            if (File.Exists(path))
                lastWrites[path] = File.GetLastWriteTimeUtc(path);
        }

        hasSnapshot = true;
    }

    private void RefreshAssets()
    {
        Reese mod = ModContent.GetInstance<Reese>();

        if (!CanReloadFromSource(mod))
        {
            Log.Chat("Cannot reload assets: mod is not running from source.");
            return;
        }

        _ = Ass.Initialized;

        List<DebugTextureAsset> assets = CollectTextureAssets();

        if (assets.Count == 0)
        {
            Log.Chat("Reloaded 0/0 assets.");
            return;
        }

        FieldInfo ownValue = typeof(Asset<Texture2D>).GetField("ownValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        PropertyInfo state = typeof(Asset<Texture2D>).GetProperty("State", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo stateBacking = typeof(Asset<Texture2D>).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        if (ownValue == null)
        {
            Log.Chat("Cannot reload assets: Asset<Texture2D>.ownValue was not found.");
            return;
        }

        int reloaded = 0;
        List<string> changed = [];
        List<string> missing = [];
        List<string> failed = [];

        foreach (DebugTextureAsset entry in assets)
        {
            string path = FindTexturePath(mod, entry.Asset);

            if (!File.Exists(path))
            {
                missing.Add(entry.Label);
                continue;
            }

            DateTime write = File.GetLastWriteTimeUtc(path);

            if (lastWrites.TryGetValue(path, out DateTime previousWrite))
            {
                if (previousWrite != write)
                    changed.Add(entry.Label);
            }
            else if (hasSnapshot)
            {
                changed.Add(entry.Label);
            }

            lastWrites[path] = write;

            try
            {
                using Stream stream = File.OpenRead(path);
                Texture2D texture = Texture2D.FromStream(Main.graphics.GraphicsDevice, stream);

                ownValue.SetValue(entry.Asset, texture);

                if (state != null && state.CanWrite)
                    state.SetValue(entry.Asset, AssetState.Loaded);
                else
                    stateBacking?.SetValue(entry.Asset, AssetState.Loaded);

                reloaded++;
            }
            catch (Exception e)
            {
                failed.Add($"{entry.Label}: {e.GetType().Name}");
            }
        }

        hasSnapshot = true;

        Log.Chat($"Reloaded {reloaded}/{assets.Count} assets.");

        if (changed.Count == 0)
            Log.Chat("Changed: none.");
        else
            Log.Chat($"Changed: {FormatNames(changed)}.");

        if (missing.Count > 0)
            Log.Chat($"Missing: {FormatNames(missing)}.");

        if (failed.Count > 0)
            Log.Chat($"Failed: {FormatNames(failed)}.");
    }

    private static bool CanReloadFromSource(Reese mod)
    {
        return mod != null && mod.RootContentSource is TModContentSource && !string.IsNullOrEmpty(mod.SourceFolder);
    }

    private static List<DebugTextureAsset> CollectTextureAssets()
    {
        List<DebugTextureAsset> assets = [];
        HashSet<Asset<Texture2D>> seen = [];

        foreach (FieldInfo field in typeof(Ass).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(Asset<Texture2D>))
            {
                if (field.GetValue(null) is Asset<Texture2D> asset && seen.Add(asset))
                    assets.Add(new DebugTextureAsset(field.Name, asset));

                continue;
            }

            if (field.FieldType != typeof(Asset<Texture2D>[]))
                continue;

            if (field.GetValue(null) is not Asset<Texture2D>[] array)
                continue;

            for (int i = 0; i < array.Length; i++)
            {
                Asset<Texture2D> asset = array[i];

                if (asset == null || !seen.Add(asset))
                    continue;

                assets.Add(new DebugTextureAsset(GetAssetLabel(field.Name, i, asset), asset));
            }
        }

        return assets;
    }

    private static string FindTexturePath(Reese mod, Asset<Texture2D> asset)
    {
        string relative = asset.Name.Replace('/', Path.DirectorySeparatorChar);
        string directPath = Path.ChangeExtension(Path.Combine(mod.SourceFolder, relative), "png");

        if (File.Exists(directPath))
            return directPath;

        string prefix = mod.Name + Path.DirectorySeparatorChar;

        if (!relative.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return directPath;

        string stripped = relative[prefix.Length..];
        string strippedPath = Path.ChangeExtension(Path.Combine(mod.SourceFolder, stripped), "png");

        if (File.Exists(strippedPath))
            return strippedPath;

        return strippedPath;
    }

    private static string GetAssetLabel(string fieldName, int index, Asset<Texture2D> asset)
    {
        int slash = asset.Name.LastIndexOf('/');

        if (slash >= 0 && slash < asset.Name.Length - 1)
            return asset.Name[(slash + 1)..];

        return $"{fieldName}[{index}]";
    }

    private static string FormatNames(List<string> names)
    {
        const int limit = 12;

        if (names.Count <= limit)
            return string.Join(", ", names);

        List<string> visible = names.GetRange(0, limit);
        return $"{string.Join(", ", visible)} +{names.Count - limit} more";
    }
}
#endif