using Microsoft.Xna.Framework;

namespace Reese.Common.MainMenu.UI;

// Each mod carries the same API so every style also works when that mod is loaded alone.
internal enum MainMenuTextStyle
{
    OriginalFlame,
    MutedFlame,
    SoftFlame,
    EnergyArcs,
    OrbitalWisps,
    Plain
}

/// <summary>Live preset values. Create is called from the drawer on every draw, so method edits can hot reload.</summary>
internal readonly record struct MainMenuTextThemeSettings
{
    public MainMenuTextThemeSettings() { }

    public MainMenuTextStyle Style { get; init; } = MainMenuTextStyle.OriginalFlame;

    // The three gradient stops and their hover targets; Plain uses BaseColor.
    public Color BaseColor { get; init; } = new(228, 48, 244);
    public Color BottomColor { get; init; } = new(104, 30, 218);
    public Color TopColor { get; init; } = new(255, 202, 250);
    public Color HoverBaseColor { get; init; } = new(228, 48, 244);
    public Color HoverBottomColor { get; init; } = new(104, 30, 218);
    public Color HoverTopColor { get; init; } = new(255, 202, 250);
    public Color OutlineColor { get; init; } = new(26, 6, 42);
    public Color IdleTintColor { get; init; } = new(170, 172, 180);
    public float HoverColorMix { get; init; } = 1f;
    public float IdleTintAmount { get; init; } = 0f;
    public float IdleBrightness { get; init; } = 1f;
    public float HoverBrightness { get; init; } = 1f;
    public float IdleOpacity { get; init; } = 1f;
    public float HoverOpacity { get; init; } = 1f;

    // Animation and effect strength are independent of text opacity.
    public bool GradientEnabled { get; init; } = true;
    public bool AnimationEnabled { get; init; } = true;
    public bool EffectsEnabled { get; init; } = true;
    public float Speed { get; init; } = 1f;
    public float Phase { get; init; } = 0.7f;
    public float IdleEffectStrength { get; init; } = 1f;
    public float HoverEffectStrength { get; init; } = 1.22f;
    public float PulseSpeed { get; init; } = 3.1f;
    public float PulseAmount { get; init; } = 0.06f;

    public MainMenuTextGradientSettings Gradient { get; init; } = new();
    public MainMenuTextGlowSettings Glow { get; init; } = new();
    public MainMenuTextFlameSettings Flame { get; init; } = new();
    public MainMenuTextSparkSettings Sparks { get; init; } = new();
    public MainMenuTextArcSettings Arcs { get; init; } = new();
    public MainMenuTextOrbitSettings Orbit { get; init; } = new();
    public MainMenuTextFalloffSettings Falloff { get; init; } = new();
    public MainMenuTextAssetSettings Assets { get; init; } = new();

    public static MainMenuTextThemeSettings Create(MainMenuTextStyle style, bool reese)
    {
        Color middle = reese ? new Color(255, 42, 52) : new Color(228, 48, 244);
        Color lower = reese ? new Color(174, 8, 30) : new Color(104, 30, 218);
        Color upper = reese ? new Color(255, 205, 174) : new Color(255, 202, 250);
        string path = reese ? "Reese/Assets/Effects/Menu/" : "Pylon/Assets/Effects/VFX/";
        MainMenuTextThemeSettings settings = new()
        {
            Style = style,
            BaseColor = middle,
            BottomColor = lower,
            TopColor = upper,
            HoverBaseColor = middle,
            HoverBottomColor = lower,
            HoverTopColor = upper,
            OutlineColor = reese ? new Color(42, 5, 12) : new Color(26, 6, 42),
            Phase = reese ? 2.8f : 0.7f,
            Assets = new()
            {
                Glow = path + "GlowSoft64",
                Flame = path + "FireSwirl",
                Spark = path + "PixelStarlight",
                Swirl = path + "PixelSwirl",
                Smoke = path + "SmokeWisp4",
                Ribbon = path + "TrailLightningBloom",
                Diamond = path + "PixelDiamondGlow"
            }
        };

        // Presets preserve every previous look. Override any property in GetSettings to customize it.
        if (style == MainMenuTextStyle.OriginalFlame)
            return settings;

        if (style == MainMenuTextStyle.Plain)
            return settings with
            {
                GradientEnabled = false,
                AnimationEnabled = false,
                EffectsEnabled = false,
                IdleBrightness = 0.86f,
                HoverBrightness = 1f,
                Glow = settings.Glow with { Enabled = false }
            };

        settings = settings with
        {
            Speed = 0.75f,
            TopColor = Color.Lerp(middle, upper, 0.45f),
            HoverTopColor = Color.Lerp(middle, upper, 0.45f),
            IdleBrightness = 0.86f,
            HoverBrightness = 0.96f,
            IdleEffectStrength = 0.08f,
            HoverEffectStrength = 0.18f,
            PulseSpeed = 2f,
            PulseAmount = 0.03f,
            Gradient = settings.Gradient with { IdleShine = 0.06f, HoverShine = 0.06f, HoverHighlight = 0f },
            Glow = settings.Glow with
            {
                InnerRadius = 1.7f, OuterRadius = 3.8f,
                InnerOpacity = 0.105f, OuterOpacity = 0.047f
            },
            Flame = settings.Flame with
            {
                Count = 7, Steps = 9, Width = 10f, TipWidth = 2.5f,
                Rise = 5f, RiseVariation = 3f, HeightFlicker = 1.5f,
                Strength = 0.28f, TipOpacity = 0f, SmoothTips = true,
                SwirlCount = 2, SwirlOpacity = 0.18f, SwirlHeight = 20f, SwirlHeightWave = 2f, SwirlDrift = 2f
            },
            Sparks = settings.Sparks with
            {
                Count = 10, Rise = 8f, RiseVariation = 3f, Margin = 6f,
                GlowOpacity = 0.35f, CoreOpacity = 0.30f, TrailOpacity = 0.12f,
                CoreSize = new Vector2(2.4f, 3.2f), CoreSizeVariation = Vector2.One, TrailSize = new Vector2(2f, 4f),
                HardCore = false, StarCount = 2, StarSize = 7f, StarSizeWave = 4f, StarOpacity = 0.28f
            },
            Falloff = settings.Falloff with { Enabled = true }
        };

        return style switch
        {
            MainMenuTextStyle.MutedFlame => Create(MainMenuTextStyle.OriginalFlame, reese) with
            {
                Style = MainMenuTextStyle.MutedFlame,
                Speed = 0.75f,
                PulseAmount = 0.03f,
                IdleTintAmount = 0.68f,
                IdleBrightness = 1f,
                HoverBrightness = 1f,
                IdleOpacity = 0.86f,
                TopColor = upper,
                HoverTopColor = upper,
                IdleEffectStrength = 0.20f,
                HoverEffectStrength = 0.75f,
                PulseSpeed = 3.1f,
                Gradient = settings.Gradient with { IdleShine = 0.18f, HoverShine = 0.18f, HoverHighlight = 0.08f },
                Flame = new(),
                Sparks = new(),
                Falloff = settings.Falloff with { Enabled = false }
            },
            MainMenuTextStyle.EnergyArcs => settings with
            {
                HoverBaseColor = reese ? new Color(255, 108, 98) : Color.Lerp(middle, upper, 0.45f),
                HoverBottomColor = reese ? new Color(255, 46, 62) : Color.Lerp(lower, middle, 0.65f),
                HoverTopColor = upper,
                HoverBrightness = 1f,
                IdleEffectStrength = 0.05f,
                HoverEffectStrength = 0.50f,
                Gradient = settings.Gradient with { HoverShine = 0.22f },
                Glow = settings.Glow with { BackdropStrength = 0.32f, BackdropPadding = new Vector2(14f) },
                Falloff = settings.Falloff with { Padding = new Vector2(10f, 18f) }
            },
            MainMenuTextStyle.OrbitalWisps => settings with
            {
                IdleEffectStrength = 0.10f,
                HoverEffectStrength = 0.46f,
                PulseSpeed = 1.6f,
                PulseAmount = 0.04f,
                Glow = settings.Glow with { BackdropStrength = 0.25f, BackdropPadding = new Vector2(20f, 10f) },
                Falloff = settings.Falloff with { Padding = new Vector2(24f, 15f) },
                Assets = settings.Assets with { Spark = path + "PixelTwinkle" }
            },
            _ => settings
        };
    }
}

