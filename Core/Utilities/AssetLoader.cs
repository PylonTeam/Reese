using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace Reese.Core.Utilities;

/// <summary>
/// Provides static access to miscallaneous texture assets within the Reese mod.
/// Automatically initializes when the mod system loads.
/// All asset fields are intended for global access throughout the mod.
/// Here we store the registry of our assets
/// </summary>
public static class Ass
{
    // Replay tool assets
    public static Asset<Texture2D> Icon_Arrow;
    public static Asset<Texture2D> Icon_Camera;
    public static Asset<Texture2D> Icon_CameraSmall;
    public static Asset<Texture2D> Icon_Chest;
    public static Asset<Texture2D> Icon_NextFrame;
    public static Asset<Texture2D> Icon_Pause;
    public static Asset<Texture2D> Icon_Play;
    public static Asset<Texture2D> Icon_Reset;
    public static Asset<Texture2D> Icon_Refresh;
    public static Asset<Texture2D> Icon_Resize;
    public static Asset<Texture2D> Icon_Stop;
    public static Asset<Texture2D> Icon_SpeedUp;
    public static Asset<Texture2D> Icon_SpeedDown;
    public static Asset<Texture2D> Icon_TeamAssigner;
    public static Asset<Texture2D> Icon_Watch;
    public static Asset<Texture2D> Slider;
    public static Asset<Texture2D> SliderHighlight;
    public static Asset<Texture2D> SliderGradient;

    // Main menu assets
    public static Asset<Texture2D> ButtonOpenFolder;
    public static Asset<Texture2D> ButtonRefresh;
    public static Asset<Texture2D> ButtonTableColumn;
    public static Asset<Texture2D> ButtonTableColumn_Border;

    // Ghost spectate assets
    public static Asset<Texture2D> Biome_Shimmer;
    public static Asset<Texture2D> GhostRight;
    public static Asset<Texture2D> GhostLeft;
    public static Asset<Texture2D> MapBG_Shimmer;
    public static Asset<Texture2D> Icon_Biome;
    public static Asset<Texture2D> Icon_CandelabraOn;
    public static Asset<Texture2D> Icon_CandelabraOff;
    public static Asset<Texture2D> Icon_Card1;
    public static Asset<Texture2D> Icon_Card2;
    public static Asset<Texture2D> Icon_Card3;
    public static Asset<Texture2D> Icon_CheckmarkGreen;
    public static Asset<Texture2D> Icon_Dead;
    public static Asset<Texture2D> Icon_Distance;
    public static Asset<Texture2D> Icon_Eye;
    public static Asset<Texture2D> Icon_EyeOff;
    public static Asset<Texture2D> Icon_FilmProjectorOn;
    public static Asset<Texture2D> Icon_FilmProjectorOff;
    public static Asset<Texture2D> Icon_GhostTeleport;
    public static Asset<Texture2D> Icon_HeldItem;
    public static Asset<Texture2D> Icon_InventoryOpen;
    public static Asset<Texture2D> Icon_InventoryClosed;
    public static Asset<Texture2D> Icon_MapOn;
    public static Asset<Texture2D> Icon_MapOff;
    public static Asset<Texture2D> Icon_NPC;
    public static Asset<Texture2D> Icon_Player;
    public static Asset<Texture2D> Icon_PlayerHead;
    public static Asset<Texture2D> Icon_TeleportOn;
    public static Asset<Texture2D> Icon_TeleportOff;
    public static Asset<Texture2D> Icon_Time;
    public static Asset<Texture2D> Icon_World;

    // Map backgrounds
    public static Asset<Texture2D>[] MapBG;

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

        MapBG = new Asset<Texture2D>[42];

        for (int i = 1; i <= MapBG.Length; i++)
            MapBG[i - 1] = RequestTexture($"MapBG{i}", $"{ModName}/Assets/MapBGs/MapBG{i}");

        FieldInfo[] fields = typeof(Ass).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType != typeof(Asset<Texture2D>))
                continue;

            field.SetValue(null, RequestTexture(field.Name, $"{ModName}/Assets/{field.Name}"));
        }

        Initialized = true;
    }

    private static Asset<Texture2D> RequestTexture(string assetName, string path)
    {
        if (!ModContent.HasAsset(path))
            throw new MissingAssetException(assetName, [path]);

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

internal sealed class MissingAssetException : Exception
{
    public MissingAssetException(string fieldName, string[] searchedPaths)
        : base($"--------------\nMOD CRASH! Missing texture asset for Ass.{fieldName}. Searched: {string.Join(", ", searchedPaths)}\n")
    {
    }
}