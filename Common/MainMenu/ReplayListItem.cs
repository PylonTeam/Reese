using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reese.Common.GhostSpectate.Drawers;
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

    public ReplayListItem(string fullPath, Action onDeleted)
    {
        ReplayBrowserLayout.Update();

        Width.Set(0f, 1f);
        Height.Set(ReplayBrowserLayout.ReplayItemHeight, 0f);
        SetPadding(0f);

        BackgroundColor = new Color(63, 82, 151) * 0.95f;
        BorderColor = new Color(89, 116, 213) * 0.95f;
        OnLeftDoubleClick += (_, _) => ReplayMenuActions.Play(fullPath);

        ReplayDisplayInfo info = ReplayDisplayInfo.FromFile(fullPath);

        preview = BuildPreviewCharacter(info);
        preview.SetAnimated(false);
        Append(new ReplayPreviewElement(preview, ReplayBrowserLayout.ReplayItemHeight));

        string dateLine = info.Date.ToString("yyyy-MM-dd");
        string timeLine = info.Date.ToString("HH:mm");
        string dateTooltip = $"Date: {dateLine} {timeLine}";

        Append(new ReplayNameElement(Path.GetFileNameWithoutExtension(info.FileName), info.MetadataTooltip, () => ReplayMenuActions.Play(fullPath))
        {
            Left = { Pixels = 68f },
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

        Append(UISortableTableColumn.CreateCenteredTwoLineCell(dateLine, timeLine, ReplayBrowserLayout.DateLeft, ReplayBrowserLayout.DateColumnWidth, dateTooltip));
        Append(UISortableTableColumn.CreateLeftTextCell(info.DurationText, ReplayBrowserLayout.DurationLeft, ReplayBrowserLayout.DurationColumnWidth - ReplayBrowserLayout.ActionColumnWidth, $"Duration: {info.DurationText}"));
        Append(UISortableTableColumn.CreateSeparator(ReplayBrowserLayout.DateLeft, 68f));
        Append(UISortableTableColumn.CreateSeparator(ReplayBrowserLayout.DurationLeft, 68f));

        const float buttonSize = 22f;
        const float buttonGap = 2f;
        float buttonTop = (ReplayBrowserLayout.ReplayItemHeight - buttonSize) * 0.5f;
        float actionLeft = -ReplayBrowserLayout.ActionColumnWidth - 1f;
        Append(CreateVanillaImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"), "Play", actionLeft, buttonTop, buttonSize, () => ReplayMenuActions.Play(fullPath)));
        Append(CreateVanillaImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"), "Delete", actionLeft + buttonSize + buttonGap, buttonTop, buttonSize, () => ReplayMenuActions.Delete(fullPath, onDeleted)));
    }

    private sealed class ReplayNameElement : UIElement
    {
        private const float TextScale = 0.86f;

        private readonly string text;
        private readonly string tooltip;
        private readonly Action onDoubleClick;

        public ReplayNameElement(string text, string tooltip, Action onDoubleClick)
        {
            this.text = string.IsNullOrWhiteSpace(text) ? "-" : text;
            this.tooltip = tooltip;
            this.onDoubleClick = onDoubleClick;
            OnLeftDoubleClick += (_, _) => this.onDoubleClick?.Invoke();
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

    private static UIImageButton CreateVanillaImageButton(Asset<Texture2D> texture, string tooltip, float left, float top, float size, Action onClick)
    {
        UIImageButton button = new(texture);
        button.Left.Set(left, 1f);
        button.Top.Set(top, 0f);
        button.Width.Set(size, 0f);
        button.Height.Set(size, 0f);
        button.SetVisibility(1f, 0.55f);

        bool playedTick = false;
        button.OnMouseOver += (_, _) =>
        {
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
                UICommon.TooltipMouseText(tooltip);
        };
        button.OnLeftClick += (_, _) => onClick?.Invoke();
        return button;
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
        preview.SetAnimated(false);
    }
}