internal readonly record struct MainMenuTextGradientSettings
{
    public MainMenuTextGradientSettings() { }
    public float Split { get; init; } = 0.46f;
    public float WaveSpeed { get; init; } = 2.1f;
    public float WaveScale { get; init; } = 5.2f;
    public float WaveStrength { get; init; } = 0.07f;
    public float ShineSpeed { get; init; } = 1.9f;
    public float ShineScale { get; init; } = 5f;
    public float ShinePower { get; init; } = 12f;
    public float IdleShine { get; init; } = 0.28f;
    public float HoverShine { get; init; } = 0.28f;
    public float HoverHighlight { get; init; } = 0.10f;
    public int StripHeight { get; init; } = 2;
}

internal readonly record struct MainMenuTextGlowSettings
{
    public MainMenuTextGlowSettings() { }
    public bool Enabled { get; init; } = true;
    public float InnerRadius { get; init; } = 1.7f;
    public float OuterRadius { get; init; } = 3.8f;
    public float InnerOpacity { get; init; } = 0.105f;
    public float OuterOpacity { get; init; } = 0.047f;
    public int Directions { get; init; } = 8;
    public bool OutlineEnabled { get; init; } = true;
    public float OutlineRadius { get; init; } = 0.9f;
    public int OutlineDirections { get; init; } = 8;
    public float BackdropStrength { get; init; } = 0.42f;
    public Vector2 BackdropPadding { get; init; } = new(18f, 20f);
}

