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
    [Range(0, MaxBaselineIntervalTicks)]
    [DefaultValue(DefaultBaselineIntervalTicks)] public int BaselineIntervalTicks = DefaultBaselineIntervalTicks;

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
