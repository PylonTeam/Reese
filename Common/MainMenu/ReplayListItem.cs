using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace Reese.Common.MainMenu;

public class ReplayListItem : UIPanel
{
    private readonly string fullPath;
    private readonly UICharacter preview;
    private readonly UIImageButton playButton;
    private readonly HoverInfoPill datePill;
    private readonly HoverInfoPill durationPill;
    private readonly HoverInfoPill worldSizePill;
    private readonly HoverInfoPill difficultyPill;

    public ReplayListItem(string fullPath)
    {
        this.fullPath = fullPath;

        Width.Set(0f, 1f);
        Height.Set(68f, 0f);
        SetPadding(6f);

        BackgroundColor = new Color(63, 82, 151) * 0.95f;
        BorderColor = new Color(89, 116, 213) * 0.95f;

        preview = new UICharacter(BuildPreviewPlayer(), animated: true, hasBackPanel: true, characterScale: 0.9f, useAClone: true);
        preview.Left.Set(4f, 0f);
        preview.Top.Set(0f, 0f);
        Append(preview);

        string fileName = Path.GetFileName(fullPath);
        string dateText = File.GetLastWriteTime(fullPath).ToString("MMM d, yyyy, h:mm tt");

        UIText name = new(fileName, 0.9f)
        {
            Left = { Pixels = 68f },
            Top = { Pixels = 4f }
        };
        Append(name);

        datePill = new HoverInfoPill(dateText, "Date")
        {
            Left = { Pixels = 68f },
            Top = { Pixels = 30f },
            Width = { Pixels = 150f},
        };
        Append(datePill);

        durationPill = new HoverInfoPill("00:00", "Duration")
        {
            Left = { Pixels = 220f },
            Top = { Pixels = 30f },
            Width = { Pixels = 60f },
        };
        Append(durationPill);

        difficultyPill = new HoverInfoPill("PlayerName", "Player name")
        {
            Left = { Pixels = 280 },
            Top = { Pixels = 30f },
            Width = { Pixels = 80f }
        };
        Append(difficultyPill);

        worldSizePill = new HoverInfoPill("WorldName", "World name")
        {
            Left = { Pixels = 360 },
            Top = { Pixels = 30f },
            Width = { Pixels = 80f }
        };
        Append(worldSizePill);
        

        Asset<Texture2D> playAsset = Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay");
        const float playScale = 2.0f;

        playButton = new UIImageButton(playAsset);
        playButton.Width.Set(playAsset.Width() * playScale, 0f);
        playButton.Height.Set(playAsset.Height() * playScale, 0f);
        //playButton.Left.Set(-18f, 1f);
        playButton.Left.Set(0, 0);
        playButton.Top.Set(0f, 0f);
        playButton.HAlign = 1f;
        playButton.VAlign = 1f;
        playButton.SetVisibility(0f, 0f);
        playButton.OnLeftClick += (_, _) => ReplaysBrowserUIState.EnterReplay(fullPath);
        playButton.OnMouseOver += (_, _) => UICommon.TooltipMouseText("Play");

        playButton.OnDraw += _ =>
        {
            CalculatedStyle dim = playButton.GetDimensions();
            float alpha = playButton.IsMouseHovering ? 1f : 0.55f;

            Main.spriteBatch.Draw(playAsset.Value, dim.Position(), null, Color.White * alpha, 0f, Vector2.Zero, playScale, SpriteEffects.None, 0f);

            if (playButton._borderTexture != null && playButton.IsMouseHovering)
                Main.spriteBatch.Draw(playButton._borderTexture.Value, dim.Position(), null, Color.White, 0f, Vector2.Zero, playScale, SpriteEffects.None, 0f);

            if (playButton.IsMouseHovering)
                UICommon.TooltipMouseText("Play");
        };
        Append(playButton);
    }

    private static Player BuildPreviewPlayer()
    {
        Main.LoadPlayers();

        var players = Main.PlayerList
            .Where(x => x?.Player != null)
            .ToArray();

        if (players.Length > 0)
        {
            int index = Main.rand.Next(players.Length);
            return (Player)players[index].Player.clientClone();
        }

        return new Player();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering || playButton.IsMouseHovering)
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