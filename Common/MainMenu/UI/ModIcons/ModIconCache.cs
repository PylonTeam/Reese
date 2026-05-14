#nullable enable
using Reese.Core.Utilities;
using ReLogic.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.ModLoader.Core;

namespace Reese.Common.MainMenu.UI.ModIcons;

internal static class ModIconCache
{
    private static readonly Dictionary<string, ModIconCacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

    // Reflection Cache
    private static readonly MethodInfo? findAllModsMethod;
    private static readonly FieldInfo? modFileField;
    private static readonly PropertyInfo? displayNameProperty;
    private static readonly PropertyInfo? nameProperty;

    static ModIconCache()
    {
        var assembly = typeof(ModLoader).Assembly;
        var modOrganizerType = assembly.GetType("Terraria.ModLoader.Core.ModOrganizer");
        var localModType = assembly.GetType("Terraria.ModLoader.Core.LocalMod");

        findAllModsMethod = modOrganizerType?.GetMethod("FindAllMods", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        modFileField = localModType?.GetField("modFile", BindingFlags.Public | BindingFlags.Instance);
        displayNameProperty = localModType?.GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Instance);
        nameProperty = localModType?.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
    }

    public static ModIconCacheEntry Get(string modName)
    {
        modName = modName.Trim();
        if (string.IsNullOrWhiteSpace(modName)) return default;

        if (Cache.TryGetValue(modName, out ModIconCacheEntry cached))
            return cached;

        ModIconCacheEntry loaded = Load(modName);
        Cache[modName] = loaded;
        return loaded;
    }

    private static ModIconCacheEntry Load(string modName)
    {
        if (ModLoader.TryGetMod(modName, out Mod mod))
        {
            Texture2D? icon = TryRequestIcon(mod, "Iconsmall", "Iconsmall.png") ??
                             TryRequestIcon(mod, "icon", "icon.png");
            return new ModIconCacheEntry(icon, mod.DisplayName);
        }

        if (findAllModsMethod?.Invoke(null, null) is IEnumerable allMods)
        {
            foreach (object localModObj in allMods)
            {
                string? internalName = nameProperty?.GetValue(localModObj) as string;

                if (string.Equals(internalName, modName, StringComparison.OrdinalIgnoreCase))
                {
                    TmodFile? file = modFileField?.GetValue(localModObj) as TmodFile;
                    string? displayName = displayNameProperty?.GetValue(localModObj) as string;
                    Texture2D? icon = null;

                    if (file != null && file.HasFile("icon.png"))
                    {
                        try
                        {
                            using (file.Open())
                            {
                                using Stream s = file.GetStream("icon.png");
                                icon = Main.Assets.CreateUntracked<Texture2D>(s, ".png").Value;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warn($"Failed to extract icon from {modName}: {e.Message}");
                        }
                    }

                    return new ModIconCacheEntry(icon, displayName ?? modName);
                }
            }
        }

        return new ModIconCacheEntry(null, modName);
    }

    private static Texture2D? TryRequestIcon(Mod mod, string assetName, string fileName)
    {
        if (!mod.FileExists(fileName)) return null;
        try { return mod.Assets.Request<Texture2D>(assetName, AssetRequestMode.ImmediateLoad).Value; }
        catch { return null; }
    }

    public static void Clear() => Cache.Clear();
}

internal readonly record struct ModIconCacheEntry(
    Texture2D? Icon,
    string? DisplayName
);