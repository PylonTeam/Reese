using Reese.Core.Configs;
using Reese.Core.Debug;
using System.Collections.Generic;
using Terraria.ModLoader;
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
    private bool openReplayHudOnReady;

    public override void OnWorldLoad()
    {
        replayHudInterface = new UserInterface();
        replayHudState = new ReplayHudState();
        openReplayHudOnReady = false;
    }

    public override void OnWorldUnload()
    {
        CloseReplayHud();

        replayHudInterface = null;
        replayHudState = null;
        openReplayHudOnReady = false;
    }

    public override void PreUpdatePlayers()
    {
        //if (ReplaySession.IsReplayPlayback)
            //ForceLocalReplaySpectator();
    }

    public void Rebuild()
    {
        replayHudState?.Rebuild();
    }

    public void RequestOpenReplayHud()
    {
        if (!ReplaySession.IsReplayPlayback)
            return;

        openReplayHudOnReady = true;
        OpenReplayHud();
    }

    public void ToggleReplayHud()
    {
        Log.Chat("ToggleReplayHud() called");

        if (IsReplayHudOpen())
        {
            CloseReplayHud();
            return;
        }

        RequestOpenReplayHud();
    }

    public void OpenReplayHud()
    {
        if (!CanOpenReplayHud())
            return;

        openReplayHudOnReady = false;
        ForceLocalReplaySpectator();

        replayHudState ??= new ReplayHudState();
        replayHudInterface.SetState(replayHudState);
    }

    public void CloseReplayHud()
    {
        openReplayHudOnReady = false;
        replayHudInterface?.SetState(null);
    }

    public bool IsReplayHudOpen()
    {
        return replayHudInterface?.CurrentState != null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (openReplayHudOnReady)
            OpenReplayHud();

        if (IsReplayHudOpen() && !CanOpenReplayHud())
        {
            CloseReplayHud();
            return;
        }

        replayHudInterface?.Update(gameTime);
    }

    private static bool CanOpenReplayHud()
    {
        return ReplaySession.IsReplayPlayback && !Main.gameMenu && Main.myPlayer is >= 0 and < Main.maxPlayers && Main.LocalPlayer?.active == true;
    }

    private static void ForceLocalReplaySpectator()
    {
        if (!ReplaySession.IsReplayPlayback || Main.myPlayer is < 0 or >= Main.maxPlayers)
            return;

        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return;

        local.ghost = true;
        Main.playerInventory = false;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        //Log.Chat(Main.LocalPlayer.ghost);

        if (replayHudInterface?.CurrentState == null)
        {
            return;
        }

        bool configUiOpen = ConfigHelper.IsAnyConfigUIOpen();
        int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        int logicIndex = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");
        int deathTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Death Text");
        int replayHudIndex = GetReplayHudInsertIndex(mouseTextIndex, logicIndex, deathTextIndex, configUiOpen);

        if (replayHudIndex == -1)
            return;

        layers.Insert(replayHudIndex, new LegacyGameInterfaceLayer("Reese: Replay HUD", () =>
        {
            Log.Chat(2);
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
}