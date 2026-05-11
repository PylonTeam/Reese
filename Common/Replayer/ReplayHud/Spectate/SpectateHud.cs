using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using Reese.Core.Configs;
using Reese.Core.Debug;
using Reese.Core.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.Spectate;

/// <summary>
/// Main spectator HUD in bottom center of the screen. 
/// Displays a horizontal list of spectatable players and allows the user to hover and lock onto a target.
/// </summary>
internal sealed class SpectateHud : UIPanel
{
    private const float HeaderHeight = 32f;
    private const float TabHeight = 36f;
    private const float StatusTextScale = 0.85f;
    private const float StatusPanelMinHeight = 28f;
    private const float StatusPanelPadding = 6f;
    private const float ContentGap = 4f;

    private const int MinShownPlayerCards = 1;
    private const int MaxShownPlayerCards = 3;
    private static int requestedShownPlayerCards = 1;
    private static bool userChangedShownPlayerCards;
    private static int cardCountRevision;
    private static int lastShownPlayerCards = 1;

    private int shownPlayerCards = 1; // number of player cards shown in the panel
    private int visibleTargetStart; // first target index shown in the player card window
    private int observedCardCountRevision;

    private int locked = -1; // currently locked spectated player index, -1 means no locked target
    private int lockedNpc = -1; // currently locked spectated NPC index, -1 means no locked NPC target
    private int hovered = -1; // currently hovered spectated player index, -1 means no hovered target
    private UIText statusText; // UI element for displaying the current status like "Spectating: PlayerName" or "Free camera" or "Auto-director"
    private string statusTextRaw = string.Empty;

    private readonly List<ITab> tabs = [];
    private ITab currentTab;
    private SpectatorTabBar tabBar;
    private UIPanel headerPanel;
    private UIPanel contentPanel;
    private UIPanel statusPanel;

    // Reflection
    private static readonly FieldInfo elementsField = typeof(UIElement).GetField("Elements", BindingFlags.Instance | BindingFlags.NonPublic);

    public SpectateHud()
    {
        Width.Set(GetPanelWidth(), 0f);
        Height.Set(0f, 0f);
        HAlign = 0.5f;
        //VAlign = 1f;
        //Top.Set(-25, 0f); // bottom padding
        ApplyTopOrBottomPosition(this, GetScale());
        //BackgroundColor = new Color(33, 43, 79) * 0.3f;
        BackgroundColor = new Color(73, 94, 171)*0.3f;

        tabs.Add(new PlayersTab());
        tabs.Add(new NPCsTab());
        currentTab = tabs[0];

        Rebuild();
    }

    public void Rebuild()
    {
        RemoveAllChildren();

        float scale = GetScale();
        float outerPadding = GetOuterPadding(scale);
        float headerHeight = GetHeaderHeight(scale);
        float tabHeight = GetTabHeight(scale);
        float playerPanelPadding = GetPlayerPanelPadding(scale);
        float navButtonWidth = GetNavButtonWidth(scale);
        float navButtonGap = GetNavButtonGap(scale);
        float cardGap = GetCardGap(scale);
        float cardWidth = UIPlayerCard.CardWidth * scale;
        float cardHeight = UIPlayerCard.CardHeight * scale;

        SetPadding(outerPadding);
        Width.Set(GetPanelWidth(), 0f);
        ApplyTopOrBottomPosition(this, scale);

        BackgroundColor = new Color(22, 28, 48) * 0.85f;
        BorderColor = Color.Black;

        currentTab ??= tabs.Count > 0 ? tabs[0] : null;

        // Target filtering
        List<int> targets = SpectatorTargetSystem.GetTargets(Main.myPlayer);

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            int playerIndex = targets[i];

            if (playerIndex < 0 || playerIndex >= Main.maxPlayers || Main.player[playerIndex] is null || !Main.player[playerIndex].active)
                targets.RemoveAt(i);
        }

        shownPlayerListHash = GetActivePlayerListHash();

        if (!targets.Contains(locked))
            locked = -1;

        if (!targets.Contains(hovered))
            hovered = -1;

        shownPlayerCards = GetShownPlayerCardsForTargetCount(targets.Count);
        lastShownPlayerCards = shownPlayerCards;
        requestedShownPlayerCards = shownPlayerCards;
        Width.Set(GetPanelWidth(), 0f);

