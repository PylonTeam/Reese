using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Common.MainMenu.UI;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.Stats;
using Reese.Common.Replayer.ReplayHud.Shared.Drawers;
using Reese.Core.Debug;
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
    private readonly ActionHoverLabel actionHoverLabel;
    private readonly bool isFavorite;
    private static readonly Color NormalTextColor = Color.White;
    private static readonly Color FaultyTextColor = new(145, 145, 145);

    public ReplayListItem(ReplayMetadata entry, Action onEntryChanged, Action<string> onFavoriteToggled)
    {
        isFavorite = ReplayFavorites.IsFavorite(entry.FullPath);
        Color replayTextColor = entry.DurationTicks == 0 ? FaultyTextColor : NormalTextColor;

        // Layout
        ReplayBrowserLayout.Update();
        Width.Set(0f, 1f);
        Height.Set(ReplayBrowserLayout.ReplayItemTotalHeight, 0f);
        SetPadding(0f);

        BackgroundColor = new Color(63, 82, 151) * 0.95f;
        BorderColor = new Color(89, 116, 213) * 0.95f;

        // Preview world
        Append(new ReplayPreviewImageElement(entry, ReplayBrowserLayout.ReplayItemHeight));

        // Replay filename
        Append(new ReplayNameElement(entry.ReplayName, replayTextColor)
        {
            Left = { Pixels = ReplayBrowserLayout.PreviewColumnWidth + ReplayBrowserLayout.StatColumnPadding + 4 },
            Top = { Pixels = 10f },
            Width = { Pixels = ReplayBrowserLayout.NameColumnWidth - ReplayBrowserLayout.PreviewColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 22f }
        });

        Color worldTextColor = entry.WorldName == "Unknown" ? FaultyTextColor : NormalTextColor;
        Color dateTextColor = entry.DateCreated == DateTime.MinValue ? FaultyTextColor : NormalTextColor;
        Color lengthTextColor = entry.DurationTicks == 0 ? FaultyTextColor : NormalTextColor;
        Color modsTextColor = entry.ModNames is null ? FaultyTextColor : NormalTextColor;
        Color sizeTextColor = entry.SizeBytes <= 0 ? FaultyTextColor : NormalTextColor;

        // World Name
        Append(new MainMenuStatElement(MainMenuReplayStats.BuildMainMenuWorldNameStat(entry.WorldName), 0.9f, iconScale: 1.25f, textColor: worldTextColor)
        {
            Left = { Pixels = ReplayBrowserLayout.PreviewColumnWidth + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 34f },
            Width = { Pixels = ReplayBrowserLayout.NameColumnWidth - ReplayBrowserLayout.PreviewColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 24f }
        });

        // Date
        Append(new MainMenuStatElement(MainMenuReplayStats.BuildMainMenuDateStat(entry.DateCreated), 0.9f, drawIcon: false, centerText: true, textColor: dateTextColor)
        {
            Left = { Pixels = ReplayBrowserLayout.DateLeft + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 34f },
            Width = { Pixels = ReplayBrowserLayout.DateColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 24f }
        });

        // Length
        Append(new MainMenuStatElement(MainMenuReplayStats.BuildMainMenuLengthStat(entry.DurationTicks), 0.9f, drawIcon: false, centerText: true, textColor: lengthTextColor)
        {
            Left = { Pixels = ReplayBrowserLayout.DurationLeft + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 34f },
            Width = { Pixels = ReplayBrowserLayout.DurationColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 24f }
        });

        // Mods
        Append(new MainMenuStatElement(MainMenuReplayStats.BuildMainMenuModsStat(entry.ModNames), 0.9f, drawIcon: false, centerText: true, textColor: modsTextColor)
        {
            Left = { Pixels = ReplayBrowserLayout.ModsLeft + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 34f },
            Width = { Pixels = ReplayBrowserLayout.ModsColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 24f }
        });

        // Size
        Append(new MainMenuStatElement(MainMenuReplayStats.BuildMainMenuSizeStat(entry.SizeBytes), 0.9f, drawIcon: false, centerText: true, textScaleMultiplier: 1.0f, textColor: sizeTextColor)
        {
            Left = { Pixels = ReplayBrowserLayout.SizeLeft + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 34f },
            Width = { Pixels = ReplayBrowserLayout.SizeColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 24f }
        });

        //Append(UISortableTableColumn.CreateSeparator(ReplayBrowserLayout.DateLeft, ReplayBrowserLayout.ReplayItemTotalHeight));
        //Append(UISortableTableColumn.CreateSeparator(ReplayBrowserLayout.DurationLeft, ReplayBrowserLayout.ReplayItemTotalHeight));

        Asset<Texture2D> favoriteTexture = Main.Assets.Request<Texture2D>(
            isFavorite ? "Images/UI/ButtonFavoriteActive" : "Images/UI/ButtonFavoriteInactive");

        // Actions
        ReplayActionDefinition[] actions =
        [
            new(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"), "Play", () => ReplayItemActions.Play(entry.FullPath)),
            new(favoriteTexture, isFavorite ? "Unfavorite" : "Favorite", () => ReplayItemActions.Favorite(entry.FullPath, () => onFavoriteToggled?.Invoke(entry.FullPath))),
            //new(Main.Assets.Request<Texture2D>("Images/UI/ButtonSeed"), "Upload image", () => ReplayItemActions.ChoosePreviewImage(entry.FullPath, onEntryChanged)),
            new(Main.Assets.Request<Texture2D>("Images/UI/ButtonRename"), "Rename", () => ReplayItemActions.Rename(entry.FullPath, onEntryChanged)),
        ];

        // Hover label
        actionHoverLabel = new ActionHoverLabel();
        actionHoverLabel.Left.Set(10f + actions.Length * ReplayBrowserLayout.ActionButtonSize + Math.Max(0, actions.Length - 1) * ReplayBrowserLayout.ActionButtonGap + ReplayBrowserLayout.ActionLabelGap, 0f);
        actionHoverLabel.Top.Set(ReplayBrowserLayout.ReplayItemHeight + (ReplayBrowserLayout.ReplayItemActionHeight - ReplayBrowserLayout.ActionButtonSize) * 0.5f + 1f, 0f);
        actionHoverLabel.Width.Set(ReplayBrowserLayout.ActionLabelWidth, 0f);
        actionHoverLabel.Height.Set(ReplayBrowserLayout.ActionButtonSize, 0f);
        Append(actionHoverLabel);

        UIImageButton[] previewActionButtons = AppendActionButtons(actions, actionHoverLabel);

        float buttonTop = ReplayBrowserLayout.ReplayItemHeight + (ReplayBrowserLayout.ReplayItemActionHeight - ReplayBrowserLayout.ActionButtonSize) * 0.5f - 1;
        float deleteLeft = ReplayBrowserLayout.SizeLeft + ReplayBrowserLayout.SizeColumnWidth - ReplayBrowserLayout.ActionButtonRightPadding - ReplayBrowserLayout.ActionButtonSize - 2;
        ActionHoverLabel deleteHoverLabel = new()
        {
            TextAlign = 1f
        };
        deleteHoverLabel.Left.Set(deleteLeft - ReplayBrowserLayout.ActionLabelGap - ReplayBrowserLayout.ActionLabelWidth + 4, 0f);
        deleteHoverLabel.Top.Set(buttonTop, 0f);
        deleteHoverLabel.Width.Set(ReplayBrowserLayout.ActionLabelWidth, 0f);
        deleteHoverLabel.Height.Set(ReplayBrowserLayout.ActionButtonSize, 0f);
        Append(deleteHoverLabel);

        UIImageButton deleteButton = CreateVanillaImageButton(
            new ReplayActionDefinition(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"), "Delete", () => ReplayItemActions.Delete(entry.FullPath, onEntryChanged)),
            deleteHoverLabel,
            deleteLeft,
            buttonTop,
            ReplayBrowserLayout.ActionButtonSize);
        Append(deleteButton);

        UIImageButton[] actionButtons = previewActionButtons.Concat([deleteButton]).ToArray();

        OnLeftDoubleClick += (evt, _) =>
        {
            if (actionButtons.Any(button => button.ContainsPoint(evt.MousePosition)))
                return;

            ReplayItemActions.Play(entry.FullPath);
        };
    }

    private sealed class ReplayPreviewImageElement : UIElement
    {
        private readonly Asset<Texture2D> fallbackIcon;

        public ReplayPreviewImageElement(ReplayMetadata metadata, float size)
        {
            fallbackIcon = GetFallbackWorldIcon(metadata);

            Width.Set(size, 0f);
            Height.Set(size, 0f);
            Left.Set(6f, 0f);
            Top.Set(6f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle area = GetDimensions().ToRectangle();
            //Texture2D texture = ReplayImages.GetTexture(info.PreviewImagePath) ?? fallbackIcon.Value;
            Texture2D texture = fallbackIcon.Value;
            DrawTextureFit(spriteBatch, texture, new Rectangle(area.X, area.Y, area.Width, area.Height));
        }

        private static void DrawTextureFit(SpriteBatch spriteBatch, Texture2D texture, Rectangle area)
        {
            if (texture == null)
                return;

            float scale = Math.Min(area.Width / (float)texture.Width, area.Height / (float)texture.Height);
            int width = Math.Max(1, (int)Math.Round(texture.Width * scale));
            int height = Math.Max(1, (int)Math.Round(texture.Height * scale));
            Rectangle destination = new(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height);

            spriteBatch.Draw(texture, destination, Color.White);
        }

        private static Asset<Texture2D> GetFallbackWorldIcon(ReplayMetadata info)
        {
            var world = Main.WorldList?.FirstOrDefault(x =>
                x != null &&
                string.Equals(x.Name, info.WorldName, StringComparison.OrdinalIgnoreCase));

            if (world == null)
                return Main.Assets.Request<Texture2D>("Images/UI/IconCorruption");

            if (world.DrunkWorld && world.RemixWorld)
                return Main.Assets.Request<Texture2D>("Images/UI/IconEverything");

            if (world.DrunkWorld)
                return Main.Assets.Request<Texture2D>("Images/UI/Icon" + (world.IsHardMode ? "Hallow" : "") + "CorruptionCrimson");

            if (world.ForTheWorthy)
                return Main.Assets.Request<Texture2D>("Images/UI/Icon" + (world.IsHardMode ? "Hallow" : "") + "FTW");

            if (world.Anniversary)
                return Main.Assets.Request<Texture2D>("Images/UI/Icon" + (world.IsHardMode ? "Hallow" : "") + "Anniversary");

            if (world.DontStarve)
                return Main.Assets.Request<Texture2D>("Images/UI/Icon" + (world.IsHardMode ? "Hallow" : "") + "DontStarve");

            if (world.RemixWorld)
                return Main.Assets.Request<Texture2D>("Images/UI/Icon" + (world.IsHardMode ? "Hallow" : "") + "Remix");

            return Main.Assets.Request<Texture2D>("Images/UI/Icon" + (world.IsHardMode ? "Hallow" : "") + (world.HasCorruption ? "Corruption" : "Crimson"));
        }
    }

    private sealed class ReplayNameElement : UIElement
    {
        private readonly string text;
        private readonly Color textColor;

        public ReplayNameElement(string text, Color textColor)
        {
            this.text = string.IsNullOrWhiteSpace(text) ? "-" : text;
            this.textColor = textColor;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            const float TextScale = 0.95f;

            Rectangle area = GetDimensions().ToRectangle();
            var font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text) * TextScale;
            Vector2 position = new(area.X, area.Y + (area.Height - size.Y) * 0.5f);

            Utils.DrawBorderString(spriteBatch, text, position, textColor, TextScale);
        }
    }

    private UIImageButton[] AppendActionButtons(ReplayActionDefinition[] actions, ActionHoverLabel hoverLabel)
    {
        UIImageButton[] buttons = new UIImageButton[actions.Length];

        for (int i = 0; i < actions.Length; i++)
        {
            buttons[i] = CreateVanillaImageButton(
                actions[i],
                hoverLabel,
                10f + i * (ReplayBrowserLayout.ActionButtonSize + ReplayBrowserLayout.ActionButtonGap),
                ReplayBrowserLayout.ReplayItemHeight + (ReplayBrowserLayout.ReplayItemActionHeight - ReplayBrowserLayout.ActionButtonSize) * 0.5f + 1f,
                ReplayBrowserLayout.ActionButtonSize);

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
        button.OnMouseOut += (_, _) => hoverLabel.ClearAction();
        button.OnUpdate += _ =>
        {
            if (button.IsMouseHovering)
            {
                hoverLabel.SetAction(action.Label);
            }
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
        public float TextAlign;

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
            Vector2 position = new(area.X + (area.Width - size.X) * TextAlign, area.Y + (area.Height - size.Y) * 0.5f + 3f);

            Utils.DrawBorderString(spriteBatch, label, position, new Color(215, 225, 255), TextScale);

            if (IsMouseHovering && !string.IsNullOrWhiteSpace(label))
                UICommon.TooltipMouseText(label);
        }
    }

    private sealed class MainMenuStatElement : UIElement
    {
        private readonly PlayerStatSnapshot stat;
        private readonly float scale;
        private readonly bool drawIcon;
        private readonly float iconScale;
        private readonly bool centerText;
        private readonly float textScaleMultiplier;
        private readonly Color? textColor;

        public MainMenuStatElement(PlayerStatSnapshot stat, float scale, bool drawIcon = true, float iconScale = 1f, bool centerText = false, float textScaleMultiplier = 1f, Color? textColor = null)
        {
            this.stat = stat;
            this.scale = scale;
            this.drawIcon = drawIcon;
            this.iconScale = iconScale;
            this.centerText = centerText;
            this.textScaleMultiplier = textScaleMultiplier;
            this.textColor = textColor;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            StatDrawer.DrawReplayStatInMainMenu(spriteBatch, GetDimensions().ToRectangle(), stat, scale, drawIcon, iconScale, centerText, textScaleMultiplier, textColor);
        }
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
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        BackgroundColor = new Color(63, 82, 151) * 0.95f;
        BorderColor = new Color(89, 116, 213) * 0.95f;
        actionHoverLabel.ClearAction();
    }
}
