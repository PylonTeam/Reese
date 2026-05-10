using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reese.Common.ReplaySpectate.UI;
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
    [DefaultValue(false)]
    public bool ShowInMainMenu = false;

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

    [BackgroundColor(150, 150, 150, 150)]
    [Header("Chat")]
    [DefaultValue(false)] public bool ShowDebugMessages;

    #region Methods
    public override void OnChanged()
    {
        base.OnChanged();
        Log.Chat("Client config changed");

        // Rebuild spectate UI
        var spectateUISystem = ModContent.GetInstance<SpectatorUISystem>();
        spectateUISystem?.RebuildUI();
    }
    #endregion
}