        // Update the number of visible targets based on the number of player cards to show
        visibleTargetStart = Math.Clamp(visibleTargetStart, 0, Math.Max(0, targets.Count - shownPlayerCards));
        int visibleTargets = Math.Min(Math.Max(0, targets.Count - visibleTargetStart), shownPlayerCards);

        // Layout
        float shownCardsWidth = shownPlayerCards * cardWidth + Math.Max(0, shownPlayerCards - 1) * cardGap;
        float visibleCardsWidth = visibleTargets * cardWidth + Math.Max(0, visibleTargets - 1) * cardGap;
        float cardsStart = navButtonWidth + navButtonGap + Math.Max(0f, shownCardsWidth - visibleCardsWidth) * 0.5f;

        headerPanel = BuildHeaderPanel(headerHeight, scale);
        Append(headerPanel);

        tabBar = new SpectatorTabBar();
        tabBar.Top.Set(headerHeight, 0f);
        tabBar.Width.Set(0f, 1f);
        tabBar.Height.Set(tabHeight, 0f);
        tabBar.BuildTabs(tabs, () => currentTab, ShowTab, scale);
        Append(tabBar);

        contentPanel = new UIPanel();
        contentPanel.SetPadding(playerPanelPadding);
        contentPanel.Top.Set(headerHeight + tabHeight, 0f);
        contentPanel.Width.Set(0f, 1f);
        contentPanel.Height.Set(GetContentHeight(scale), 0f);
        contentPanel.BackgroundColor = UICommon.DefaultUIBlueMouseOver * 0.3f;
        contentPanel.BorderColor = Color.Black;
        Append(contentPanel);

        BuildContent(targets, visibleTargets, scale, playerPanelPadding, navButtonWidth, cardGap, cardWidth, cardHeight, cardsStart);

        statusPanel = new UIPanel();
        statusPanel.SetPadding(GetStatusPanelPadding(scale));
        statusPanel.Width.Set(0f, 1f);
        statusPanel.BackgroundColor = new Color(35, 54, 96) * 0.85f;
        statusPanel.BorderColor = Color.Black;
        Append(statusPanel);

