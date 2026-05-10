using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Common.Replayer.ReplaySpectate.Drawers;
using Reese.Core.Debug;
using Reese.Core.Stats;
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
using static Reese.Common.MainMenu.ReplayBrowserPanel;

namespace Reese.Common.MainMenu;

internal sealed class ReplayListItem : UIPanel
{
    private readonly UICharacter preview;
    private readonly ActionHoverLabel actionHoverLabel;
    private UIImage _worldIcon;

    public ReplayListItem(ReplayListEntry entry, Action onDeleted)
    {
        var constructorWatch = System.Diagnostics.Stopwatch.StartNew();
        ReplayDisplayInfo info = entry.Info;

        // Layout
        ReplayBrowserLayout.Update();
        Width.Set(0f, 1f);
        Height.Set(ReplayBrowserLayout.ReplayItemTotalHeight, 0f);
        SetPadding(0f);

        BackgroundColor = new Color(63, 82, 151) * 0.95f;
        BorderColor = new Color(89, 116, 213) * 0.95f;

        // Preview Character
        //preview = BuildPreviewCharacter(info);
        //preview.SetAnimated(false);
        //Append(new ReplayPreviewElement(preview, ReplayBrowserLayout.ReplayItemHeight));

        // Preview world
        Append(new ReplayWorldIconElement(info, ReplayBrowserLayout.ReplayItemHeight));

        // Replay filename
        Append(new ReplayNameElement(Path.GetFileNameWithoutExtension(entry.FullPath), entry.Name)
        {
            Left = { Pixels = ReplayBrowserLayout.PreviewColumnWidth + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 10f },
            Width = { Pixels = ReplayBrowserLayout.NameColumnWidth - ReplayBrowserLayout.PreviewColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 22f }
        });

        // World Name
        Append(new MainMenuStatElement(PlayerStats.BuildMainMenuWorldNameStat(info.WorldName), 0.9f, iconScale: 1.25f)
        {
            Left = { Pixels = ReplayBrowserLayout.PreviewColumnWidth + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 34f },
            Width = { Pixels = ReplayBrowserLayout.NameColumnWidth - ReplayBrowserLayout.PreviewColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 24f }
        });

        // Date
        Append(new MainMenuStatElement(PlayerStats.BuildMainMenuDateStat(entry.Date), 0.9f, drawIcon: false, centerText: true)
        {
            Left = { Pixels = ReplayBrowserLayout.DateLeft + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 34f },
            Width = { Pixels = ReplayBrowserLayout.DateColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 24f }
        });

        // Length
        Append(new MainMenuStatElement(PlayerStats.BuildMainMenuLengthStat(entry.Duration), 0.9f, drawIcon: false, centerText: true)
        {
            Left = { Pixels = ReplayBrowserLayout.DurationLeft + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 34f },
            Width = { Pixels = ReplayBrowserLayout.DurationColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 24f }
        });

        // Size
        Append(new MainMenuStatElement(PlayerStats.BuildMainMenuSizeStat(info.FileSizeText), 0.9f, drawIcon: false, centerText: true, textScaleMultiplier: 0.8f)
        {
            Left = { Pixels = ReplayBrowserLayout.SizeLeft + ReplayBrowserLayout.StatColumnPadding },
            Top = { Pixels = 34f },
            Width = { Pixels = ReplayBrowserLayout.SizeColumnWidth - ReplayBrowserLayout.StatColumnPadding * 2f },
            Height = { Pixels = 24f }
        });

        //Append(UISortableTableColumn.CreateSeparator(ReplayBrowserLayout.DateLeft, ReplayBrowserLayout.ReplayItemTotalHeight));
        //Append(UISortableTableColumn.CreateSeparator(ReplayBrowserLayout.DurationLeft, ReplayBrowserLayout.ReplayItemTotalHeight));

        // Hover label
        actionHoverLabel = new ActionHoverLabel();
        actionHoverLabel.Left.Set(ReplayBrowserLayout.ReplayItemHeight - ReplayBrowserLayout.ActionButtonRightPadding + ReplayBrowserLayout.ActionLabelGap, 0f);
        actionHoverLabel.Top.Set(ReplayBrowserLayout.ReplayItemHeight + (ReplayBrowserLayout.ReplayItemActionHeight - ReplayBrowserLayout.ActionButtonSize) * 0.5f, 0f);
        actionHoverLabel.Width.Set(ReplayBrowserLayout.ActionLabelWidth, 0f);
        actionHoverLabel.Height.Set(ReplayBrowserLayout.ActionButtonSize, 0f);
        Append(actionHoverLabel);

        ReplayActionDefinition[] actions =
        [
            new(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"), "Play", () => ReplayItemActions.Play(entry.FullPath)),
            new(Main.Assets.Request<Texture2D>("Images/UI/ButtonRename"), "Rename", () => ReplayItemActions.Rename(entry.FullPath, onDeleted))
        ];
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
            new ReplayActionDefinition(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"), "Delete", () => ReplayItemActions.Delete(entry.FullPath, onDeleted)),
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

        constructorWatch.Stop();

        if (constructorWatch.ElapsedMilliseconds >= 20)
            Log.Debug($"Replay row slow: {info.FileName} took {constructorWatch.ElapsedMilliseconds} ms");
    }

    private sealed class ReplayWorldIconElement : UIElement
    {
        private readonly Asset<Texture2D> icon;

        public ReplayWorldIconElement(ReplayDisplayInfo info, float size)
        {
            Width.Set(size, 0f);
            Height.Set(size, 0f);
            Left.Set(6f, 0f);
            Top.Set(6f, 0f);

            icon = GetIcon(info);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle area = GetDimensions().ToRectangle();
            Texture2D texture = icon.Value;
            spriteBatch.Draw(texture, area, Color.White);
        }

        private static Asset<Texture2D> GetIcon(ReplayDisplayInfo info)
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
        private readonly string tooltip;

        public ReplayNameElement(string text, string tooltip)
        {
            this.text = string.IsNullOrWhiteSpace(text) ? "-" : text;
            this.tooltip = tooltip;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            const float TextScale = 0.95f;

            Rectangle area = GetDimensions().ToRectangle();
            var font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text) * TextScale;
            Vector2 position = new(area.X, area.Y + (area.Height - size.Y) * 0.5f);

            Utils.DrawBorderString(spriteBatch, text, position, Color.White, TextScale);
        }
    }

    private UIImageButton[] AppendActionButtons(ReplayActionDefinition[] actions, ActionHoverLabel hoverLabel)
    {
        UIImageButton[] buttons = new UIImageButton[actions.Length];
        float buttonSize = ReplayBrowserLayout.ActionButtonSize;
        float buttonGap = ReplayBrowserLayout.ActionButtonGap;
        float top = ReplayBrowserLayout.ReplayItemHeight + (ReplayBrowserLayout.ReplayItemActionHeight - buttonSize) * 0.5f + 1;
        float left = ReplayBrowserLayout.ReplayItemHeight - ReplayBrowserLayout.ActionButtonRightPadding - buttonSize * actions.Length - buttonGap * Math.Max(0, actions.Length - 1) + 4;

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
        button.OnMouseOut += (_, _) => hoverLabel.ClearAction();
        button.OnUpdate += _ =>
        {
            if (button.IsMouseHovering)
            {
                hoverLabel.SetAction(action.Label);
                UICommon.TooltipMouseText(action.Label);
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

        public MainMenuStatElement(PlayerStatSnapshot stat, float scale, bool drawIcon = true, float iconScale = 1f, bool centerText = false, float textScaleMultiplier = 1f)
        {
            this.stat = stat;
            this.scale = scale;
            this.drawIcon = drawIcon;
            this.iconScale = iconScale;
            this.centerText = centerText;
            this.textScaleMultiplier = textScaleMultiplier;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            StatDrawer.DrawReplayStatInMainMenu(spriteBatch, GetDimensions().ToRectangle(), stat, scale, drawIcon, iconScale, centerText, textScaleMultiplier);
        }
    }

    private sealed class ReplayPreviewElement : UIElement
    {
        public ReplayPreviewElement(UICharacter preview, float size)
        {
            Width.Set(size, 0f);
            Height.Set(size, 0f);
            Left.Set(6, 0);
            Top.Set(6, 0);

            preview.Width.Set(size, 0f);
            preview.Height.Set(size, 0f);
            Append(preview);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            EntityDrawer.DrawEntityBackground(spriteBatch, GetDimensions().ToRectangle());
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