internal readonly record struct MainMenuTextFlameSettings
{
    public MainMenuTextFlameSettings() { }
    public int Count { get; init; } = 14;
    public int Steps { get; init; } = 9;
    public float Width { get; init; } = 10f;
    public float TipWidth { get; init; } = 2.5f;
    public float Rise { get; init; } = 10f;
    public float RiseVariation { get; init; } = 7f;
    public float HeightFlicker { get; init; } = 3f;
    public float MotionSpeed { get; init; } = 2.5f;
    public float MotionVariation { get; init; } = 1.8f;
    public float Bend { get; init; } = 4f;
    public float RootOffset { get; init; } = 2f;
    public float RootJitter { get; init; } = 7f;
    public float Strength { get; init; } = 0.4f;
    public float TipOpacity { get; init; } = 0.4f;
    public bool SmoothTips { get; init; } = false;
    public int SwirlCount { get; init; } = 4;
    public float SwirlWidthFactor { get; init; } = 0.42f;
    public float SwirlHeight { get; init; } = 27f;
    public float SwirlHeightWave { get; init; } = 3f;
    public float SwirlDrift { get; init; } = 3f;
    public float SwirlSpeed { get; init; } = 1.5f;
    public float SwirlSpin { get; init; } = 0.4f;
    public float SwirlOpacity { get; init; } = 0.38f;
}

internal readonly record struct MainMenuTextSparkSettings
{
    public MainMenuTextSparkSettings() { }
    public int Count { get; init; } = 28;
    public float Speed { get; init; } = 0.5f;
    public float SpeedVariation { get; init; } = 0.48f;
    public float Rise { get; init; } = 17f;
    public float RiseVariation { get; init; } = 7f;
    public float Margin { get; init; } = 6f;
    public float Wobble { get; init; } = 2f;
    public float LifeWobble { get; init; } = 4f;
    public Vector2 GlowSize { get; init; } = new(5f, 7f);
    public Vector2 GlowSizeVariation { get; init; } = new(3f, 4f);
    public Vector2 CoreSize { get; init; } = new(0.7f, 1.2f);
    public Vector2 CoreSizeVariation { get; init; } = new(0.8f, 1.6f);
    public Vector2 TrailSize { get; init; } = new(1.6f, 6f);
    public float TrailOffset { get; init; } = 3f;
    public float GlowOpacity { get; init; } = 0.35f;
    public float CoreOpacity { get; init; } = 0.76f;
    public float TrailOpacity { get; init; } = 0.24f;
    public bool HardCore { get; init; } = true;
    public int StarCount { get; init; } = 5;
    public float StarSize { get; init; } = 10f;
    public float StarSizeWave { get; init; } = 7f;
    public float StarOpacity { get; init; } = 0.65f;
    public float StarSpeed { get; init; } = 1.5f;
    public float StarSpeedVariation { get; init; } = 1f;
    public float StarSpin { get; init; } = 0.2f;
}

