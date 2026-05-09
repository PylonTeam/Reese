using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Reese.Common.GhostSpectate.UI;
using Reese.Core.Debug;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Reese.Core.Configs;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public enum SpectateUIPosition
    {
        Top,
        Bottom,
    }

    public enum SpectateUISize
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

    [BackgroundColor(30, 150, 30)]
    [DefaultValue(true)]
    public bool ShowCameraFade = true;

    [Header("UI")]
    [BackgroundColor(30, 150, 150)]
    [DefaultValue(SpectateUIPosition.Top)]
    [JsonConverter(typeof(StringEnumConverter))]
    public SpectateUIPosition spectateUIPosition;

    [BackgroundColor(30, 150, 150)]
    [DefaultValue(SpectateUISize.Small)]
    [JsonConverter(typeof(StringEnumConverter))]
    public SpectateUISize spectateUISize;

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
