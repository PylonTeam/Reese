using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud;

/// <summary>
/// Shows all the HUD elements related to replay spectating:
/// Bottom HUD: replay controls
/// Top HUD: player spectating controls
/// Right HUD: replay info and stats
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class ReplayHudSystem : ModSystem
{
    private UserInterface replayHudInterface;
    private ReplayHudState replayHudState;

    public override void Load()
    {
        replayHudInterface = new UserInterface();
        replayHudState = new ReplayHudState();
    }

    public override void Unload()
    {
        replayHudInterface = null;
        replayHudState = null;
    }

    public void Rebuild()
    {
        replayHudState?.Rebuild();
    }

    public void ToggleReplayHud()
    {
        Log.Chat("ToggleReplayHud() called");

        if (IsReplayHudOpen())
        {
            CloseFullHud();
        }
        else
        {
            OpenFullHud();
        }
    }

    public void OpenFullHud()
    {
        if (!ReplayPlayback.IsReplayPlayback)
            return;

        replayHudInterface.SetState(replayHudState);
        Log.Chat("Replay HUD opened.");
    }

    public void CloseFullHud()
    {
        replayHudInterface?.SetState(null);
    }

    public bool IsReplayHudOpen()
    {
        return replayHudInterface?.CurrentState != null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        replayHudInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        // Debug stuff here
        //Main.LocalPlayer.ghost = true;

        if (replayHudInterface?.CurrentState == null)
            return;

        bool configUiOpen = ConfigHelper.IsAnyConfigUIOpen();
        int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        int logicIndex = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");
        int deathTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Death Text");
        int replayHudIndex = GetReplayHudInsertIndex(mouseTextIndex, logicIndex, deathTextIndex, configUiOpen);

        if (replayHudIndex == -1)
            return;

        layers.Insert(replayHudIndex, new LegacyGameInterfaceLayer("Reese: Replay HUD", () =>
        {
            replayHudInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
            return true;
        }, InterfaceScaleType.UI));
    }

    private static int GetReplayHudInsertIndex(int mouseTextIndex, int logicIndex, int deathTextIndex, bool configUiOpen)
    {
        if (mouseTextIndex != -1)
            return mouseTextIndex;

        if (configUiOpen && logicIndex != -1)
            return logicIndex;

        return deathTextIndex;
    }

    // Close huds
    public void CloseSpectateHud()
    {
        replayHudState?.CloseSpectateHud();
    }

    public void CloseInfoHud()
    {
        replayHudState?.CloseInfoHud();
    }

    public void ClosePlaybackHud()
    {
        replayHudState?.ClosePlaybackHud();
    }
}