using Microsoft.Xna.Framework;
using Reese.Core.Utilities;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replay.ReplayHud.Shared.UI;

/// <summary>
/// Base panel for replay HUD surfaces that can be moved by dragging their background.
/// </summary>
public abstract class DraggablePanel : UIElement
{
    private bool dragging;
    private Vector2 dragOffset;

    protected UIPanel ContentPanel;

    public DraggablePanel(string title)
    {
        Width.Set(350, 0);
        Height.Set(460, 0);
        Top.Set(0, 0);
        Left.Set(0, 0);
        VAlign = 0.7f;
        HAlign = 0.9f;
        SetPadding(0);

        ContentPanel = new UIPanel
        {
            Top = new StyleDimension(0f, 0f),
            Left = new StyleDimension(0f, 0f),
            Width = new StyleDimension(0f, 1f),
            Height = new StyleDimension(0f, 1f),
            BackgroundColor = new Color(12, 18, 42) * 0.96f,
            BorderColor = Color.Black
        };
        ContentPanel.OverflowHidden = true;
        ContentPanel.SetPadding(0);
        Append(ContentPanel);
    }

    public override void Recalculate()
    {
        base.Recalculate();

        if (ContentPanel != null)
        {
            ContentPanel.Top.Set(0f, 0f);
            ContentPanel.Left.Set(0f, 0f);
            ContentPanel.Width.Set(0f, 1f);
            ContentPanel.Height.Set(0f, 1f);
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;

        if (Parent == null)
            return;

        if (dragging)
        {
            CalculatedStyle parent = Parent.GetDimensions();

            Left.Pixels = Main.mouseX - dragOffset.X - parent.X;
            Top.Pixels = Main.mouseY - dragOffset.Y - parent.Y;

            CalculatedStyle dimensions = GetDimensions();
            Left.Pixels = Utils.Clamp(Left.Pixels, 0f, Math.Max(0f, parent.Width - dimensions.Width));
            Top.Pixels = Utils.Clamp(Top.Pixels, 0f, Math.Max(0f, parent.Height - dimensions.Height));
            Recalculate();
        }

        Rectangle parentSpace = Parent.GetDimensions().ToRectangle();
        if (!GetDimensions().ToRectangle().Intersects(parentSpace))
        {
            Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
            Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);
            Recalculate();
        }
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (Parent == null || !CanStartDrag(evt.Target))
            return;

        if (HAlign != 0f || VAlign != 0f || Left.Percent != 0f || Top.Percent != 0f)
        {
            CalculatedStyle parent = Parent.GetDimensions();
            CalculatedStyle dimensions = GetDimensions();

            HAlign = 0f;
            VAlign = 0f;
            Left.Percent = 0f;
            Top.Percent = 0f;

            Left.Pixels = dimensions.X - parent.X;
            Top.Pixels = dimensions.Y - parent.Y;

            Recalculate();
        }

        dragging = true;
        dragOffset = evt.MousePosition - GetDimensions().Position();
    }

    protected virtual bool CanStartDrag(UIElement target)
    {
        return target == this || target == ContentPanel;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        dragging = false;
    }
}
