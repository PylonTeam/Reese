using Reese.Core.Configs.ConfigElements;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Reese.Core.Configs;

public class ServerConfig : ModConfig
{
    public const int DefaultBaselineIntervalSeconds = 60 * 15;
    public const int MaxBaselineIntervalSeconds = 60 * 5;

    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("Recording")]

    [BackgroundColor(250, 60, 60, 150)]
    [Range(0, MaxBaselineIntervalSeconds)]
    [DefaultValue(DefaultBaselineIntervalSeconds)]
    [Slider]
    [DrawTicks]
    [Increment(30)]
    [ConfigIcon(nameof(Ass.Stopwatch), ConfigIconPlacement.Cut)]
    public int BaselineIntervalSeconds;

    [BackgroundColor(250, 60, 60, 150)]
    [ConfigIcon(nameof(Ass.IconPlayer), nameof(Ass.IconXGray), grayWhenOff: true, placement: ConfigIconPlacement.Cut)]
    [DefaultValue(true)]
    public bool AutoStartRecordingOnEnterWorld;

    [BackgroundColor(250, 60, 60, 150)]
    [ConfigIcon(nameof(Ass.IconPlayerHead), nameof(Ass.IconXGray), grayWhenOff: true, placement: ConfigIconPlacement.Cut)]
    [DefaultValue(true)]
    public bool AutoStartRecordingWhenPlayersAreInWorld;

    [BackgroundColor(250, 60, 60, 150)]
    [DefaultValue(true)]
    [ConfigIcon(nameof(Ass.IconCheckGreen), nameof(Ass.IconXGray), placement: ConfigIconPlacement.Cut)]
    public bool CaptureModsUsedInReplay;

    [BackgroundColor(250, 60, 60, 150)]
    [ConfigIcon(nameof(Ass.IconCamera), ConfigIconPlacement.Cut)]
    [Expand(false, false)]
    public MaxLengthRecordingConfig maxLengthRecordingConfig = new();

    [Header("Spectating")]
    [ConfigIcon(nameof(Ass.Ghost), ConfigIconPlacement.Cut)]
    [BackgroundColor(30, 150, 150)]
    [Expand(false, false)]
    public GhostSpectatingConfig ghostSpectatingConfig = new();

    public class MaxLengthRecordingConfig
    {
        [ConfigIcon(nameof(Ass.IconCheckGreen), nameof(Ass.IconXGray), grayWhenOff: true)]
        [BackgroundColor(250, 60, 60, 150)]
        [DefaultValue(false)]
        public bool EnableMaxLengthRecording;

        [RequiresField(nameof(EnableMaxLengthRecording))]
        [BackgroundColor(250, 60, 60, 150)]
        [DefaultValue(60)]
        [Range(min: 10, max: 60 * 10)]
        public int MaxRecordingLengthMinutes = 60;

        [RequiresField(nameof(EnableMaxLengthRecording))]
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
