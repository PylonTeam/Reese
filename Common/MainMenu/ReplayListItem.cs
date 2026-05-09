using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Common.ReplaySpectate.Drawers;
using Reese.Core.Debug;
using Reese.UI;
using ReLogic.Content;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace Reese.Common.MainMenu;

internal sealed class ReplayListItem : UIPanel
{
    private readonly UICharacter preview;
    private readonly ActionHoverLabel actionHoverLabel;

    public ReplayListItem(string fullPath, Action onDeleted)
    {
        ReplayBrowserLayout.Update();

        Width.Set(0f, 1f);
        Height.Set(ReplayBrowserLayout.ReplayItemTotalHeight, 0f);
        SetPadding(0f);

        BackgroundColor = new Color(63, 82, 151) * 0.95f;
        BorderColor = new Color(89, 116, 213) * 0.95f;

        ReplayDisplayInfo info = ReplayDisplayInfo.FromFile(fullPath);

        preview = BuildPreviewCharacter(info);
        preview.SetAnimated(false);
        Append(new ReplayPreviewElement(preview, ReplayBrowserLayout.ReplayItemHeight));

        string dateLine = info.Date.ToString("yyyy-MM-dd");
        string timeLine = info.Date.ToString("HH:mm");

        Append(new ReplayNameElement(Path.GetFileNameWithoutExtension(info.FileName), info.MetadataTooltip)
        {
            Left = { Pixels = 66f },
            Top = { Pixels = 4f },
            Width = { Pixels = ReplayBrowserLayout.NameColumnWidth - 76f },
            Height = { Pixels = 22f }
        });

        Append(new MainMenuStatElement(info.PlayerNameRaw, world: false)
        {
            Left = { Pixels = 72f },
            Top = { Pixels = 22f },
            Width = { Pixels = ReplayBrowserLayout.NameColumnWidth - 106f },
            Height = { Pixels = 20f }
        });
        Append(new MainMenuStatElement(info.WorldNameRaw, world: true)
        {
            Left = { Pixels = 72f },
            Top = { Pixels = 44f },
            Width = { Pixels = ReplayBrowserLayout.NameColumnWidth - 106f },
            Height = { Pixels = 20f }
        });

        Append(UISortableTableColumn.CreateCenteredTwoLineCell(dateLine, timeLine, ReplayBrowserLayout.DateLeft, ReplayBrowserLayout.DateColumnWidth));
        Append(UISortableTableColumn.CreateCenteredTextCell(info.DurationText, ReplayBrowserLayout.DurationLeft, ReplayBrowserLayout.DurationColumnWidth, $"Duration: {info.DurationText}"));
        Append(UISortableTableColumn.CreateSeparator(ReplayBrowserLayout.DateLeft, ReplayBrowserLayout.ReplayItemTotalHeight));
        Append(UISortableTableColumn.CreateSeparator(ReplayBrowserLayout.DurationLeft, ReplayBrowserLayout.ReplayItemTotalHeight));

        actionHoverLabel = new ActionHoverLabel();
        actionHoverLabel.Left.Set(ReplayBrowserLayout.ReplayItemHeight - ReplayBrowserLayout.ActionButtonRightPadding + ReplayBrowserLayout.ActionLabelGap, 0f);
        actionHoverLabel.Top.Set(ReplayBrowserLayout.ReplayItemHeight + (ReplayBrowserLayout.ReplayItemActionHeight - ReplayBrowserLayout.ActionButtonSize) * 0.5f, 0f);
        actionHoverLabel.Width.Set(ReplayBrowserLayout.ActionLabelWidth, 0f);
        actionHoverLabel.Height.Set(ReplayBrowserLayout.ActionButtonSize, 0f);
        Append(actionHoverLabel);

        ReplayActionDefinition[] actions =
        [
            new(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"), "Play", () => ReplayMenuActions.Play(fullPath)),
            new(Main.Assets.Request<Texture2D>("Images/UI/ButtonRename"), "Rename", () => ReplayMenuActions.Rename(fullPath, onDeleted))
        ];
        UIImageButton[] previewActionButtons = AppendActionButtons(actions, actionHoverLabel);

        float buttonTop = ReplayBrowserLayout.ReplayItemHeight + (ReplayBrowserLayout.ReplayItemActionHeight - ReplayBrowserLayout.ActionButtonSize) * 0.5f;
        float deleteLeft = ReplayBrowserLayout.DurationLeft + ReplayBrowserLayout.DurationColumnWidth - ReplayBrowserLayout.ActionButtonRightPadding - ReplayBrowserLayout.ActionButtonSize;
        UIImageButton deleteButton = CreateVanillaImageButton(
            new ReplayActionDefinition(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"), "Delete", () => ReplayMenuActions.Delete(fullPath, onDeleted)),
            actionHoverLabel,
            deleteLeft,
            buttonTop,
            ReplayBrowserLayout.ActionButtonSize);
        Append(deleteButton);

        UIImageButton[] actionButtons = previewActionButtons.Concat([deleteButton]).ToArray();

        OnLeftDoubleClick += (evt, _) =>
        {
            if (actionButtons.Any(button => button.ContainsPoint(evt.MousePosition)))
                return;

            ReplayMenuActions.Play(fullPath);
        };
    }

    private sealed class ReplayNameElement : UIElement
    {
        private const float TextScale = 0.86f;

        private readonly string text;
        private readonly string tooltip;

        public ReplayNameElement(string text, string tooltip)
        {
            this.text = string.IsNullOrWhiteSpace(text) ? "-" : text;
            this.tooltip = tooltip;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle area = GetDimensions().ToRectangle();
            var font = FontAssets.MouseText.Value;
            string drawText = FitText(font, text, area.Width);
            Vector2 size = font.MeasureString(drawText) * TextScale;
            Vector2 position = new(area.X, area.Y + (area.Height - size.Y) * 0.5f);

            Utils.DrawBorderString(spriteBatch, drawText, position, Color.White, TextScale);

            if (IsMouseHovering && !string.IsNullOrWhiteSpace(tooltip))
                UICommon.TooltipMouseText(tooltip);
        }

        private static string FitText(ReLogic.Graphics.DynamicSpriteFont font, string value, float maxWidth)
        {
            if (font.MeasureString(value).X * TextScale <= maxWidth)
                return value;

            const string suffix = "..";
            for (int length = value.Length - 1; length > 0; length--)
            {
                string candidate = value[..length] + suffix;
                if (font.MeasureString(candidate).X * TextScale <= maxWidth)
                    return candidate;
            }

            return font.MeasureString(suffix).X * TextScale <= maxWidth ? suffix : string.Empty;
        }
    }

    private UIImageButton[] AppendActionButtons(ReplayActionDefinition[] actions, ActionHoverLabel hoverLabel)
    {
        UIImageButton[] buttons = new UIImageButton[actions.Length];
        float buttonSize = ReplayBrowserLayout.ActionButtonSize;
        float buttonGap = ReplayBrowserLayout.ActionButtonGap;
        float top = ReplayBrowserLayout.ReplayItemHeight + (ReplayBrowserLayout.ReplayItemActionHeight - buttonSize) * 0.5f;
        float left = ReplayBrowserLayout.ReplayItemHeight - ReplayBrowserLayout.ActionButtonRightPadding - buttonSize * actions.Length - buttonGap * Math.Max(0, actions.Length - 1);

        for (int i = 0; i < actions.Length; i++)
        {
            buttons[i] = CreateVanillaImageButton(actions[i], hoverLabel, left + i * (buttonSize + buttonGap), top, buttonSize);
            Append(buttons[i]);
        }

        return buttons;
    }

    private static UIImageButton CreateVanillaImageButton(ReplayActionDefinition action, ActionHoverLabel hoverLabel, float left, float top, float size)
    {
        UIImageButton button = new(action.Texture);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        button.Width.Set(size, 0f);
        button.Height.Set(size, 0f);
        button.SetVisibility(1f, 0.55f);

        bool playedTick = false;
        button.OnMouseOver += (_, _) =>
        {
            hoverLabel.SetAction(action.Label);

            if (!playedTick)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                playedTick = true;
            }
        };
        button.OnMouseOut += (_, _) => playedTick = false;
        button.OnUpdate += _ =>
        {
            if (button.IsMouseHovering)
                UICommon.TooltipMouseText(action.Label);
        };
        button.OnLeftClick += (_, _) => action.Click?.Invoke();
        return button;
    }

    private readonly struct ReplayActionDefinition(Asset<Texture2D> texture, string label, Action click)
    {
        public Asset<Texture2D> Texture { get; } = texture;
        public string Label { get; } = label;
        public Action Click { get; } = click;
    }

    private sealed class ActionHoverLabel : UIElement
    {
        private const float TextScale = 0.88f;

        private string label = "";

        public void SetAction(string label)
        {
            this.label = label;
        }

        public void ClearAction()
        {
            label = "";
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle area = GetDimensions().ToRectangle();
            var font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(label) * TextScale;
            Vector2 position = new(area.X, area.Y + (area.Height - size.Y) * 0.5f + 3f);

            Utils.DrawBorderString(spriteBatch, label, position, new Color(215, 225, 255), TextScale);

            if (IsMouseHovering && !string.IsNullOrWhiteSpace(label))
                UICommon.TooltipMouseText(label);
        }
    }

    private sealed class MainMenuStatElement : UIElement
    {
        private readonly string text;
        private readonly bool world;

        public MainMenuStatElement(string text, bool world)
        {
            this.text = text;
            this.world = world;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle area = GetDimensions().ToRectangle();

            if (world)
                StatDrawer.DrawWorldNameStatInMainMenu(spriteBatch, area, text, 0.78f);
            else
                StatDrawer.DrawPlayerNameStatInMainMenu(spriteBatch, area, text, 0.78f);
        }
    }

    private sealed class ReplayPreviewElement : UIElement
    {
        public ReplayPreviewElement(UICharacter preview, float size)
        {
            Width.Set(size, 0f);
            Height.Set(size, 0f);

            preview.Width.Set(size, 0f);
            preview.Height.Set(size, 0f);
            Append(preview);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            EntityDrawer.DrawEntityBackground(spriteBatch, GetDimensions().ToRectangle());
        }
    }

    private static UICharacter BuildPreviewCharacter(ReplayDisplayInfo info)
    {
        try
        {
            return new UICharacter(BuildPreviewPlayer(info), animated: false, hasBackPanel: false, characterScale: 0.9f, useAClone: false);
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to build replay preview character for '{info?.PlayerNameRaw}': {e.Message}");
            return new UICharacter(BuildFallbackPreviewPlayer(), animated: false, hasBackPanel: false, characterScale: 0.9f, useAClone: false);
        }
    }

    private static Player BuildFallbackPreviewPlayer()
    {
        Player player = new()
        {
            active = true,
            dead = false,
            name = "Player",
            Male = true,
            hair = 0,
            skinVariant = 0,
            hairColor = Color.Brown,
            skinColor = Color.White,
            eyeColor = Color.White,
            shirtColor = Color.White,
            underShirtColor = Color.White,
            pantsColor = Color.White,
            shoeColor = Color.White
        };

        return player;
    }

    private static Player BuildPreviewPlayer(ReplayDisplayInfo info)
    {
        if (info?.PlayerSnapshot != null)
            return info.PlayerSnapshot.ToPlayer();

        string playerName = info?.PlayerNameRaw;
        Main.LoadPlayers();

        var players = Main.PlayerList
            .Where(x => x?.Player != null)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(playerName))
        {
            var match = players.FirstOrDefault(x => string.Equals(x.Player.name, playerName, StringComparison.OrdinalIgnoreCase));
            if (match?.Player != null)
                return (Player)match.Player.clientClone();
        }

        if (players.Length > 0)
        {
            var selected = players.FirstOrDefault(x => x.Player == Main.LocalPlayer) ?? players[0];
            return (Player)selected.Player.clientClone();
        }

        return new Player();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        BackgroundColor = new Color(73, 94, 171);
        BorderColor = new Color(89, 116, 213);
        preview.SetAnimated(true);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        BackgroundColor = new Color(63, 82, 151) * 0.95f;
        BorderColor = new Color(89, 116, 213) * 0.95f;
        actionHoverLabel.ClearAction();
        preview.SetAnimated(false);
    }
}
