using Microsoft.Xna.Framework.Input;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using Reese.Common.Replayer.ReplayHud.Shared.UI;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

/// <summary>
/// Main spectator HUD in bottom center of the screen. 
/// Displays a horizontal list of spectatable players and allows the user to hover and lock onto a target.
/// </summary>
internal sealed class SpectateHud : UIElement
{
    // Layout
    private const float HeaderHeight = 32f;
    private const float TabHeight = 36f;
    private const float NavButtonGap = 6f;
    private const float ContentGap = 6f;

    // Card display logic
    private const int MaxVisibleCards = 3;

    private int visiblePlayerStart; // index of the first visible player target in the list of all targets, used for pagination
    private int visibleNpcStart; // index of the first visible NPC target in the list of all targets, used for pagination

    // Targeting
    private int locked = -1; // currently locked spectated player index, -1 means no locked target
    private int lockedNpc = -1; // currently locked spectated NPC index, -1 means no locked NPC target
    private int hovered = -1; // currently hovered spectated player index, -1 means no hovered target
    private float panelWidth;
    
    // Content
    private readonly List<ITab> tabs = [];
    private ITab currentTab;
    private TabBar tabBar;
    private UIPanel headerPanel;
    private UIPanel contentPanel;
    private UIStatusPanel statusPanel;

    // Reflection
    private static readonly FieldInfo elementsField = typeof(UIElement).GetField("Elements", BindingFlags.Instance | BindingFlags.NonPublic);

    private bool IsPvPAdventureLoaded => ModLoader.TryGetMod("PvPAdventure", out _);

    public SpectateHud()
    {
        HAlign = 0.5f;
        VAlign = 0f;
        Left.Set(0, 0f);
        Top.Set(IsPvPAdventureLoaded ? 40 : 4 , 0f); 
        Width.Set(ReplayInfo.InfoHud.PanelWidth, 0f);
        Height.Set(GetPanelHeight(), 0f);

        tabs.Add(new PlayersTab());
        tabs.Add(new NPCsTab());
        currentTab = tabs[0];

        Rebuild();
    }

    private void Rebuild()
    {
        //Log.Chat("Rebuilding SpectateHud...");

        RemoveAllChildren();

        // Layout
        float scale = GetScale();
        float headerHeight = GetHeaderHeight();
        float tabHeight = GetTabHeight();
        float playerPanelPadding = GetPlayerPanelPadding();
        float navButtonWidth = GetNavButtonWidth();
        float cardGap = GetCardGap();
        float cardWidth = GetCardWidth();
        float cardHeight = GetCardHeight();
        float contentHeight = GetContentHeight();

        currentTab ??= tabs.Count > 0 ? tabs[0] : null;

        Height.Set(GetPanelHeight(), 0f);

        // Header
        headerPanel = BuildHeaderPanel(headerHeight);
        Append(headerPanel);

        // Tabs
        tabBar = new TabBar();
        tabBar.Top.Set(headerHeight, 0f);
        tabBar.Width.Set(0f, 1f);
        tabBar.Height.Set(tabHeight, 0f);
        tabBar.BuildTabs(tabs, () => currentTab, ShowTab, scale);
        Append(tabBar);

        // Content
        contentPanel = new UIPanel();
        contentPanel.SetPadding(playerPanelPadding);
        contentPanel.Top.Set(headerHeight + tabHeight, 0f);
        contentPanel.Width.Set(0f, 1f);
        contentPanel.Height.Set(contentHeight, 0f);
        //contentPanel.BackgroundColor = UICommon.DefaultUIBlueMouseOver * 0.9f;
        contentPanel.BackgroundColor = new Color(20, 20, 60) * 0.9f;
        contentPanel.BorderColor = Color.Black;
        Append(contentPanel);

        statusPanel = new UIStatusPanel(scale);
        statusPanel.Top.Set(headerHeight + tabHeight + contentHeight + GetContentGap(), 0f);
        Append(statusPanel);

        RefreshTargets();
    }

