using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Reese.Core.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.ModBrowser;
using Terraria.UI;

namespace Reese.Common.MainMenu.State;

internal sealed class ExtraReeseMainMenuUIState : UIState
{
    private const float FooterButtonHeight = 40f;
    private const float FooterButtonGap = 6f;
    private const float FooterGap = 12f;
    private const float FooterHeight = FooterButtonHeight * 2f + FooterButtonGap;

    private readonly Action onBack;
    private ReplayBrowser replayBrowser;
    private UIAutoScaleTextTextPanel<string> refreshButton;
    private UIBrowserStatus statusBadge;
    private MainMenuLoaderImage loaderImage;
    private bool refreshLoading;
    private DateTime refreshLoadingUntil;
    private string statusText = "Completed";
    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastUiScale;

    public ExtraReeseMainMenuUIState(Action onBack)
    {
        this.onBack = onBack;
        Rebuild();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (KeyboardHelper.Pressed(Keys.Escape))
            onBack?.Invoke();

#if DEBUG
        if (KeyboardHelper.Pressed(Keys.F5))
        {
            Rebuild();
            return;
        }
#endif

        if (refreshLoading && DateTime.UtcNow >= refreshLoadingUntil)
            SetCurrentAsyncState(AsyncProviderState.Completed);

        UpdateScreenMetrics();
    }

    private void Rebuild()
    {
        RemoveAllChildren();

        replayBrowser = new ReplayBrowser
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        replayBrowser.OnRefreshStarted += () =>
        {
            SetCurrentAsyncState(AsyncProviderState.Loading);
            refreshLoadingUntil = DateTime.UtcNow.AddSeconds(0.5);
        };
        Append(replayBrowser);

        loaderImage = new MainMenuLoaderImage(0.5f, 0.5f, 1f)
        {
            WithBackground = false
        };

        UIElement footer = CreateFooter();
        footer.HAlign = 0.5f;
        footer.VAlign = 0.5f;
        footer.Width.Set(ReplayBrowser.PanelWidth, 0f);
        footer.Height.Set(FooterHeight, 0f);
        footer.Top.Set(ReplayBrowser.BrowserPanelHeight * 0.5f + FooterGap + FooterHeight * 0.5f, 0f);
        Append(footer);

        SetCurrentAsyncState(AsyncProviderState.Completed);
        UpdateScreenMetrics(force: true);
    }

    private UIElement CreateFooter()
    {
        UIElement footer = new();
        footer.SetPadding(0f);

        UIElement buttons = new();
        buttons.Width.Set(-10f, 0.5f);
        buttons.Height.Set(0f, 1f);
        footer.Append(buttons);

        refreshButton = CreateActionButton("Refresh", Refresh);
        buttons.Append(refreshButton);

        UIAutoScaleTextTextPanel<string> backButton = CreateActionButton("Back", () => onBack?.Invoke());
        backButton.Top.Set(FooterButtonHeight + FooterButtonGap, 0f);
        buttons.Append(backButton);

        statusBadge = new UIBrowserStatus
        {
            HAlign = 1f,
            VAlign = 0.5f
        };
        statusBadge.OnUpdate += _ =>
        {
            if (statusBadge.IsMouseHovering)
                UICommon.TooltipMouseText(statusText);
        };
        footer.Append(statusBadge);

        return footer;
    }

    private void Refresh()
    {
        replayBrowser?.Refresh();
    }

    private void SetCurrentAsyncState(AsyncProviderState state, string text = null)
    {
        bool loading = state == AsyncProviderState.Loading;

        refreshLoading = loading;
        refreshButton?.SetText(loading ? "Loading" : "Refresh");
        statusBadge?.SetCurrentState(state);
        statusText = text ?? state.ToString();

        if (loading)
        {
            if (loaderImage != null && loaderImage.Parent == null)
                Append(loaderImage);
        }
        else if (loaderImage?.Parent != null)
        {
            loaderImage.Parent.RemoveChild(loaderImage);
        }
    }

