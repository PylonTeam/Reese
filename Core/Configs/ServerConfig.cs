using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Reese.Core.Configs;

public class ServerConfig : ModConfig
{
    public const bool DefaultAutoStartRecordingOnEnterWorld = false;
    public const int DefaultBaselineIntervalTicks = 1800;
    public const int MaxBaselineIntervalTicks = 60 * 60 * 60;
    public const int DefaultMaxRecordingLengthMinutes = 0;

    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("Recording")]
    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(DefaultAutoStartRecordingOnEnterWorld)]
    public bool AutoStartRecordingOnEnterWorld = DefaultAutoStartRecordingOnEnterWorld;

    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(DefaultMaxRecordingLengthMinutes)]
    public int MaxRecordingLengthMinutes = DefaultMaxRecordingLengthMinutes;

    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(false)]
    public bool AutoStartRecordingAfterMaxLength;

    // Shorter baseline intervals make replay seeking smoother and quicker, but increase file size quickly.
    // Larger baseline intervals keep file size small, but make replay seeking take longer.
    [BackgroundColor(250, 60, 60, 150)]
    [Range(0, MaxBaselineIntervalTicks)]
    [DefaultValue(DefaultBaselineIntervalTicks)]
    public int BaselineIntervalTicks = DefaultBaselineIntervalTicks;
}
