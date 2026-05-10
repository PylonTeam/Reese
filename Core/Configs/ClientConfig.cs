using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reese.Common.Replayer.ReplaySpectate.UI;
using Reese.Core.Debug;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Reese.Core.Configs;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public enum ReplayHUDPosition
    {
        Top,
        Bottom,
    }

    public enum ReplayHUDSize
    {
        Small,
        Medium,
        Big,
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
    [DefaultValue(true)]
    public bool ShowCameraFade = true;

    [Header("UI")]
    [BackgroundColor(30, 150, 150)]
    [DefaultValue(ReplayHUDPosition.Top)]
    [JsonConverter(typeof(StringEnumConverter))]
    public ReplayHUDPosition replayHUDPosition;

    [BackgroundColor(30, 150, 150)]
    [DefaultValue(ReplayHUDSize.Medium)]
    [JsonConverter(typeof(StringEnumConverter))]
    public ReplayHUDSize replayHUDSize;

    [Header("Debug")]
    [BackgroundColor(150, 150, 150, 150)]
    [DefaultValue(false)] public bool ShowDebugMessages;

    // Temporary options set to false to hide them from users until they are ready to be used.
    // These will likely be removed in the future once the features they relate to are fully implemented and ready for use.
    [BackgroundColor(150, 150, 150, 150)]
    [DefaultValue(false)] public bool IsSeekbarEnabled;

    #region Methods
    public override void OnChanged()
    {
        base.OnChanged();
        Log.Chat("Client config changed");

        // Rebuild spectate UI
        var spectateUISystem = ModContent.GetInstance<ReplayUISystem>();
        spectateUISystem?.RebuildUI();
    }
    #endregion
}