internal readonly record struct MainMenuTextArcSettings
{
    public MainMenuTextArcSettings() { }
    public int Count { get; init; } = 2;
    public int Segments { get; init; } = 16;
    public float WidthPadding { get; init; } = 16f;
    public float TopOffset { get; init; } = -3f;
    public float BottomOffset { get; init; } = 5f;
    public float TopHeight { get; init; } = 22f;
    public float BottomHeight { get; init; } = 18f;
    public float WaveSpeed { get; init; } = 0.7f;
    public float WaveHeight { get; init; } = 2.5f;
    public float GlowOpacity { get; init; } = 0.45f;
    public float RibbonOpacity { get; init; } = 1.25f;
    public int GlintCount { get; init; } = 3;
    public float GlintSpeed { get; init; } = 0.55f;
    public float GlintPulseSpeed { get; init; } = 1.3f;
    public Vector2 GlintSize { get; init; } = new(3.5f, 7f);
    public Vector2 GlintSizeWave { get; init; } = new(1f, 3f);
    public float GlintOpacity { get; init; } = 0.65f;
}

internal readonly record struct MainMenuTextOrbitSettings
{
    public MainMenuTextOrbitSettings() { }
    public int WispCount { get; init; } = 2;
    public float SideOffset { get; init; } = 3f;
    public float Drift { get; init; } = 3f;
    public float DriftSpeed { get; init; } = 0.45f;
    public Vector2 SmokeSize { get; init; } = new(38f, 29f);
    public float SmokeOpacity { get; init; } = 0.24f;
    public float SmokeSpeed { get; init; } = 0.11f;
    public Vector2 SwirlSize { get; init; } = new(19f, 24f);
    public float SwirlOpacity { get; init; } = 0.70f;
    public float SwirlSpeed { get; init; } = 0.38f;
    public Vector2 WispGlowSize { get; init; } = new(24f);
    public float WispGlowOpacity { get; init; } = 0.32f;
    public float BreatheSpeed { get; init; } = 1.4f;
    public float BreatheAmount { get; init; } = 0.06f;
    public int SatelliteCount { get; init; } = 2;
    public float SatelliteSpeed { get; init; } = 0.52f;
    public Vector2 RadiusPadding { get; init; } = new(7f, 3f);
    public int TrailSteps { get; init; } = 6;
    public float TrailSpacing { get; init; } = 0.1f;
    public Vector2 TrailSize { get; init; } = new(4f);
    public Vector2 TrailSizeWave { get; init; } = new(4f, 3f);
    public float TrailOpacity { get; init; } = 0.55f;
    public float StarSize { get; init; } = 11f;
    public float StarOpacity { get; init; } = 0.85f;
    public float StarSpin { get; init; } = 0.15f;
    public float StarPulseSpeed { get; init; } = 1.7f;
    public float StarPulseAmount { get; init; } = 0.14f;
}

internal readonly record struct MainMenuTextFalloffSettings
{
    public MainMenuTextFalloffSettings() { }
    public bool Enabled { get; init; } = false;
    public Vector2 Padding { get; init; } = new(10f, 12f);
    public Vector2 Start { get; init; } = new(0.5f, 0.4f);
}

internal readonly record struct MainMenuTextAssetSettings
{
    public MainMenuTextAssetSettings() { }
    // Full mod asset paths, without file extensions. Empty or missing paths disable that texture.
    public string Glow { get; init; } = "";
    public string Flame { get; init; } = "";
    public string Spark { get; init; } = "";
    public string Swirl { get; init; } = "";
    public string Smoke { get; init; } = "";
    public string Ribbon { get; init; } = "";
    public string Diamond { get; init; } = "";
}

