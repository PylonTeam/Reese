using Reese.Common.Replayer.ReplaySpectate.SpectatorMode;
using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplaySpectate.UI;

[Autoload(Side = ModSide.Client)]
public class ReplayUISystem : ModSystem
{
    private UserInterface spectatorInterface;
    private ReplayUIState spectatorState;

    private UserInterface replayControlsInterface;
    private UIState replayControlsState;
    private ReplayControlsPanel replayControlsPanel;

    public override void OnWorldLoad()
    {
        spectatorInterface = new UserInterface();
        spectatorState = new ReplayUIState();
        spectatorInterface.SetState(spectatorState);

        replayControlsInterface = new UserInterface();
        replayControlsState = null;
        replayControlsPanel = null;
    }

    public override void OnWorldUnload()
    {
        CloseAllReplayUI();

        spectatorInterface = null;
        spectatorState = null;
        replayControlsInterface = null;
    }

    public void RebuildUI()
    {
        spectatorState?.Rebuild();
    }

    public void ToggleSpectatorInfoPanel()
    {
        if (spectatorInterface?.CurrentState == null)
            OpenSpectatorUI();

        spectatorState?.ToggleInfoPanel();
    }

    public void OnLocalModeAccepted(SpectateMode mode)
    {
        spectatorState?.OnLocalModeAccepted(mode);
    }

    public bool IsMouseOverSpectatorUI()
    {
        return spectatorState?.IsMouseOverVisiblePanel() == true || IsMouseOverReplayControls();
    }

    public bool IsAnyReplayUIOpen()
    {
        return spectatorInterface?.CurrentState != null || IsReplayControlsActive();
    }

    public void ToggleAllReplayUI()
    {
        if (IsAnyReplayUIOpen())
        {
            CloseAllReplayUI();
            return;
        }

        OpenAllReplayUI();
    }

    public void OpenAllReplayUI()
    {
        OpenSpectatorUI();
        OpenReplayControls();
    }

    public void CloseAllReplayUI()
    {
        CloseSpectatorUI();
        CloseReplayControls();
    }

    public void OpenSpectatorUI()
    {
        if (spectatorInterface == null)
            return;

        spectatorState ??= new ReplayUIState();
        spectatorInterface.SetState(spectatorState);
    }

    public void CloseSpectatorUI()
    {
        spectatorInterface?.SetState(null);
    }

    public bool IsReplayControlsActive()
    {
        return replayControlsInterface?.CurrentState != null;
    }

    public void ToggleReplayControls()
    {
        if (IsReplayControlsActive())
        {
            CloseReplayControls();
            return;
        }

        OpenReplayControls();
    }

    public void OpenReplayControls()
    {
        if (!ReplaySession.IsReplayPlayback || replayControlsInterface == null)
            return;

        replayControlsState = new UIState();
        replayControlsPanel = new ReplayControlsPanel();
        replayControlsState.Append(replayControlsPanel);
        replayControlsInterface.SetState(replayControlsState);
    }

    public void CloseReplayControls()
    {
        replayControlsInterface?.SetState(null);
        replayControlsState = null;
        replayControlsPanel = null;
    }

    public bool IsMouseOverReplayControls()
    {
        return IsReplayControlsActive() && replayControlsPanel?.ContainsPoint(Main.MouseScreen) == true;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        spectatorInterface?.Update(gameTime);
        replayControlsInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int spectatorIndex = layers.FindIndex(l => l.Name == "Vanilla: Death Text");

        if (ConfigHelper.IsAnyConfigUIOpen())
            spectatorIndex = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");

        if (spectatorIndex != -1 && spectatorInterface?.CurrentState != null)
        {
            layers.Insert(spectatorIndex, new LegacyGameInterfaceLayer("Reese: Spectator UI", () =>
            {
                spectatorInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                return true;
            }, InterfaceScaleType.UI));
        }

        int controlsIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        if (controlsIndex == -1 || replayControlsInterface?.CurrentState == null)
            return;

        layers.Insert(controlsIndex, new LegacyGameInterfaceLayer("Reese: Replay Controls", () =>
        {
            replayControlsInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
            return true;
        }, InterfaceScaleType.UI));
    }
}