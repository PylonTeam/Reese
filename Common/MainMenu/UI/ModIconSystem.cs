#nullable enable
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria.UI.Chat;

namespace Reese.Common.MainMenu.UI;

/// <summary>
/// Registers a new item tag for mod icons so we can display them as part of the replay stats.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class ModIconSystem : ModSystem
{
    public override void Load()
    {
        ChatManager.Register<ModIconTagHandler>(["mi", "modicon"]);
    }

    public override void Unload()
    {
        ModIconCache.Clear();
    }
}

internal sealed class ModIconTagHandler : ITagHandler
{
    TextSnippet ITagHandler.Parse(string text, Color baseColor, string? options)
    {
        return ModIconSnippet.CreateSnippet(text);
    }
}

internal sealed class ModIconSnippet : TextSnippet
{
    private const float IconDrawSize = 20f;
    private const float ReservedWidth = 24f;
    private const float VerticalOffset = 1f;

    private readonly string modName;
    private readonly ModIconCacheEntry modCache;

    private ModIconSnippet(string? modName)
    {
        this.modName = (modName ?? string.Empty).Trim();
        modCache = ModIconCache.Get(this.modName);

        Text = string.Empty;
        Color = Color.White;
        CheckForHover = true;
    }

    public static TextSnippet CreateSnippet(string? modName)
    {
        return new ModIconSnippet(modName);
    }

    public override bool UniqueDraw(
        bool justCheckingString,
        out Vector2 size,
        SpriteBatch spriteBatch,
        Vector2 position = default,
        Color color = default,
        float scale = 1f
    )
    {
        float safeScale = Math.Max(0f, scale);

        size = new Vector2(
            ReservedWidth * safeScale,
            ReservedWidth * safeScale
        );

        if (justCheckingString || IsShadowPass(color))
        {
            return true;
        }

        if (modCache.Icon is not { } icon)
        {
            return true;
        }

        int drawSize = Math.Max(1, (int)Math.Round(IconDrawSize * safeScale));

        Rectangle destination = new(
            (int)Math.Round(position.X),
            (int)Math.Round(position.Y + VerticalOffset * safeScale),
            drawSize,
            drawSize
        );

        spriteBatch.Draw(icon, destination, Color.White);
        return true;
    }

    private static bool IsShadowPass(Color color)
    {
        return color.R <= 2 && color.G <= 2 && color.B <= 2;
    }

    public override void OnHover()
    {
        if (!string.IsNullOrWhiteSpace(modCache.DisplayName))
        {
            Main.instance.MouseText(modCache.DisplayName);
            return;
        }

        if (!string.IsNullOrWhiteSpace(modName))
        {
            Main.instance.MouseText(modName);
        }
    }

    public override float GetStringLength(DynamicSpriteFont font)
    {
        return ReservedWidth;
    }

    public override Color GetVisibleColor()
    {
        return Color.White;
    }
}

internal readonly record struct ModIconCacheEntry(
    Texture2D? Icon,
    string? DisplayName
);

internal static class ModIconCache
{
    private static readonly Dictionary<string, ModIconCacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ModIconCacheEntry Get(string modName)
    {
        modName = modName.Trim();

        if (string.IsNullOrWhiteSpace(modName))
        {
            return default;
        }

        if (Cache.TryGetValue(modName, out ModIconCacheEntry cached))
        {
            return cached;
        }

        ModIconCacheEntry loaded = Load(modName);
        Cache[modName] = loaded;
        return loaded;
    }

    public static void Clear()
    {
        Cache.Clear();
    }

    private static ModIconCacheEntry Load(string modName)
    {
        if (!ModLoader.TryGetMod(modName, out Mod mod))
        {
            return new ModIconCacheEntry(null, modName);
        }

        Texture2D? icon =
            TryRequestIcon(mod, "icon_small", "icon_small.rawimg", "icon_small.png") ??
            TryRequestIcon(mod, "icon", "icon.png", "icon.rawimg");

        return new ModIconCacheEntry(icon, mod.DisplayName);
    }

    private static Texture2D? TryRequestIcon(Mod mod, string assetName, params string[] fileNames)
    {
        bool exists = false;

        foreach (string fileName in fileNames)
        {
            if (mod.FileExists(fileName))
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            return null;
        }

        try
        {
            return mod.Assets.Request<Texture2D>(assetName, AssetRequestMode.ImmediateLoad).Value;
        }
        catch (Exception exception)
        {
            Log.Chat($"Failed to load '{assetName}' icon for mod '{mod.Name}': {exception.Message}");
            return null;
        }
    }
}