    private void RefreshTargets()
    {
        if (contentPanel == null)
            return;

        //Log.Chat("Refreshing SpectateHud...");

        float scale = GetScale();
        float playerPanelPadding = GetPlayerPanelPadding();
        float navButtonWidth = GetNavButtonWidth();
        float cardGap = GetCardGap();
        float cardWidth = GetCardWidth();
        float cardHeight = GetCardHeight();

        List<int> playerTargets = GetPlayerTargets();
        List<int> npcTargets = GetNpcTargets();

        if (!playerTargets.Contains(locked))
            locked = -1;

        if (!playerTargets.Contains(hovered))
            hovered = -1;

        if (!npcTargets.Contains(lockedNpc))
            lockedNpc = -1;

        int playerCards = Math.Min(MaxVisibleCards, playerTargets.Count);
        int npcCards = Math.Min(MaxVisibleCards, npcTargets.Count);

        visiblePlayerStart = ClampVisibleStart(visiblePlayerStart, playerTargets.Count, playerCards);
        visibleNpcStart = ClampVisibleStart(visibleNpcStart, npcTargets.Count, npcCards);

        bool showingNpcs = currentTab?.Tab == SpectatorTab.NPCs;
        int activeRealCards = showingNpcs ? npcCards : playerCards;
        int activeDisplayCards = Math.Max(1, activeRealCards);
        int activeTargetCount = showingNpcs ? npcTargets.Count : playerTargets.Count;
        bool showButtons = activeTargetCount > activeDisplayCards;
        float cardsStart = showButtons ? navButtonWidth + NavButtonGap * scale : 0f;

        panelWidth = GetPanelWidth(activeDisplayCards, cardWidth, showButtons);
        Width.Set(panelWidth, 0f);

        tabBar?.RefreshHeaders();
        BuildContent(playerTargets, npcTargets, playerCards, npcCards, playerPanelPadding, navButtonWidth, cardGap, cardWidth, cardHeight, cardsStart, showButtons);

        Recalculate();
        UpdateStatusText();
    }

    private void ShowTab(SpectatorTab tab)
    {
        ITab nextTab = GetTab(tab);

        if (nextTab == null || currentTab == nextTab)
            return;

        currentTab = nextTab;
        RefreshTargets();
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

    private UIPanel BuildHeaderPanel(float height)
    {
        float scale = GetScale();

        UIPanel panel = new();
        panel.Height.Set(height, 0f);
        panel.Width.Set(0f, 1f);
        panel.SetPadding(0f);
        panel.BackgroundColor = new Color(63, 82, 151);
        panel.BorderColor = Color.Black;

        panel.Append(new UIText("Spectate", large: false, textScale: 1f * scale)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        });

        Color normalColor = panel.BackgroundColor;
        Color hoverColor = new Color(95, 45, 55);

        panel.Append(new ClosePanel(
            () => ModContent.GetInstance<ReplayHudSystem>().CloseSpectateHud(),
            scale,
            normalColor
        ));

        return panel;
    }

    private void BuildContent(
        List<int> playerTargets,
        List<int> npcTargets,
        int playerCards,
        int npcCards,
        float playerPanelPadding,
        float navButtonWidth,
        float cardGap,
        float cardWidth,
        float cardHeight,
        float cardsStart,
        bool showButtons)
    {
        if (contentPanel == null)
            return;

        contentPanel.RemoveAllChildren();

        float scale = GetScale();

        if (currentTab?.Tab == SpectatorTab.Players)
        {
            if (playerTargets.Count == 0)
            {
                contentPanel.Append(new UIText("No players are available\n to spectate.", 0.9f)
                {
                    HAlign = 0.5f,
                    VAlign = 0.5f,
                    TextColor = Color.LightGray
                });

                return;
            }

            if (showButtons)
                AddPrevButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, () => NavigateTarget(-1), "Go to previous player");

            for (int i = 0; i < playerCards; i++)
            {
                int playerIndex = playerTargets[visiblePlayerStart + i];

                UIPlayerCard playerCard = new(playerIndex, i, this, scale);
                playerCard.Width.Set(cardWidth, 0f);
                playerCard.Height.Set(cardHeight, 0f);
                playerCard.Left.Set(cardsStart + i * (cardWidth + cardGap), 0f);
                playerCard.OnLeftClick += (evt, _) =>
                {
                    if (evt.Target != playerCard)
                        return;

                    SpectatorTargetSystem.TogglePlayerTarget(playerIndex);
                    UpdateTarget();
                    UpdateStatusText();
                };

                contentPanel.Append(playerCard);
            }

            if (showButtons)
                AddNextButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, () => NavigateTarget(1), "Go to next player");

