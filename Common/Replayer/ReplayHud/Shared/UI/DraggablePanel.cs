using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Shared.UI;

/// <summary>
/// Class used to define common properties for admin tool window panels,
/// </summary>
public abstract class DraggablePanel : UIElement
{
    // Dragging
    private bool dragging;
    private Vector2 dragOffset;

    // Content
    protected UIPanel TitlePanel;
    protected UIPanel ContentPanel;
    protected UIPanel LeftIconTitlePanel;
    protected UIPanel RightIconTitlePanel;

    // Overridable properties
    protected abstract void OnRightIconTitlePanelClick();
    protected abstract void OnLeftIconTitlePanelClick();
    protected abstract Asset<Texture2D> LeftIcon { get; }
    protected virtual float LeftIconScale => 1f;
    public DraggablePanel(string title)
    {
        // Size and position
        Width.Set(350, 0);
        Height.Set(460, 0);
        Top.Set(0, 0);
        Left.Set(0, 0);
        VAlign = 0.7f;
        HAlign = 0.9f;
        SetPadding(0);

        TitlePanel = new();
        TitlePanel.Height.Set(40, 0);
        TitlePanel.Width.Set(0, 1);
        TitlePanel.SetPadding(0);
        TitlePanel.BackgroundColor = new Color(63, 82, 151) * 1f;

        UIText titleText = new(title, large: true, textScale: 0.55f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        TitlePanel.Append(titleText);

        ContentPanel = new UIPanel
        {
            Top = new StyleDimension(40f, 0f),
            Left = new StyleDimension(0f, 0f),
            Width = new StyleDimension(0f, 1f),
            Height = new StyleDimension(420f, 0f),
            BackgroundColor = new Color(20, 20, 60) * 0.7f,
            BorderColor = Color.Black
        };
        ContentPanel.OverflowHidden = true;
        ContentPanel.SetPadding(0);
        Append(ContentPanel);

        RightIconTitlePanel = new UIPanel
        {
            Height = new StyleDimension(0, 1),
            Width = new StyleDimension(40, 0),
            HAlign = 1f,
            VAlign = 0.5f
        };
        RightIconTitlePanel.OnLeftClick += (_, _) => OnRightIconTitlePanelClick();
        RightIconTitlePanel.OnMouseOver += (_, _) => RightIconTitlePanel.BorderColor = Color.Yellow;
        RightIconTitlePanel.OnMouseOut += (_, _) => RightIconTitlePanel.BorderColor = Color.Black;

        var closeText = new UIText("X", large: true, textScale: 0.55f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };

        RightIconTitlePanel.Append(closeText);
        TitlePanel.Append(RightIconTitlePanel);
        Append(TitlePanel);

        // Refresh panel
        LeftIconTitlePanel = new()
        {
            Height = new StyleDimension(0, 1),
            Width = new StyleDimension(40, 0),
            VAlign = 0.5f
        };
        LeftIconTitlePanel.OnLeftClick += (_, _) => OnLeftIconTitlePanelClick();
        LeftIconTitlePanel.OnMouseOver += (_, _) =>
        {
            LeftIconTitlePanel.BorderColor = Color.Yellow;
        };
        LeftIconTitlePanel.OnMouseOut += (_, _) => LeftIconTitlePanel.BorderColor = Color.Black;
        LeftIconTitlePanel.SetPadding(0);

        LeftIconTitlePanel.Append(new UIImage(LeftIcon.Value)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        });

        TitlePanel.Append(LeftIconTitlePanel);
    }
    public override void Recalculate()
    {
        base.Recalculate();

        if (TitlePanel != null)
        {
            TitlePanel.Top.Set(0f, 0f);
            TitlePanel.Width.Set(0f, 1f);
            TitlePanel.Height.Set(40f, 0f);
        }

        if (ContentPanel != null)
        {
            ContentPanel.Top.Set(40f, 0f);
            ContentPanel.Left.Set(0f, 0f);
            ContentPanel.Width.Set(0f, 1f);
            ContentPanel.Height.Set(Height.Pixels - 40f, 0f);
        }
    }

    #region Dragging
    public override void Update(GameTime gameTime)
    {
        // TEMP DEBUG FIX BOTTOM CONTENT PANEL NOT INTERACTABLE.
        // NOTE: ALSO USE MOD RELOADER UIELEMENT DEBUGGER FOR THIS, ITS REALLY GOOD!
        //Log.Chat("height: " + ContentPanel.Height.Pixels);
        //Log.Chat("width: " + Width.Pixels);
        //ContentPanel.MaxHeight.Pixels = 10;
        //ContentPanel.Height.Pixels = 0;
        //ContentPanel.Top.Pixels = 40;

        base.Update(gameTime);

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;

        if (LeftIconTitlePanel.IsMouseHovering)
            Main.instance.MouseText("Reset");

        if (Parent == null)
            return;

        if (RightIconTitlePanel.IsMouseHovering || LeftIconTitlePanel.IsMouseHovering)
            //Log.Chat("hovering something");
            return;

        if (dragging)
        {
            var parent = Parent.GetDimensions();

            Left.Pixels = Main.mouseX - dragOffset.X - parent.X;
            Top.Pixels = Main.mouseY - dragOffset.Y - parent.Y;

            // Clamp to screen
            var dims = GetDimensions();
            Left.Pixels = Utils.Clamp(Left.Pixels, 0f, Math.Max(0f, parent.Width - dims.Width));
            Top.Pixels = Utils.Clamp(Top.Pixels, 0f, Math.Max(0f, parent.Height - dims.Height));
            Recalculate();
        }

        // Ensure panel stays on screen
        var parentSpace = Parent.GetDimensions().ToRectangle();
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

        if (RightIconTitlePanel.IsMouseHovering || LeftIconTitlePanel.IsMouseHovering)
            return;

        if (TitlePanel == null || !TitlePanel.ContainsPoint(evt.MousePosition) || Parent == null)
            return;

        if (HAlign != 0f || VAlign != 0f || Left.Percent != 0f || Top.Percent != 0f)
        {
            var p = Parent.GetDimensions();
            var d = GetDimensions();

            HAlign = 0f;
            VAlign = 0f;
            Left.Percent = 0f;
            Top.Percent = 0f;

            Left.Pixels = d.X - p.X;
            Top.Pixels = d.Y - p.Y;

            Recalculate();
        }

        dragging = true;
        dragOffset = evt.MousePosition - GetDimensions().Position();
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        dragging = false;
    }
    #endregion
}
