using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reese.Common.Replay.Hud;
using Reese.Common.Replay.Hud.Ghost;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Reese.Core.Configs;

public class ClientConfig : ModConfig
{
    public const int DefaultSeekMaxMillisecondsPerFrame = 8;
    public const int MinSeekMaxMillisecondsPerFrame = 1;
    public const int MaxSeekMaxMillisecondsPerFrame = 80;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("MainMenu")]

    [BackgroundColor(30, 150, 30)]
    [DefaultValue(true)]
    public bool AddExtraMenuState = true;

    [Header("Replay")]
    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(true)] public bool ShowWelcomeMessageOnEnterWorld;

    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(true)] public bool TryLoadModsUsedInReplay = true;

    // Smooth = 8 ms, Fast = 16-32 ms, Snappy = 50-100 ms, Laggy = 100+ (only do on strong computers)
    [BackgroundColor(250, 60, 60, 150)]
    [Range(MinSeekMaxMillisecondsPerFrame, MaxSeekMaxMillisecondsPerFrame)]
    [Slider]
    [DrawTicks]
    [Increment(8)]
    [DefaultValue(DefaultSeekMaxMillisecondsPerFrame)]
    public int SeekMaxMillisecondsPerFrame = DefaultSeekMaxMillisecondsPerFrame;

    [Header("Debug")]
    [BackgroundColor(150, 150, 150, 150)]
    [DefaultValue(false)] public bool ShowDebugMessages;

    #region Methods
    public override void OnChanged()
    {
        base.OnChanged();
        Log.Chat("Client config changed");

        // Rebuild replay HUD
        var replayHudSystem = ModContent.GetInstance<ReplayHudSystem>();
        replayHudSystem?.Rebuild();

        var ghostHudSystem = ModContent.GetInstance<GhostHudSystem>();
        ghostHudSystem?.Rebuild();
    }
    #endregion
}