            return;
        }

        if (currentTab?.Tab != SpectatorTab.NPCs)
            return;

        if (npcTargets.Count == 0)
        {
            contentPanel.Append(new UIText("No NPCs are available\n to spectate.", 0.9f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                TextColor = Color.LightGray
            });

            return;
        }

        if (showButtons)
            AddPrevButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, () => NavigateNpcTarget(-1), "Go to previous NPC");

        for (int i = 0; i < npcCards; i++)
        {
            int npcIndex = npcTargets[visibleNpcStart + i];

            UINPCCard npcCard = new(npcIndex, i, scale);
            npcCard.Width.Set(cardWidth, 0f);
            npcCard.Height.Set(cardHeight, 0f);
            npcCard.Left.Set(cardsStart + i * (cardWidth + cardGap), 0f);
            npcCard.OnLeftClick += (_, _) =>
            {
                UpdateTarget();
                UpdateStatusText();
            };

            contentPanel.Append(npcCard);
        }

        if (showButtons)
            AddNextButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, () => NavigateNpcTarget(1), "Go to next NPC");
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        RefreshTargetsIfNeeded();
        UpdateTarget();

        if (currentTab?.Tab == SpectatorTab.Players)
        {
            HandleNavigationKeys(gameTime, NavigateTarget);

            int nextHover = GetHoveredSlot();

            if (nextHover != hovered)
            {
                if (hovered >= 0)
                    EndHover();

                if (nextHover >= 0)
                    BeginHover(nextHover);
            }
        }
        else
        {
            if (currentTab?.Tab == SpectatorTab.NPCs)
                HandleNavigationKeys(gameTime, NavigateNpcTarget);

            if (hovered >= 0)
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
                return $"Click to stop spectating {Main.player[hovered].name}";

            return $"Click to spectate {Main.player[hovered].name}";
        }

        if (locked >= 0 && Main.player[locked]?.active == true)
            return $"Spectating {Main.player[locked].name}";

        if (lockedNpc >= 0 && Main.npc[lockedNpc]?.active == true)
            return $"Spectating {Main.npc[lockedNpc].FullName}";

        if (Main.LocalPlayer?.ghost == true)
            return "Ghost mode enabled";

        return "You are not spectating anyone";
    }

    private void UpdateStatusText()
    {
        if (statusPanel == null)
            return;

        bool showGhost = hovered < 0 && locked < 0 && lockedNpc < 0 && Main.LocalPlayer?.ghost == true;
        statusPanel.SetStatus(GetStatusText(), showGhost);
    }

    private static bool IsTargetValid(int playerIndex)
    {
        return playerIndex >= 0 && SpectatorTargetSystem.GetTargets(Main.myPlayer).Contains(playerIndex);
    }

    #region Target navigation
    private Keys? heldNavigationKey;
    private double navigationRepeatTimer;

    private const double NavigationInitialRepeatDelay = 0.35;
    private const double NavigationRepeatInterval = 0.06;
    private void HandleNavigationKeys(GameTime gameTime, Action<int> navigate)
    {
        int direction = 0;

        if (KeyboardHelper.Pressed(Keys.Left))
            direction = -1;

        if (KeyboardHelper.Pressed(Keys.Right))
            direction = 1;

        if (direction != 0)
        {
            navigate(direction);
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
        navigate(heldKey == Keys.Left ? -1 : 1);
    }

    private void NavigateTarget(int direction)
    {
        List<int> targets = GetPlayerTargets();

        if (targets.Count == 0)
        {
            SpectatorTargetSystem.ClearTarget();
            UpdateTarget();
            UpdateStatusText();
            return;
        }

        int oldVisiblePlayerStart = visiblePlayerStart;
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
        visiblePlayerStart = MakeVisible(visiblePlayerStart, nextIndex, targets.Count);

        if (visiblePlayerStart != oldVisiblePlayerStart)
            Rebuild();
        else
            UpdateStatusText();
    }

    private void NavigateNpcTarget(int direction)
    {
        List<int> targets = GetNpcTargets();

        if (targets.Count == 0)
        {
            SpectatorTargetSystem.ClearTarget();
            UpdateTarget();
            UpdateStatusText();
            return;
        }

        int oldVisibleNpcStart = visibleNpcStart;
        int currentIndex = targets.IndexOf(lockedNpc);
        int nextIndex = currentIndex < 0 ? direction < 0 ? targets.Count - 1 : 0 : currentIndex + direction;

        if (nextIndex < 0)
            nextIndex = targets.Count - 1;

        if (nextIndex >= targets.Count)
            nextIndex = 0;

        int npcIndex = targets[nextIndex];

        SpectatorTargetSystem.SetNPCTarget(npcIndex);
        lockedNpc = npcIndex;
        locked = -1;
        visibleNpcStart = MakeVisible(visibleNpcStart, nextIndex, targets.Count);

        if (visibleNpcStart != oldVisibleNpcStart)
            Rebuild();
        else
            UpdateStatusText();
    }

    private static int ClampVisibleStart(int start, int targetCount, int visibleCards)
    {
        return Math.Clamp(start, 0, Math.Max(0, targetCount - visibleCards));
    }

    private static int MakeVisible(int start, int targetIndex, int targetCount)
    {
        int visibleCards = Math.Min(MaxVisibleCards, targetCount);
        start = ClampVisibleStart(start, targetCount, visibleCards);

        if (targetIndex < start)
            return ClampVisibleStart(targetIndex, targetCount, visibleCards);

        if (targetIndex >= start + visibleCards)
            return ClampVisibleStart(targetIndex - visibleCards + 1, targetCount, visibleCards);

        return start;
    }

    #endregion

    #region Rebuild if player list changes
    private int shownPlayerListHash; // cached active Main.player list hash to detect joins/leaves
    private int shownNpcListHash; // cached active Main.npc list hash to detect joins/leaves

    private void RefreshTargetsIfNeeded()
    {
        int playerListHash = GetActivePlayerListHash();
        int npcListHash = GetActiveNpcListHash();

        if (playerListHash == shownPlayerListHash && npcListHash == shownNpcListHash)
            return;

        shownPlayerListHash = playerListHash;
        shownNpcListHash = npcListHash;

        RefreshTargets();
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

    private static int GetActiveNpcListHash()
    {
        HashCode hash = new();

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];

            if (npc is null || !npc.active)
                continue;

            hash.Add(i);
        }

        return hash.ToHashCode();
    }

    private static List<int> GetPlayerTargets()
    {
        return SpectatorTargetSystem.GetTargets(Main.myPlayer);
    }

    private static List<int> GetNpcTargets()
    {
        List<int> targets = [];

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (Main.npc[i]?.active == true)
                targets.Add(i);
        }

        return targets;
    }

    private static int GetPlayerTargetCount()
    {
        return SpectatorTargetSystem.GetTargets(Main.myPlayer).Count;
    }

    private static int GetNpcTargetCount()
    {
        int count = 0;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (Main.npc[i]?.active == true)
                count++;
        }

        return count;
    }
    #endregion

    #region Layout Helpers
    private static float GetScale() => 0.85f;
    private static float GetHeaderHeight() => HeaderHeight * GetScale();
    private static float GetTabHeight() => TabHeight * GetScale();
    private static float GetPlayerPanelPadding() => 10f * GetScale();
    private static float GetNavButtonWidth() => 24f * GetScale();
    private static float GetCardGap() => 6f * GetScale();
    private static float GetContentGap() => ContentGap * GetScale();
    private static float GetCardWidth() => UIPlayerCard.CardWidth * GetScale();
    private static float GetCardHeight() => UIPlayerCard.CardHeight * GetScale();
    private static float GetContentHeight()
    {
        return GetCardHeight() + GetPlayerPanelPadding() * 2f;
    }

    private static float GetStatusHeight()
    {
        return 44f * GetScale();
    }

    private static float GetPanelHeight()
    {
        return GetHeaderHeight() + GetTabHeight() + GetContentHeight() + GetContentGap() + GetStatusHeight();
    }

    private static float GetPanelWidth(int shownCards, float cardWidth, bool showButtons)
    {
        float cardsWidth = shownCards * cardWidth + Math.Max(0, shownCards - 1) * GetCardGap();
        float contentWidth = cardsWidth;

        if (showButtons)
        {
            contentWidth += GetNavButtonWidth() * 2f + 6 * GetScale() * 2f;
        }

        return contentWidth + GetPlayerPanelPadding() * 2f;
    }

    private void AddPrevButton(UIPanel playersPanel, float playerPanelPadding, float navButtonWidth, float cardHeight, Action onClick, string hoverText)
    {
        float scale = GetScale();
        float buttonHeight = 30f * scale;
        UIAutoScaleTextTextPanel<string> prevButton = new("<");
        prevButton.SetPadding(0f);
        prevButton.Top.Set(playerPanelPadding + cardHeight * 0.5f - buttonHeight * 0.5f, 0f);
        prevButton.Height.Set(buttonHeight, 0f);
        prevButton.Width.Set(navButtonWidth, 0f);
        prevButton.BackgroundColor = new Color(55, 48, 92) * 0.9f;
        prevButton.BorderColor = Color.Black;
        prevButton.OnLeftClick += (_, _) => onClick();
        prevButton.OnMouseOver += (_, _) => prevButton.BorderColor = Color.Yellow;
        prevButton.OnMouseOut += (_, _) => prevButton.BorderColor = Color.Black;

        playersPanel.Append(prevButton);
    }

    private void AddNextButton(UIPanel playersPanel, float playerPanelPadding, float navButtonWidth, float cardHeight, Action onClick, string hoverText)
    {
        float scale = GetScale();
        float buttonHeight = 30f * scale;

        UIAutoScaleTextTextPanel<string> nextButton = new(">");
        nextButton.SetPadding(0f);
        nextButton.HAlign = 1f;
        nextButton.Top.Set(playerPanelPadding + cardHeight * 0.5f - buttonHeight * 0.5f, 0f);
        nextButton.Height.Set(buttonHeight, 0f);
        nextButton.Width.Set(navButtonWidth, 0f);
        nextButton.BackgroundColor = new Color(55, 48, 92) * 0.9f;
        nextButton.BorderColor = Color.Black;
        nextButton.OnLeftClick += (_, _) => onClick();
        nextButton.OnMouseOver += (_, _) => nextButton.BorderColor = Color.Yellow;
        nextButton.OnMouseOut += (_, _) => nextButton.BorderColor = Color.Black;

        playersPanel.Append(nextButton);
    }
    #endregion

    private sealed class PlayersTab : ITab
    {
        public SpectatorTab Tab => SpectatorTab.Players;
        public string HeaderText => $"Players ({GetPlayerTargetCount()})";
        public string TooltipText => "Spectate players";
        public Asset<Texture2D> Icon => Ass.IconPlayer;
        public float IconScale => 1.2f;
        public Vector2 IconOffset => new(2, 0);
        public Vector2 TextOffset => new(-12, 0);
        public void Refresh()
        {
        }
    }

    private sealed class NPCsTab : ITab
    {
        public SpectatorTab Tab => SpectatorTab.NPCs;
        public string HeaderText => $"NPCs ({GetNpcTargetCount()})";
        public string TooltipText => "Spectate NPCs";
        public Asset<Texture2D> Icon => Ass.IconNPC;
        public float IconScale => 1f;
        public Vector2 IconOffset => new(-2, -4);
        public Vector2 TextOffset => new(-2, 0);
        public void Refresh()
        {
        }
    }

}
