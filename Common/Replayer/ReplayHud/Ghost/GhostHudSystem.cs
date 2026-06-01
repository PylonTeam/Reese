using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Ghost;

[Autoload(Side = ModSide.Client)]
public sealed class GhostHudSystem : ModSystem
{
    private UserInterface ghostHudInterface;
    private GhostHudState ghostHudState;

    public override void Load()
    {
        ghostHudInterface = new UserInterface();
        ghostHudState = new GhostHudState();
    }

    public override void Unload()
    {
        ghostHudInterface = null;
        ghostHudState = null;
    }

    public void Rebuild()
    {
        ghostHudState?.Rebuild();
    }

    public void OpenFullHud()
    {
        if (!SpectatorMode.CanUseGhostHud)
            return;

        ghostHudState?.ShowAllHuds();
        ghostHudInterface.SetState(ghostHudState);
    }

    public void CloseFullHud()
    {
        ghostHudInterface?.SetState(null);
    }

    public bool IsGhostHudOpen()
    {
        return ghostHudInterface?.CurrentState != null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
#if DEBUG
        if (KeyboardHelper.Pressed(Keys.F5))
        {
            Log.Chat("F5 pressed, rebuilding entire Ghost HUD.");
            Rebuild();
        }
#endif

        SyncGhostHudState();

        ghostHudInterface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (ghostHudInterface?.CurrentState == null || !SpectatorMode.CanUseGhostHud)
            return;

        bool configUiOpen = ConfigHelper.IsAnyConfigUIOpen();
        int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        int logicIndex = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");
        int deathTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Death Text");
        int ghostHudIndex = GetGhostHudInsertIndex(mouseTextIndex, logicIndex, deathTextIndex, configUiOpen);

        if (ghostHudIndex == -1)
            return;

        layers.Insert(ghostHudIndex, new LegacyGameInterfaceLayer("Reese: Ghost HUD", () =>
        {
            ghostHudInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
            return true;
        }, InterfaceScaleType.UI));
    }

    private void SyncGhostHudState()
    {
        if (SpectatorMode.CanUseGhostHud)
        {
            if (!IsGhostHudOpen())
                OpenFullHud();

            return;
        }

        if (IsGhostHudOpen())
            CloseFullHud();
    }

    private static int GetGhostHudInsertIndex(int mouseTextIndex, int logicIndex, int deathTextIndex, bool configUiOpen)
    {
        if (mouseTextIndex != -1)
            return mouseTextIndex;

        if (configUiOpen && logicIndex != -1)
            return logicIndex;

        return deathTextIndex;
    }

    public void CloseSpectateHud()
    {
        ghostHudState?.CloseSpectateHud();
    }

    public void CloseGhostHud()
    {
        ghostHudState?.CloseGhostHud();
    }
}
