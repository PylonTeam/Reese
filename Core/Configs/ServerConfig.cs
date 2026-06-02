using Reese.Core.Configs.ConfigElements;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Reese.Core.Configs;

public class ServerConfig : ModConfig
{
    public const int DefaultBaselineIntervalSeconds = 30;
    public const int MaxBaselineIntervalSeconds = 60 * 5;

    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("Recording")]
    [BackgroundColor(250, 60, 60, 150)]
    [ConfigIcon(nameof(Ass.IconCameraSmall), ConfigIconPlacement.Cut)]
    [Expand(false, false)]
    public AutoRecordingConfig autoRecordingConfig = new();

    [BackgroundColor(250, 60, 60, 150)]
    [Range(0, MaxBaselineIntervalSeconds)]
    [DefaultValue(DefaultBaselineIntervalSeconds)]
    [Slider]
    [DrawTicks]
    [Increment(30)]
    [ConfigIcon(nameof(Ass.Stopwatch), ConfigIconPlacement.Cut)]
    public int BaselineIntervalSeconds = DefaultBaselineIntervalSeconds;

    [Header("Spectating")]
    [ConfigIcon(nameof(Ass.Ghost), ConfigIconPlacement.Cut)]
    [BackgroundColor(30, 150, 150)]
    [Expand(false, false)]
    public GhostSpectatingConfig ghostSpectatingConfig = new();

    public class AutoRecordingConfig
    {
        [BackgroundColor(250, 60, 60, 150)]
        [DefaultValue(true)]
        public bool AutoStartRecordingOnEnterWorld = true;

        [BackgroundColor(250, 60, 60, 150)]
        [DefaultValue(60)]
        public int MaxRecordingLengthMinutes = 60;

        [BackgroundColor(250, 60, 60, 150)]
        [DefaultValue(true)]
        public bool AutoStartRecordingAfterMaxLength;
    }

    public class GhostSpectatingConfig
    {
        [ConfigIcon(nameof(Ass.IconCheckGreen), nameof(Ass.IconXGray), grayWhenOff: true)]
        [BackgroundColor(30, 150, 150)]
        [DefaultValue(true)]
        public bool IsGhostSpectatingEnabled = true;

        [RequiresField(nameof(IsGhostSpectatingEnabled))]
        [BackgroundColor(30, 150, 150)]
        [DefaultValue(false)]
        public bool ForceSpectateWhenJoining;

        [RequiresField(nameof(IsGhostSpectatingEnabled))]
        [BackgroundColor(30, 150, 150)]
        [DefaultValue(false)]
        public bool ShowSpectatorJoinPanel;

        [RequiresField(nameof(IsGhostSpectatingEnabled))]
        [BackgroundColor(30, 150, 150)]
        [DefaultValue(false)]
        public bool DrawGhosts;

        [RequiresField(nameof(IsGhostSpectatingEnabled))]
        [BackgroundColor(30, 150, 150)]
        [DefaultValue(false)]
        public bool DrawGhostsNameplates;
    }

}
