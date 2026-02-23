using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace Reese.Core;

/// <summary>
/// Provides static access to miscallaneous texture assets within the Reese mod.
/// Automatically initializes when the mod system loads.
/// All asset fields are intended for global access throughout the mod.
/// Here we store the registry of our assets
/// </summary>
public static class Ass
{
    // --- Assets start here --- (prefer alphabetical order for readability)

    // Icons
    public static Asset<Texture2D> Icon_NextFrame;
    public static Asset<Texture2D> Icon_Pause;
    public static Asset<Texture2D> Icon_Play;
    public static Asset<Texture2D> Icon_Reset;
    public static Asset<Texture2D> Icon_Resize;
    public static Asset<Texture2D> Icon_Stop;
    public static Asset<Texture2D> Icon_SpeedUp;
    public static Asset<Texture2D> Icon_SpeedDown;
    public static Asset<Texture2D> Icon_TeamAssigner;
    public static Asset<Texture2D> Icon_Watch;

    // Sliders
    public static Asset<Texture2D> Slider;
    public static Asset<Texture2D> SliderHighlight;
    public static Asset<Texture2D> SliderGradient;

    /// --- Special Initialization flag, do not touch ---
    public static bool Initialized { get; set; }

    /// <summary>
    /// Initializes static assets
    /// Automatically runs once the mod system loads via <see cref="AssetLoader"/>
    /// </summary>
    static Ass()
    {
        // Load all assets from Assets/Custom
        foreach (FieldInfo f in typeof(Ass).GetFields())
        {
            if (f.FieldType == typeof(Asset<Texture2D>))
            {
                var asset = ModContent.Request<Texture2D>(
                    $"Reese/Assets/{f.Name}",
                    AssetRequestMode.AsyncLoad);
                f.SetValue(null, asset);
            }
        }
    }
}

/// <summary>
/// Initializes asset loading for the mod when the system is loaded with all assets in <see cref="Ass"/>
/// </summary>
public class AssetLoader : ModSystem
{
    public override void Load() => _ = Ass.Initialized;
}
