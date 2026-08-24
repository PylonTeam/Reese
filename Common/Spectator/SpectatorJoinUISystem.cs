using System.Collections.Generic;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.Spectator;

[Autoload(Side = ModSide.Client)]
public sealed class SpectatorJoinUISystem : ModSystem
{
    // UI
    private UserInterface ui;
    private SpectatorJoinUIState joinState;

    // Enabled check
    public static bool IsEnabled
    {
        get
        {
            return SpectatorModeSystem.IsGhostSpectatingEnabled &&
                   SpectatorModeSystem.ShowSpectatorJoinPanel &&
                   !global::Reese.Common.Replay.ReplayPlayback.IsReplayPlayback;
        }
    }

    public override void OnWorldLoad()
    {
        ui = new();
        joinState = new();

        if (!IsEnabled)
            Close();
    }

    public void Open()
    {
        if (!IsEnabled)
            return;

        ui ??= new();
        joinState ??= new();
        ui.SetState(joinState);
    }

    public void Toggle()
    {
        // toggle join UI.
        if (ui?.CurrentState == null)
            Open();
        else
            ui?.SetState(null);
    }

    public void Close()
    {
        if (ui?.CurrentState != null)
            ui?.SetState(null);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        //var ss = ModContent.GetInstance<SpawnSelector.SpawnSystem>();
        //if (ss.ui.CurrentState != null)
        //{
        //    if (Interface.CurrentState != null)
        //    {
        //        Interface.SetState(null);
        //    }
        //}

        ui?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (ui?.CurrentState == null)
            return;

        int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        if (index == -1)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "Reese: Spectator Join UI",
            () =>
            {
                ui.Draw(Main.spriteBatch, new GameTime());
                return true;
            },
            InterfaceScaleType.UI));
    }
}
