using Humanizer;
using Reese.Common.MainMenu.UI;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.Stats;
using ReLogic.Content;
using System;
using System.Linq;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace Reese.Common.MainMenu;

internal sealed class ReplayListItem : UIPanel
{
    private static readonly Color NormalTextColor = Color.White;
    private static readonly Color FaultyTextColor = new(145, 145, 145);

    private readonly ActionLabel actionLabel;

    public ReplayListItem(ReplayMetadata metadata, Action onEntryChanged, Action<string> onFavoriteToggled)
    {
        ReplayLayout.Update();

        Width.Set(0f, 1f);
        Height.Set(ReplayLayout.ReplayItemTotalHeight, 0f);
        SetPadding(0f);

        BackgroundColor = new Color(63, 82, 151) * 0.95f;
        BorderColor = new Color(89, 116, 213) * 0.95f;

        bool isNew = ReplayFlags.IsNew(metadata.FullPath);
        bool isPlayedBefore = ReplayFlags.HasPlayed(metadata.FullPath);
        bool isFavorite = ReplayFlags.IsFavorite(metadata.FullPath);

        Append(new Preview(metadata, ReplayLayout.ReplayItemHeight));

        Append(new NameText(metadata.ReplayName, TextColor(metadata.DurationTicks == 0))
        {
            Left = { Pixels = ReplayLayout.PreviewColumnWidth + ReplayLayout.StatColumnPadding + 4f },
            Top = { Pixels = 10f },
            Width = { Pixels = ReplayLayout.NameColumnWidth - ReplayLayout.PreviewColumnWidth - ReplayLayout.StatColumnPadding * 2f },
            Height = { Pixels = 22f }
        });

        AddStat(MainMenuReplayStats.BuildMainMenuWorldNameStat(metadata.WorldName), ReplayLayout.PreviewColumnWidth + ReplayLayout.StatColumnPadding,
            ReplayLayout.NameColumnWidth - ReplayLayout.PreviewColumnWidth - ReplayLayout.StatColumnPadding * 2f, TextColor(metadata.WorldName == "Unknown"), true, false, 1.25f);

        AddStat(MainMenuReplayStats.BuildMainMenuDateStat(metadata.DateCreated), ReplayLayout.DateLeft + ReplayLayout.StatColumnPadding,
            ReplayLayout.DateColumnWidth - ReplayLayout.StatColumnPadding * 2f, TextColor(metadata.DateCreated == DateTime.MinValue));

        AddStat(MainMenuReplayStats.BuildMainMenuLengthStat(metadata.DurationTicks), ReplayLayout.DurationLeft + ReplayLayout.StatColumnPadding,
            ReplayLayout.DurationColumnWidth - ReplayLayout.StatColumnPadding * 2f, TextColor(metadata.DurationTicks == 0));

        AddStat(MainMenuReplayStats.BuildMainMenuModsStat(metadata.ModNames), ReplayLayout.ModsLeft + ReplayLayout.StatColumnPadding,
            ReplayLayout.ModsColumnWidth - ReplayLayout.StatColumnPadding * 2f, TextColor(metadata.ModNames is null));

        AddStat(MainMenuReplayStats.BuildMainMenuSizeStat(metadata.SizeBytes), ReplayLayout.SizeLeft + ReplayLayout.StatColumnPadding,
            ReplayLayout.SizeColumnWidth - ReplayLayout.StatColumnPadding * 2f, TextColor(metadata.SizeBytes <= 0));

        Asset<Texture2D> favoriteTexture = Main.Assets.Request<Texture2D>(isFavorite ? "Images/UI/ButtonFavoriteActive" : "Images/UI/ButtonFavoriteInactive");

        ButtonAction[] actions =
        [
            new(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"), "Play", () => ReplayActions.EnterReplay(metadata.FullPath)),
            new(favoriteTexture, isFavorite ? "Unfavorite" : "Favorite", () => ReplayActions.Favorite(metadata.FullPath, () => onFavoriteToggled?.Invoke(metadata.FullPath))),
            new(Main.Assets.Request<Texture2D>("Images/UI/ButtonRename"), "Rename", () => ReplayActions.Rename(metadata.FullPath, onEntryChanged))
        ];

        actionLabel = AddLabel(10f + actions.Length * ReplayLayout.ActionButtonSize + Math.Max(0, actions.Length - 1) * ReplayLayout.ActionButtonGap + ReplayLayout.ActionLabelGap);
        UIImageButton[] buttons = AddButtons(actions, actionLabel);

        float buttonTop = ReplayLayout.ReplayItemHeight + (ReplayLayout.ReplayItemActionHeight - ReplayLayout.ActionButtonSize) * 0.5f - 1f;
        float deleteLeft = ReplayLayout.SizeLeft + ReplayLayout.SizeColumnWidth - ReplayLayout.ActionButtonRightPadding - ReplayLayout.ActionButtonSize - 2f;

        ActionLabel rightLabel = AddLabel(deleteLeft - ReplayLayout.ActionLabelGap - ReplayLayout.ActionLabelWidth + 4f, textAlign: 1f);

        if (isNew)
        {
            Append(new FlagIcon(rightLabel, Ass.IconNewlyGenerated.Value, "New")
            {
                Left = { Pixels = deleteLeft },
                Top = { Pixels = 5f },
                Width = { Pixels = ReplayLayout.ActionButtonSize },
                Height = { Pixels = ReplayLayout.ActionButtonSize }
            });
        }
        else if (isPlayedBefore)
        {
            Append(new FlagIcon(rightLabel, Ass.IconPlayedBefore.Value, "Played")
            {
                Left = { Pixels = deleteLeft },
                Top = { Pixels = 5f },
                Width = { Pixels = ReplayLayout.ActionButtonSize },
                Height = { Pixels = ReplayLayout.ActionButtonSize }
            });
        }

        ButtonAction deleteAction = isFavorite
            ? new ButtonAction(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"), "Cannot Delete (Favorited)", null)
            : new ButtonAction(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"), "Delete", () => ReplayActions.Delete(metadata.FullPath, onEntryChanged));

        UIImageButton deleteButton = Button(deleteAction, rightLabel, deleteLeft, buttonTop, ReplayLayout.ActionButtonSize);
        Append(deleteButton);

        UIImageButton[] allButtons = buttons.Concat([deleteButton]).ToArray();

        OnLeftDoubleClick += (evt, _) =>
        {
            if (allButtons.Any(button => button.ContainsPoint(evt.MousePosition)))
                return;

            ReplayActions.EnterReplay(metadata.FullPath);
        };
    }

    private void AddStat(PlayerStatSnapshot stat, float left, float width, Color color, bool icon = false, bool center = true, float iconScale = 1f)
    {
        Append(new Stat(stat, 0.9f, icon, iconScale, center, color)
        {
            Left = { Pixels = left },
            Top = { Pixels = 34f },
            Width = { Pixels = width },
            Height = { Pixels = 24f }
        });
    }

    private ActionLabel AddLabel(float left, float textAlign = 0f)
    {
        ActionLabel label = new() { TextAlign = textAlign };
        label.Left.Set(left, 0f);
        label.Top.Set(ReplayLayout.ReplayItemHeight + (ReplayLayout.ReplayItemActionHeight - ReplayLayout.ActionButtonSize) * 0.5f + 1f, 0f);
        label.Width.Set(ReplayLayout.ActionLabelWidth, 0f);
        label.Height.Set(ReplayLayout.ActionButtonSize, 0f);
        Append(label);
        return label;
    }

    private UIImageButton[] AddButtons(ButtonAction[] actions, ActionLabel label)
    {
        UIImageButton[] buttons = new UIImageButton[actions.Length];

        for (int i = 0; i < actions.Length; i++)
        {
            buttons[i] = Button(actions[i], label, 10f + i * (ReplayLayout.ActionButtonSize + ReplayLayout.ActionButtonGap),
                ReplayLayout.ReplayItemHeight + (ReplayLayout.ReplayItemActionHeight - ReplayLayout.ActionButtonSize) * 0.5f + 1f, ReplayLayout.ActionButtonSize);
            Append(buttons[i]);
        }

        return buttons;
    }

    private static UIImageButton Button(ButtonAction action, ActionLabel label, float left, float top, float size)
    {
        UIImageButton button = new(action.Texture);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        button.Width.Set(size, 0f);
        button.Height.Set(size, 0f);
        button.SetVisibility(1f, 0.55f);

        bool ticked = false;

        button.OnMouseOver += (_, _) =>
        {
            label.Set(action.Text);

            if (ticked)
                return;

            SoundEngine.PlaySound(SoundID.MenuTick);
            ticked = true;
        };

        button.OnMouseOut += (_, _) =>
        {
            ticked = false;
            label.Clear();
        };

        button.OnUpdate += _ =>
        {
            if (button.IsMouseHovering)
                label.Set(action.Text);
        };

        button.OnLeftClick += (_, _) => action.Click?.Invoke();
        return button;
    }

    private static Color TextColor(bool faulty)
    {
        return faulty ? FaultyTextColor : NormalTextColor;
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
        actionLabel.Clear();
    }

    private readonly struct ButtonAction(Asset<Texture2D> texture, string text, Action click)
    {
        public Asset<Texture2D> Texture { get; } = texture;
        public string Text { get; } = text;
        public Action Click { get; } = click;
    }

    private sealed class Preview : UIElement
    {
        private readonly Asset<Texture2D> texture;

        public Preview(ReplayMetadata metadata, float size)
        {
            texture = GetWorldIcon(metadata);
            Width.Set(size, 0f);
            Height.Set(size, 0f);
            Left.Set(6f, 0f);
            Top.Set(6f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            DrawFit(spriteBatch, texture.Value, GetDimensions().ToRectangle());
        }

        private static void DrawFit(SpriteBatch spriteBatch, Texture2D texture, Rectangle area)
        {
            if (texture == null)
                return;

            float scale = Math.Min(area.Width / (float)texture.Width, area.Height / (float)texture.Height);
            int width = Math.Max(1, (int)Math.Round(texture.Width * scale));
            int height = Math.Max(1, (int)Math.Round(texture.Height * scale));
            spriteBatch.Draw(texture, new Rectangle(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height), Color.White);
        }

        private static Asset<Texture2D> GetWorldIcon(ReplayMetadata metadata)
        {
            var world = Main.WorldList?.FirstOrDefault(x => x != null && string.Equals(x.Name, metadata.WorldName, StringComparison.OrdinalIgnoreCase));

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

    private sealed class NameText(string text, Color color) : UIElement
    {
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            const float Scale = 0.95f;

            Rectangle area = GetDimensions().ToRectangle();
            string value = string.IsNullOrWhiteSpace(text) ? "-" : text;
            Vector2 size = FontAssets.MouseText.Value.MeasureString(value) * Scale;
            Utils.DrawBorderString(spriteBatch, value, new Vector2(area.X, area.Y + (area.Height - size.Y) * 0.5f), color, Scale);
        }
    }

    private sealed class ActionLabel : UIElement
    {
        private const float Scale = 0.88f;

        private string text = "";
        public float TextAlign;

        public void Set(string value)
        {
            text = value;
        }

        public void Clear()
        {
            text = "";
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle area = GetDimensions().ToRectangle();
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * Scale;
            Vector2 pos = new(area.X + (area.Width - size.X) * TextAlign, area.Y + (area.Height - size.Y) * 0.5f + 3f);
            Utils.DrawBorderString(spriteBatch, text, pos, new Color(215, 225, 255), Scale);
        }
    }

    private sealed class Stat(PlayerStatSnapshot stat, float scale, bool icon, float iconScale, bool center, Color color) : UIElement
    {
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            StatDrawer.DrawReplayStatInMainMenu(spriteBatch, GetDimensions().ToRectangle(), stat, scale, icon, iconScale, center, 1f, color);
        }
    }

    private sealed class FlagIcon(ActionLabel label, Texture2D texture, string tooltip) : UIElement
    {
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!IsMouseHovering)
                return;

            Main.LocalPlayer.mouseInterface = true;
            label.Set(tooltip);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            label.Clear();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            Rectangle area = GetDimensions().ToRectangle();

#if DEBUG
            //sb.Draw(TextureAssets.MagicPixel.Value, area, Color.Red * 0.35f);
#endif

            float scale = Math.Min(area.Width / (float)texture.Width, area.Height / (float)texture.Height);
            int width = Math.Max(1, (int)Math.Round(texture.Width * scale));
            int height = Math.Max(1, (int)Math.Round(texture.Height * scale));

            Rectangle target = new(
                area.X + (area.Width - width) / 2,
                area.Y + (area.Height - height) / 2,
                width,
                height
            );

            sb.Draw(texture, target, Color.White);
        }
    }
}