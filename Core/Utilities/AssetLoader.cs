using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Exceptions;

namespace Reese.Core.Utilities;

/// <summary>
/// Provides static access to miscallaneous texture assets within the Reese mod.
/// Automatically initializes when the mod system loads.
/// All asset fields are intended for global access throughout the mod.
/// Here we store the registry of our assets
/// </summary>
public static class Ass
{
    // Main menu
    public static Asset<Texture2D> IconNewlyGenerated;
    public static Asset<Texture2D> IconArrowDown;
    public static Asset<Texture2D> IconArrowUp;
    public static Asset<Texture2D> ButtonOpenFolder;
    public static Asset<Texture2D> ButtonRefresh;
    public static Asset<Texture2D> ButtonTableColumn;
    public static Asset<Texture2D> ButtonTableColumn_Border;
    public static Asset<Texture2D> ButtonTableColumn_Selected;

    // Replay playback HUD
    public static Asset<Texture2D> IconCamera;
    public static Asset<Texture2D> IconCameraSmall;
    public static Asset<Texture2D> IconChest;
    public static Asset<Texture2D> IconNextFrame;
    public static Asset<Texture2D> IconPause;
    public static Asset<Texture2D> IconPlay;
    public static Asset<Texture2D> IconReset;
    public static Asset<Texture2D> IconRefresh;
    public static Asset<Texture2D> IconResize;
    public static Asset<Texture2D> IconStop;
    public static Asset<Texture2D> IconSpeedUp;
    public static Asset<Texture2D> IconSpeedDown;
    public static Asset<Texture2D> Slider;
    public static Asset<Texture2D> SliderHighlight;
    public static Asset<Texture2D> SliderGradient;

    // Replay spectate HUD
    public static Asset<Texture2D> IconSword;
    public static Asset<Texture2D> IconDead;
    public static Asset<Texture2D> IconDistance;
    public static Asset<Texture2D> GhostRight;
    public static Asset<Texture2D> GhostLeft;
    public static Asset<Texture2D> IconBiome;
    public static Asset<Texture2D> IconEye;
    public static Asset<Texture2D> IconInventoryOpen;
    public static Asset<Texture2D> IconInventoryClosed;
    public static Asset<Texture2D> IconHeldItem;
    public static Asset<Texture2D> IconNPC;
    public static Asset<Texture2D> IconPlayer;

    // Replay info HUD
    public static Asset<Texture2D> IconArrow;
    public static Asset<Texture2D> IconCandelabraOn;
    public static Asset<Texture2D> IconCandelabraOff;
    public static Asset<Texture2D> IconCheckmarkGreen;
    public static Asset<Texture2D> IconFilmProjectorOn;
    public static Asset<Texture2D> IconFilmProjectorOff;
    public static Asset<Texture2D> IconGear;
    public static Asset<Texture2D> IconMapOn;
    public static Asset<Texture2D> IconMapOff;
    public static Asset<Texture2D> IconPlayerHead;
    public static Asset<Texture2D> IconTeleportOn;
    public static Asset<Texture2D> IconTeleportOff;
    public static Asset<Texture2D> IconWorld;

    // Map backgrounds
    //public static Asset<Texture2D>[] MapBG;

    // Flag
    public static bool Initialized { get; set; }

    /// <summary>
    /// Initializes static assets
    /// Automatically runs once the mod system loads via <see cref="AssetLoader"/>
    /// </summary>
    static Ass()
    {
        if (Main.dedServ)
        {
            Initialized = true;
            return;
        }

        const string ModName = "Reese";
        List<string> missingAssets = [];

        //MapBG = new Asset<Texture2D>[42];

        //for (int i = 1; i <= MapBG.Length; i++)
        //    MapBG[i - 1] = RequestTexture($"MapBG{i}", $"{ModName}/Assets/MapBGs/MapBG{i}", missingAssets);

        FieldInfo[] fields = typeof(Ass).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType != typeof(Asset<Texture2D>))
                continue;

            field.SetValue(null, RequestTexture(field.Name, $"{ModName}/Assets/{field.Name}", missingAssets));
        }

        // Check if any assets failed to load, and throw a single exception if they did
        if (missingAssets.Count > 0)
        {
            throw new MissingAssetException(missingAssets);
        }

        Initialized = true;
    }

    private static Asset<Texture2D> RequestTexture(string assetName, string path, List<string> missingAssets)
    {
        if (!ModContent.HasAsset(path))
        {
            missingAssets.Add($"Ass.{assetName} (Searched: {path})");
            return null; // Return null temporarily since we're going to crash shortly anyway
        }

        return ModContent.Request<Texture2D>(path, AssetRequestMode.AsyncLoad);
    }
}

/// <summary>
/// Initializes asset loading for the mod when the system is loaded with all assets in <see cref="Ass"/>
/// </summary>
public class AssetLoader : ModSystem
{
    public override void Load() => _ = Ass.Initialized;
}

internal sealed class MissingAssetException : MissingResourceException
{
    public MissingAssetException(List<string> missingAssets)
        : base($"--------------\nMOD CRASH! Missing texture assets:\n{string.Join("\n", missingAssets)}\n--------------")
    {
    }
}