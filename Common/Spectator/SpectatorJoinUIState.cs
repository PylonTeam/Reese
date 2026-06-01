using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Spectator;

public class SpectatorJoinUIState : UIState
{
    private UI.UIDraggableElement Root;
    private UIPanel Container;

    public override void OnActivate()
    {
        RemoveAllChildren();

        Root = new()
        {
            Width = new StyleDimension(290f, 0f),
            Height = new StyleDimension(162f, 0f),
            HAlign = 0.5f
        };
        Append(Root);

        // Title
        var title = new UITextPanel<string>("Choose Player or Spectator", 0.6f, large: true)
        {
            HAlign = 0.5f,
            BackgroundColor = new Color(73, 94, 171)
        };
        //title.SetPadding(0f);
        title.Width.Set(0f, 1f);

        title.OnLeftMouseDown += (evt, _) => Root.BeginDrag(evt);
        title.OnLeftMouseUp += (evt, _) => Root.EndDrag(evt);

        Root.Append(title);

        // Force a layout pass so we can measure the title height
        Root.Recalculate();
        float panelHeight = title.GetOuterDimensions().Height;

        Container = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        Container.Top.Set(panelHeight, 0f);
        Container.Width.Set(0f, 1f);
        Container.Height.Set(-panelHeight, 1f);
        Root.Append(Container);

        var list = new UIList
        {
            PaddingTop = 0f,
            ListPadding = 8f
        };
        list.Width.Set(0f, 1f);
        list.Height.Set(0f, 1f);
        list.Left.Set(0f, 0f);
        list.Top.Set(0f, 0f);
        Container.Append(list);

        // Enter player / spectator mode
        var playerRow = new UI.UITextActionPanel("Player", EnterPlayerMode, panelHeight, 0.5f, true, Ass.IconPlayerHead.Value);
        var spectatorRow = new UI.UITextActionPanel("Spectator", EnterSpectateMode, panelHeight, 0.5f, true, Ass.IconEye.Value);

        list.Add(playerRow);
        list.Add(spectatorRow);

        // Recalc after modifications
        Root.Recalculate();
    }

    private void EnterPlayerMode()
    {
        SpectatorModeSystem.RequestSetLocalModeFromJoinPanel(SpectateMode.Player);
        ModContent.GetInstance<SpectatorJoinUISystem>()?.Close();
    }

    private void EnterSpectateMode()
    {
        SpectatorModeSystem.RequestSetLocalModeFromJoinPanel(SpectateMode.Spectator);
        ModContent.GetInstance<SpectatorJoinUISystem>()?.Close();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }
}
