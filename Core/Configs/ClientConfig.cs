using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reese.Common.Replayer.ReplayHud;
using Reese.Core.Debug;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Reese.Core.Configs;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public enum ReplayHudPosition
    {
        Top,
        Bottom,
    }

    public enum ReplayHudSize
    {
        Small,
        Medium,
        Large,
    }

    [Header("MainMenu")]

    [BackgroundColor(30, 150, 30)]
    [DefaultValue(true)]
    public bool AddExtraMenuState = true;

    [BackgroundColor(30, 150, 30)]
    [DefaultValue(true)]
    public bool ShowInMainMenu = true;

    [Header("Replaying")]

    [BackgroundColor(200, 60, 60, 230)]
    [DefaultValue(true)] public bool ShowWelcomeMessageOnEnterWorld;

    [BackgroundColor(200, 60, 60, 230)]
    [DefaultValue(true)]
    public bool ShowCameraFade = true;

    [Header("UI")]
    [BackgroundColor(30, 150, 150)]
    [DefaultValue(ReplayHudPosition.Top)]
    [JsonConverter(typeof(StringEnumConverter))]
    public ReplayHudPosition replayHudPosition;

    [BackgroundColor(30, 150, 150)]
    [DefaultValue(ReplayHudSize.Medium)]
    [JsonConverter(typeof(StringEnumConverter))]
    public ReplayHudSize replayHudSize;

    [Header("Debug")]
    [BackgroundColor(150, 150, 150, 150)]
    [DefaultValue(false)] public bool ShowDebugMessages;

    [BackgroundColor(150, 150, 150, 150)]
    [DefaultValue(false)] public bool ShowDebugDrawer;

    // Temporary options set to false to hide them from users until they are ready to be used.
    // These will likely be removed in the future once the features they relate to are fully implemented and ready for use.
    [BackgroundColor(150, 150, 150, 150)]
    [DefaultValue(false)] public bool IsSeekbarEnabled;

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