    private static UIAutoScaleTextTextPanel<string> CreateActionButton(string text, Action onClick)
    {
        UIAutoScaleTextTextPanel<string> button = new(text);
        button.Width.Set(0f, 1f);
        button.Height.Set(FooterButtonHeight, 0f);
        button.PaddingLeft = 10f;
        button.PaddingRight = 10f;
        button.PaddingTop = 6f;
        button.PaddingBottom = 6f;
        button.BackgroundColor = UICommon.DefaultUIBlueMouseOver;
        button.BorderColor = Color.Black;

        bool playedTick = false;
        button.OnMouseOver += (_, _) =>
        {
            button.BackgroundColor = UICommon.DefaultUIBlue;
            button.BorderColor = Colors.FancyUIFatButtonMouseOver;
            if (playedTick)
                return;

            SoundEngine.PlaySound(SoundID.MenuTick);
            playedTick = true;
        };
        button.OnMouseOut += (_, _) =>
        {
            button.BackgroundColor = UICommon.DefaultUIBlueMouseOver;
            button.BorderColor = Color.Black;
            playedTick = false;
        };
        button.OnLeftClick += (_, _) => onClick?.Invoke();
        return button;
    }

    private void UpdateScreenMetrics(bool force = false)
    {
        if (!force && lastScreenWidth == Main.screenWidth && lastScreenHeight == Main.screenHeight && lastUiScale == Main.UIScale)
            return;

        lastScreenWidth = Main.screenWidth;
        lastScreenHeight = Main.screenHeight;
        lastUiScale = Main.UIScale;
        Recalculate();
    }

    private sealed class MainMenuLoaderImage : UIElement
    {
        public bool WithBackground;
        public int FrameTick;
        public int Frame;

        private readonly float scale;
        private readonly Asset<Texture2D> backgroundTexture;
        private readonly Asset<Texture2D> loaderTexture;

        public MainMenuLoaderImage(float hAlign, float vAlign, float scale = 1f)
        {
            this.scale = scale;
            backgroundTexture = UICommon.LoaderBgTexture;
            loaderTexture = UICommon.LoaderTexture;

            Width.Set(200f * scale, 0f);
            Height.Set(200f * scale, 0f);
            HAlign = hAlign;
            VAlign = vAlign;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (loaderTexture?.Value == null)
                return;

            if (++FrameTick >= 5)
            {
                FrameTick = 0;
                if (++Frame >= 16)
                    Frame = 0;
            }

            CalculatedStyle dimensions = GetDimensions();
            Vector2 position = new((int)dimensions.X, (int)dimensions.Y);

            if (WithBackground && backgroundTexture?.Value != null)
                spriteBatch.Draw(backgroundTexture.Value, position, new Rectangle(0, 0, 200, 200), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            Rectangle frame = new(200 * (Frame / 8), 200 * (Frame % 8), 200, 200);
            spriteBatch.Draw(loaderTexture.Value, position, frame, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}

internal sealed class MainMenuReplayBrowserUIState : UIState
{
    private const float Margin = 20f;

    private ReplayBrowser replayBrowser;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastUiScale;

    public override void OnInitialize()
    {
        replayBrowser = new ReplayBrowser
        {
            Left = { Pixels = -(ReplayBrowser.PanelWidth + Margin), Percent = 1f },
            Top = { Pixels = Margin }
        };
        Append(replayBrowser);
        UpdateScreenMetrics(force: true);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UpdateScreenMetrics();
    }

    private void UpdateScreenMetrics(bool force = false)
    {
        if (!force && lastScreenWidth == Main.screenWidth && lastScreenHeight == Main.screenHeight && lastUiScale == Main.UIScale)
            return;

        lastScreenWidth = Main.screenWidth;
        lastScreenHeight = Main.screenHeight;
        lastUiScale = Main.UIScale;
        Recalculate();
    }
}