        statusText = new UIText("", textScale: GetStatusTextScale(scale))
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            TextColor = Color.White
        };
        statusPanel.Append(statusText);
        statusTextRaw = string.Empty;
        UpdateStatusText();

        Recalculate();
    }

    private void ShowTab(SpectatorTab tab)
    {
        ITab nextTab = GetTab(tab);

        if (nextTab == null || currentTab == nextTab)
            return;

        currentTab = nextTab;
        Rebuild();
    }

    private ITab GetTab(SpectatorTab tab)
    {
        foreach (ITab candidate in tabs)
        {
            if (candidate.Tab == tab)
                return candidate;
        }

        return null;
    }

    private static UIPanel BuildHeaderPanel(float height, float scale)
    {
        UIPanel panel = new();
        panel.Height.Set(height, 0f);
        panel.Width.Set(0f, 1f);
        panel.SetPadding(0f);
        panel.BackgroundColor = new Color(63, 82, 151);
        panel.BorderColor = Color.Black;

        panel.Append(new UIText("Spectate Info", large: false, textScale: 1f * scale)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        });

        return panel;
    }

    private void BuildContent(
        List<int> targets,
        int visibleTargets,
        float scale,
        float playerPanelPadding,
        float navButtonWidth,
        float cardGap,
        float cardWidth,
        float cardHeight,
        float cardsStart)
    {
        if (contentPanel == null)
            return;

        contentPanel.RemoveAllChildren();

        if (currentTab?.Tab != SpectatorTab.Players)
            return;

        if (targets.Count == 0)
        {
            UIText noPlayersText = new("No players are available to spectate.", 0.9f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                TextColor = Color.LightGray
            };

            contentPanel.Append(noPlayersText);
            return;
        }

        AddPrevButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, scale);

        for (int i = 0; i < visibleTargets; i++)
        {
            int targetIndex = visibleTargetStart + i;
            int playerIndex = targets[targetIndex];

            UIPlayerCard playerCard = new(playerIndex, i, this, scale);
            playerCard.Width.Set(cardWidth, 0f);
            playerCard.Height.Set(cardHeight, 0f);
            playerCard.Left.Set(cardsStart + i * (cardWidth + cardGap), 0f);
            playerCard.OnLeftClick += (evt, element) =>
            {
                SpectatorTargetSystem.TogglePlayerTarget(playerIndex);
                UpdateTarget();
                UpdateStatusText();
            };
            contentPanel.Append(playerCard);
        }

        AddNextButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, scale);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        RebuildIfNeeded();
        RebuildIfCardCountChanged();

        if (currentTab?.Tab == SpectatorTab.Players)
        {
            HandleTargetNavigationKeys(gameTime);

            int nextHover = GetHoveredSlot();

            if (nextHover != hovered)
            {
                if (hovered >= 0)
                    EndHover();

                if (nextHover >= 0)
                    BeginHover(nextHover);
            }
        }
        else if (hovered >= 0)
        {
            EndHover();
        }

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    public void UpdateTarget()
    {
        int oldLocked = locked;
        int oldLockedNpc = lockedNpc;

        Player target = SpectatorTargetSystem.GetLockedPlayerTarget();
        locked = target?.active == true ? target.whoAmI : -1;

        NPC npcTarget = SpectatorTargetSystem.GetLockedNPCTarget();
        lockedNpc = npcTarget?.active == true ? npcTarget.whoAmI : -1;

        if (locked != oldLocked || lockedNpc != oldLockedNpc)
            UpdateStatusText();
    }

    private int GetHoveredSlot()
    {
        if (!ContainsPoint(Main.MouseScreen) || contentPanel == null)
            return -1;

        return GetHoveredSlot(contentPanel);
    }

    private static int GetHoveredSlot(UIElement element)
    {
        if (element is UIPlayerCard slot && slot.ContainsPoint(Main.MouseScreen))
            return slot.PlayerIndex;

        if (elementsField?.GetValue(element) is not List<UIElement> children)
            return -1;

        for (int i = 0; i < children.Count; i++)
        {
            int playerIndex = GetHoveredSlot(children[i]);

            if (playerIndex >= 0)
                return playerIndex;
        }

        return -1;
    }

    private void BeginHover(int playerIndex)
    {
        if (!IsTargetValid(playerIndex))
            return;

        hovered = playerIndex;
        SpectatorTargetSystem.SetPreviewTarget(playerIndex);
        UpdateStatusText();
    }

    private void EndHover()
    {
        hovered = -1;
        SpectatorTargetSystem.ClearPreviewTarget();
        UpdateStatusText();
    }

    private string GetStatusText()
    {
        if (hovered >= 0 && Main.player[hovered]?.active == true)
        {
            if (locked == hovered)
                return $"Following {Main.player[hovered].name}. Click to stop following";

            return $"Previewing {Main.player[hovered].name}. Click to follow";
        }

        if (locked >= 0 && Main.player[locked]?.active == true)
            return $"Following {Main.player[locked].name}";

        if (lockedNpc >= 0 && Main.npc[lockedNpc]?.active == true)
            return $"Following \"{Main.npc[lockedNpc].FullName}\"";

        return "You are in ghost mode";
    }

    private void UpdateStatusText()
    {
        SetStatusTextInternal(GetStatusText());
    }

    internal void SetStatusText(string text)
    {
        SetStatusTextInternal(text);
    }

    internal void ResetStatusText()
    {
        UpdateStatusText();
    }

    private void SetStatusTextInternal(string text)
    {
        if (statusText == null || statusPanel == null)
            return;

        text ??= string.Empty;

        if (statusTextRaw == text)
            return;

        statusTextRaw = text;

        float scale = GetScale();
        string wrappedText = WrapStatusText(text, GetStatusTextMaxWidth(scale), GetStatusTextScale(scale), out int lineCount);
        statusText.SetText(wrappedText, GetStatusTextScale(scale), false);
        UpdateStatusPanelLayout(scale, lineCount);
    }

    private void UpdateStatusPanelLayout(float scale, int lineCount)
    {
        float lineHeight = FontAssets.MouseText.Value.LineSpacing * GetStatusTextScale(scale);
        float textHeight = Math.Max(lineHeight, lineCount * lineHeight);
        float padding = GetStatusPanelPadding(scale);
        float statusHeight = Math.Max(textHeight + padding * 2f, StatusPanelMinHeight * scale);

        statusPanel.Height.Set(statusHeight, 0f);
        statusPanel.Top.Set(GetHeaderHeight(scale) + GetTabHeight(scale) + GetContentHeight(scale) + GetContentGap(scale), 0f);
        Height.Set(GetPanelHeight(statusHeight, scale), 0f);
        Recalculate();
    }

    private static bool IsTargetValid(int playerIndex)
    {
        return playerIndex >= 0 && SpectatorTargetSystem.GetTargets(Main.myPlayer).Contains(playerIndex);
    }

    public static int ShownPlayerCardCount => lastShownPlayerCards;

    public static void ChangeShownPlayerCards(int direction)
    {
        int targetCount = SpectatorTargetSystem.GetTargets(Main.myPlayer).Count;
        int maxShownPlayerCards = Math.Min(MaxShownPlayerCards, targetCount);
        int next = lastShownPlayerCards + direction;

        if (next < MinShownPlayerCards)
        {
            Main.NewText("Minimum players are already showing.", Color.Yellow);
            return;
        }

        if (next > maxShownPlayerCards)
        {
            Main.NewText("Maximum players are already showing.", Color.Yellow);
            return;
        }

        userChangedShownPlayerCards = true;
        requestedShownPlayerCards = next;
        cardCountRevision++;
    }

    #region Target navigation
    private Keys? heldNavigationKey;
    private double navigationRepeatTimer;

    private const double NavigationInitialRepeatDelay = 0.35;
    private const double NavigationRepeatInterval = 0.06;
    private void HandleTargetNavigationKeys(GameTime gameTime)
    {
        int direction = 0;

        if (KeyboardHelper.Pressed(Keys.Left))
            direction = -1;

        if (KeyboardHelper.Pressed(Keys.Right))
            direction = 1;

        if (direction != 0)
        {
            NavigateTarget(direction);
            heldNavigationKey = direction < 0 ? Keys.Left : Keys.Right;
            navigationRepeatTimer = NavigationInitialRepeatDelay;
            return;
        }

        if (heldNavigationKey is not Keys heldKey || !Main.keyState.IsKeyDown(heldKey))
        {
            heldNavigationKey = null;
            return;
        }

        navigationRepeatTimer -= gameTime.ElapsedGameTime.TotalSeconds;

        if (navigationRepeatTimer > 0)
            return;

        navigationRepeatTimer += NavigationRepeatInterval;
        NavigateTarget(heldKey == Keys.Left ? -1 : 1);
    }

    private void NavigateTarget(int direction)
    {
        List<int> targets = SpectatorTargetSystem.GetTargets(Main.myPlayer);

        if (targets.Count == 0)
        {
            SpectatorTargetSystem.ClearTarget();
            UpdateTarget();
            UpdateStatusText();
            return;
        }

        int oldVisibleTargetStart = visibleTargetStart;
        int currentIndex = targets.IndexOf(locked);
        int nextIndex = currentIndex < 0 ? direction < 0 ? targets.Count - 1 : 0 : currentIndex + direction;

        if (nextIndex < 0)
            nextIndex = targets.Count - 1;

        if (nextIndex >= targets.Count)
            nextIndex = 0;

        int playerIndex = targets[nextIndex];

        SpectatorTargetSystem.SetPlayerTarget(playerIndex);
        locked = playerIndex;
        lockedNpc = -1;

        MakeTargetVisible(nextIndex, targets.Count);

        if (visibleTargetStart != oldVisibleTargetStart)
            Rebuild();
        else
            UpdateStatusText();
    }

    private void MakeTargetVisible(int targetIndex, int targetCount)
    {
        int maxVisibleTargetStart = Math.Max(0, targetCount - shownPlayerCards);

        visibleTargetStart = Math.Clamp(visibleTargetStart, 0, maxVisibleTargetStart);

        if (targetIndex < visibleTargetStart)
            visibleTargetStart = targetIndex;
        else if (targetIndex >= visibleTargetStart + shownPlayerCards)
            visibleTargetStart = targetIndex - shownPlayerCards + 1;

        visibleTargetStart = Math.Clamp(visibleTargetStart, 0, maxVisibleTargetStart);
    }

    #endregion

    #region Rebuild if player list changes
    public override void OnActivate()
    {
        base.OnActivate();
        Rebuild();
    }
    private int shownPlayerListHash; // cached active Main.player list hash to detect joins/leaves
    
    private void RebuildIfNeeded()
    {
#if DEBUG
        if (KeyboardHelper.Pressed(Keys.F5))
        {
            Log.Chat("F5 pressed, rebuilding. Last player hash: " + shownPlayerListHash);
            Rebuild();
        }
#endif

            // Update player cache if the list of targets has changed
            // Update player cache if the Main.player list has changed
            int playerListHash = GetActivePlayerListHash();

        if (playerListHash != shownPlayerListHash)
            Rebuild();
    }

    private void RebuildIfCardCountChanged()
    {
        if (observedCardCountRevision == cardCountRevision)
            return;

        observedCardCountRevision = cardCountRevision;
        Rebuild();
    }
    private static int GetActivePlayerListHash()
    {
        HashCode hash = new();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (player is null || !player.active)
                continue;

            hash.Add(i);
            //hash.Add(player.whoAmI);
            //hash.Add(player.name);
        }

        return hash.ToHashCode();
    }
    #endregion

    #region Layout Helpers
    private static void ApplyTopOrBottomPosition(UIElement element, float scale)
    {
        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();

        if (clientConfig.replayHudPosition == ClientConfig.ReplayHudPosition.Top)
        {
            element.VAlign = 0f;
            element.Top.Set(40f * scale, 0f);
            return;
        }

        element.VAlign = 1f;
        element.Top.Set(-25f * scale, 0f);
    }

    private static float GetScale()
    {
        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();

        float scale = clientConfig.replayHudSize switch
        {
            ClientConfig.ReplayHudSize.Small => 0.6f,
            ClientConfig.ReplayHudSize.Medium => 0.8f,
            ClientConfig.ReplayHudSize.Large => 1.0f,
            _ => 1f
        };

        return scale;
    }

    private static float GetHeaderHeight(float scale) => HeaderHeight * scale;

    private static float GetTabHeight(float scale) => TabHeight * scale;

    private static float GetOuterPadding(float scale) => 10f * scale;

    private static float GetContentGap(float scale) => ContentGap * scale;

    private static float GetPlayerPanelPadding(float scale) => 4f * scale;

    private static float GetNavButtonWidth(float scale) => 24f * scale;

    private static float GetNavButtonGap(float scale) => 8f * scale;

    private static float GetCardGap(float scale) => 6f * scale;

    private static float GetStatusPanelPadding(float scale) => StatusPanelPadding * scale;

    private static float GetStatusTextScale(float scale) => StatusTextScale * scale;

    private static float GetContentHeight(float scale) => UIPlayerCard.CardHeight * scale + GetPlayerPanelPadding(scale) * 2f;

    private static int GetShownPlayerCardsForTargetCount(int targetCount)
    {
        if (targetCount <= 0)
            return MinShownPlayerCards;

        int maxShownPlayerCards = Math.Min(MaxShownPlayerCards, targetCount);
        int desired = userChangedShownPlayerCards ? requestedShownPlayerCards : targetCount;

        return Math.Clamp(desired, MinShownPlayerCards, maxShownPlayerCards);
    }

    private static float GetPanelHeight(float statusPanelHeight, float scale)
    {
        float outerPadding = GetOuterPadding(scale);
        float contentHeight = GetContentHeight(scale);
        float headerHeight = GetHeaderHeight(scale);
        float tabHeight = GetTabHeight(scale);
        float rowGap = GetContentGap(scale);

        return outerPadding * 2f + headerHeight + tabHeight + contentHeight + rowGap + statusPanelHeight;
    }

    private int GetPanelWidth()
    {
        float scale = GetScale();
        float outerPadding = GetOuterPadding(scale);
        float playerPanelPadding = GetPlayerPanelPadding(scale);
        float navButtonWidth = GetNavButtonWidth(scale);
        float navButtonGap = GetNavButtonGap(scale);
        float cardGap = GetCardGap(scale);
        float cardWidth = UIPlayerCard.CardWidth * scale;

        return (int)(outerPadding * 2f + playerPanelPadding * 2f + shownPlayerCards * cardWidth + Math.Max(0, shownPlayerCards - 1) * cardGap + navButtonWidth * 2f + navButtonGap * 2f);
    }

    private float GetStatusTextMaxWidth(float scale)
    {
        float outerPadding = GetOuterPadding(scale);
        float statusPadding = GetStatusPanelPadding(scale);
        float width = GetPanelWidth() - outerPadding * 2f - statusPadding * 2f;
        return Math.Max(40f * scale, width);
    }

    private static string WrapStatusText(string text, float maxWidth, float textScale, out int lineCount)
    {
        lineCount = 1;

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (maxWidth <= 0f)
            return text;

        var font = FontAssets.MouseText.Value;
        string[] words = text.Split(' ');
        StringBuilder builder = new();
        float lineWidth = 0f;
        int lines = 1;

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            string token = lineWidth == 0f ? word : " " + word;
            float tokenWidth = font.MeasureString(token).X * textScale;

            if (lineWidth > 0f && lineWidth + tokenWidth > maxWidth)
            {
                builder.Append('\n');
                lines++;
                lineWidth = 0f;
                token = word;
                tokenWidth = font.MeasureString(token).X * textScale;
            }

            builder.Append(token);
            lineWidth += tokenWidth;
        }

        lineCount = Math.Max(1, lines);
        return builder.ToString();
    }

    private void AddPrevButton(UIPanel playersPanel, float playerPanelPadding, float navButtonWidth, float cardHeight, float scale)
    {
        UIAutoScaleTextTextPanel<string> prevButton = new("<");
        prevButton.SetPadding(0f);
        prevButton.Top.Set(playerPanelPadding + cardHeight * 0.5f - 15f * scale, 0f);
        prevButton.Height.Set(30f * scale, 0f); prevButton.Width.Set(navButtonWidth, 0f);
        prevButton.BackgroundColor = new Color(55, 48, 92) * 0.9f;
        prevButton.BorderColor = Color.Black;
        prevButton.OnLeftClick += (evt, element) => NavigateTarget(-1);
        prevButton.OnMouseOver += (evt, element) =>
        {
            prevButton.BorderColor = Color.Yellow;
            SetStatusText("Go to previous player");
        };
        prevButton.OnMouseOut += (evt, element) =>
        {
            prevButton.BorderColor = Color.Black;
            UpdateStatusText();
        };

        playersPanel.Append(prevButton);
    }

    private void AddNextButton(UIPanel playersPanel, float playerPanelPadding, float navButtonWidth, float cardHeight, float scale)
    {
        UIAutoScaleTextTextPanel<string> nextButton = new(">");
        nextButton.SetPadding(0f);
        nextButton.HAlign = 1f;
        nextButton.Top.Set(playerPanelPadding + cardHeight * 0.5f - 15f * scale, 0f);
        nextButton.Height.Set(30f * scale, 0f);
        nextButton.Width.Set(navButtonWidth, 0f);
        nextButton.BackgroundColor = new Color(55, 48, 92) * 0.9f;
        nextButton.BorderColor = Color.Black;
        nextButton.OnLeftClick += (evt, element) => NavigateTarget(1);
        nextButton.OnMouseOver += (evt, element) =>
        {
            nextButton.BorderColor = Color.Yellow;
            SetStatusText("Go to next player");
        };
        nextButton.OnMouseOut += (evt, element) =>
        {
            nextButton.BorderColor = Color.Black;
            UpdateStatusText();
        };

        playersPanel.Append(nextButton);
    }
    #endregion

    private sealed class PlayersTab : ITab
    {
        public SpectatorTab Tab => SpectatorTab.Players;
        public string HeaderText => "Players";
        public string TooltipText => "Spectate players";
        public Asset<Texture2D> Icon => Ass.Icon_Player;
        public float IconScale => 1f;
        public Vector2 IconOffset => new(0f, -2f);

        public void Refresh()
        {
        }
    }

    private sealed class NPCsTab : ITab
    {
        public SpectatorTab Tab => SpectatorTab.NPCs;
        public string HeaderText => "NPCs";
        public string TooltipText => "Spectate NPCs";
        public Asset<Texture2D> Icon => Ass.Icon_NPC;
        public float IconScale => 1f;
        public Vector2 IconOffset => new(0f, -2f);

        public void Refresh()
        {
        }
    }

}
