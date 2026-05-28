using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reese.Common.Replayer.ReplayHud;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Reese.Core.Configs;

public class ClientConfig : ModConfig
{
    public const int DefaultBaselineIntervalTicks = 1800;
    public const int MaxBaselineIntervalTicks = 60 * 60 * 60;
    public const int DefaultMaxRecordingLengthMinutes = 0;
    public const int DefaultSeekMaxMillisecondsPerFrame = 8;
    public const int MinSeekMaxMillisecondsPerFrame = 1;
    public const int MaxSeekMaxMillisecondsPerFrame = 100;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("MainMenu")]

    [BackgroundColor(30, 150, 30)]
    [DefaultValue(true)]
    public bool AddExtraMenuState = true;

    [BackgroundColor(30, 150, 30)]
    [DefaultValue(true)]
    public bool ShowInMainMenu = true;

    [Header("Replay")]
    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(true)] public bool ShowWelcomeMessageOnEnterWorld;

    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(true)] public bool AutoStartRecordingOnEnterWorld = true;

    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(DefaultMaxRecordingLengthMinutes)] public int MaxRecordingLengthMinutes = DefaultMaxRecordingLengthMinutes;

    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(false)] public bool AutoStartRecordingAfterMaxLength;

    // Shorter baseline intervals makes replay smoother + quicker but adds file size super quick
    // Larger baseline intervals keep file size small but makes replay seeking take longer

    // Recommendations: 
    // - Stay between 900 and 18,000. Anything outside this range is buggy.
    // - If you set baseline intervals higher also set MinSeekMaxMilliSecondsPerFrame higher for faster seeking
    [BackgroundColor(250, 60, 60, 150)]
    [Range(0, MaxBaselineIntervalTicks)]
    [DefaultValue(DefaultBaselineIntervalTicks)] public int BaselineIntervalTicks = DefaultBaselineIntervalTicks;


    // Smooth = 8 ms, Fast = 16-32 ms, Snappy = 50-100 ms, Laggy = 100+ (only do on strong computers)
    [BackgroundColor(250, 60, 60, 150)]
    [Range(MinSeekMaxMillisecondsPerFrame, MaxSeekMaxMillisecondsPerFrame)]
    [DefaultValue(DefaultSeekMaxMillisecondsPerFrame)] public int SeekMaxMillisecondsPerFrame = DefaultSeekMaxMillisecondsPerFrame;

    [Header("Debug")]
    [BackgroundColor(150, 150, 150, 150)]
    [DefaultValue(false)] public bool ShowDebugMessages;

    [BackgroundColor(150, 150, 150, 150)]
    [DefaultValue(false)] public bool ShowDebugDrawer;

    [BackgroundColor(150, 150, 150, 150)]
    [DefaultValue(false)] public bool EnableBackwardsSeeking;

    #region Methods
    public override void OnChanged()
    {
        base.OnChanged();
        Log.Chat("Client config changed");

        // Rebuild replay HUD
        var replayHudSystem = ModContent.GetInstance<ReplayHudSystem>();
        replayHudSystem?.Rebuild();
    }
    #endregion
}
