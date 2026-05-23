using Microsoft.Xna.Framework.Input;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using Reese.Common.Replayer.ReplayHud.Shared.UI;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
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
    private const float ContentGap = 6f;
    private const float DetailStatusGap = 6f;
    private const float ScrollbarWidth = 18f;

    // Card display logic
    private const int MinCardsPerRow = 3;
    private static int CardsPerRow => SpectateHudClientSettings.PlayersPerRow;
    private static int MaxVisibleCards => CardsPerRow * SpectateHudClientSettings.RowsVisible;

    // Targeting
    private int locked = -1; // currently locked spectated player index, -1 means no locked target
    private int lockedNpc = -1; // currently locked spectated NPC index, -1 means no locked NPC target
    private int hovered = -1; // currently hovered spectated player index, -1 means no hovered target
    private float currentContentHeight;
    private float playerCardScale = GetPlayerCardScale();
    private int settingsRevision = SpectateHudClientSettings.Revision;
    
    // Content
    private readonly List<ITab> tabs = [];
    private ITab currentTab;
    private TabBar tabBar;
    private UIPanel headerPanel;
    private UIPanel contentPanel;
    private UIStatusPanel statusPanel;
    private UIGrid targetGrid;
    private UIElement detailPanel;
    private int detailPlayer = -2;
    private int detailNpc = -2;

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
        Height.Set(GetPanelHeight(GetGridContentHeight(0), false, SpectateHudClientSettings.ShowDescription), 0f);

        tabs.Add(new PlayersTab());
        tabs.Add(new NPCsTab());
        currentTab = tabs[0];

        Rebuild();
    }

    private void Rebuild()
    {
        //Log.Chat("Rebuilding SpectateHud...");

        RemoveAllChildren();
        targetGrid = null;
        detailPanel = null;
        statusPanel = null;
        detailPlayer = -2;
        detailNpc = -2;

        // Layout
        float scale = GetScale();
        float headerHeight = GetHeaderHeight();
        float tabHeight = GetTabHeight();
        float playerPanelPadding = GetPlayerPanelPadding();
        float contentHeight = currentContentHeight = GetActiveContentHeight();

        currentTab ??= tabs.Count > 0 ? tabs[0] : null;

        Height.Set(GetPanelHeight(contentHeight, ShouldShowDetail(), SpectateHudClientSettings.ShowDescription), 0f);

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

        RefreshTargets();
    }

    private void RefreshTargets()
    {
        if (contentPanel == null)
            return;

        //Log.Chat("Refreshing SpectateHud...");

        float playerPanelPadding = GetPlayerPanelPadding();

        List<int> playerTargets = GetPlayerTargets();
        List<int> npcTargets = GetNpcTargets();

        if (!playerTargets.Contains(locked))
            locked = -1;

        if (!playerTargets.Contains(hovered))
            hovered = -1;

        if (!npcTargets.Contains(lockedNpc))
            lockedNpc = -1;

        bool showingNpcs = currentTab?.Tab == SpectatorTab.NPCs;
        int activeCount = showingNpcs ? npcTargets.Count : playerTargets.Count;
        currentContentHeight = GetGridContentHeight(activeCount);

        bool showDetail = ShouldShowDetail();
        bool showDescription = SpectateHudClientSettings.ShowDescription;
        Width.Set(GetPanelWidthWithBottom(GetGridPanelWidth(activeCount), showDetail, showDescription), 0f);
        Height.Set(GetPanelHeight(currentContentHeight, showDetail, showDescription), 0f);
        contentPanel.Height.Set(currentContentHeight, 0f);
        contentPanel.SetPadding(playerPanelPadding);

        tabBar?.RefreshHeaders();
        BuildContent(playerTargets, npcTargets);

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
        panel.Append(new ClosePanel(
            () => ModContent.GetInstance<ReplayHudSystem>().CloseSpectateHud(),
            scale,
            normalColor
        ));

        return panel;
    }

    private void BuildContent(List<int> playerTargets, List<int> npcTargets)
    {
        if (contentPanel == null)
            return;

        contentPanel.RemoveAllChildren();
        targetGrid = null;

        float scale = GetScale();
        float cardScale = scale * playerCardScale;

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

            BuildEntityGrid(playerTargets.Count, i =>
            {
                int playerIndex = playerTargets[i];
                UIPlayerCard card = new(playerIndex, i, cardScale);
                card.OnLeftClick += (evt, _) =>
                {
                    if (evt.Target != card || !IsMouseInTargetGridViewport())
                        return;

                    SpectatorTargetSystem.TogglePlayerTarget(playerIndex);
                    if (TeammateHudOverlay.IsAnyOpen)
                        TeammateHudOverlay.Open(playerIndex);
                    UpdateTarget();
                    UpdateStatusText();
                };

                return card;
            });
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

        BuildEntityGrid(npcTargets.Count, i =>
        {
            int npcIndex = npcTargets[i];
            UINPCCard card = new(npcIndex, i, cardScale);
            card.OnLeftClick += (evt, _) =>
            {
                if (evt.Target != card || !IsMouseInTargetGridViewport())
                    return;

                SpectatorTargetSystem.ToggleNPCTarget(npcIndex);
                UpdateTarget();
                UpdateStatusText();
            };

            return card;
        });
    }

    private void BuildEntityGrid(int targetCount, Func<int, UIElement> buildCard)
    {
        float cardWidth = GetCardWidth();
        float cardHeight = GetCardHeight();
        float cardGap = GetCardGap();
        bool showScrollbar = targetCount > MaxVisibleCards;
        int columns = GetVisibleColumns(targetCount);
        int visibleRows = GetVisibleRows(targetCount);
        float gridWidth = GetGridWidth(columns);
        float gridHeight = GetGridHeight(visibleRows);
        float scrollbarLeft = gridWidth + cardGap + 2f;

        targetGrid = new UIGrid
        {
            ListPadding = cardGap,
            OverflowHidden = true,
            ManualSortMethod = static _ => { }
        };
        targetGrid.Width.Set(gridWidth, 0f);
        targetGrid.Height.Set(gridHeight, 0f);

        UIElement gridHost = new() { HAlign = 0.5f };
        gridHost.Width.Set(showScrollbar ? scrollbarLeft + GetScrollbarWidth() : gridWidth, 0f);
        gridHost.Height.Set(gridHeight, 0f);
        gridHost.Append(targetGrid);
        contentPanel.Append(gridHost);

        List<UIElement> items = [];

        for (int rowStart = 0; rowStart < targetCount; rowStart += CardsPerRow)
        {
            int rowCards = Math.Min(CardsPerRow, targetCount - rowStart);
            AddCenteredRowSpacer(items, columns, rowCards, cardWidth, cardHeight, cardGap);

            for (int i = 0; i < rowCards; i++)
            {
                UIElement card = buildCard(rowStart + i);
                card.Width.Set(cardWidth, 0f);
                card.Height.Set(cardHeight, 0f);
                items.Add(card);
            }
        }

        targetGrid.AddRange(items);

        if (!showScrollbar)
            return;

        UIScrollbar scrollbar = new();
        scrollbar.Width.Set(GetScrollbarWidth(), 0f);
        scrollbar.Height.Set(gridHeight, 0f);
        scrollbar.Left.Set(scrollbarLeft, 0f);
        gridHost.Append(scrollbar);
        targetGrid.SetScrollbar(scrollbar);
    }

    private static void AddCenteredRowSpacer(List<UIElement> items, int columns, int rowCards, float cardWidth, float cardHeight, float cardGap)
    {
        if (rowCards >= columns)
            return;

        float rowWidth = GetGridWidth(columns, cardWidth, cardGap);
        float cardsWidth = rowCards * cardWidth + Math.Max(0, rowCards - 1) * cardGap;
        float spacerWidth = (rowWidth - cardsWidth) * 0.5f - cardGap;

        if (spacerWidth <= 0f)
            return;

        UIElement spacer = new();
        spacer.IgnoresMouseInteraction = true;
        spacer.Width.Set(spacerWidth, 0f);
        spacer.Height.Set(cardHeight, 0f);
        items.Add(spacer);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        UpdatePlayerCardScale();
        RefreshSettingsIfNeeded();
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
        if (!ContainsPoint(Main.MouseScreen) || contentPanel == null || !IsMouseInTargetGridViewport())
            return -1;

        return GetHoveredSlot(contentPanel);
    }

    private bool IsMouseInTargetGridViewport()
    {
        return targetGrid?.GetDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint()) == true;
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
        UpdateBottomPanels();
        Recalculate();

        if (!SpectateHudClientSettings.ShowDescription || statusPanel == null)
            return;

        bool showGhost = hovered < 0 && locked < 0 && lockedNpc < 0 && Main.LocalPlayer?.ghost == true;
        statusPanel.SetStatus(GetStatusText(), showGhost);
    }

    private void UpdateBottomPanels()
    {
        bool showDetail = ShouldShowDetail();
        bool showDescription = SpectateHudClientSettings.ShowDescription;
        bool showBottom = showDetail || showDescription;
        float bottomTop = showBottom ? GetBottomTop(currentContentHeight) : 0f;
        float scale = GetScale();

        if (showDetail)
        {
            if (detailPanel == null || detailPlayer != locked || detailNpc != lockedNpc)
            {
                if (detailPanel?.Parent != null)
                    RemoveChild(detailPanel);

                detailPanel = locked >= 0
                    ? new UIPlayerDetailPanel(locked, scale)
                    : new UINPCDetailPanel(lockedNpc, scale);
                detailPlayer = locked;
                detailNpc = lockedNpc;
                Append(detailPanel);
            }

            detailPanel.Top.Set(bottomTop, 0f);
            detailPanel.Width.Set(GetDetailWidth(), 0f);
            detailPanel.Height.Set(GetDetailHeight(), 0f);
        }
        else
        {
            if (detailPanel?.Parent != null)
                RemoveChild(detailPanel);

            detailPanel = null;
            detailPlayer = -2;
            detailNpc = -2;
        }

        if (showDescription)
        {
            statusPanel ??= new UIStatusPanel(scale);

            if (statusPanel.Parent == null)
                Append(statusPanel);

            float statusLeft = showDetail ? GetDetailWidth() + GetDetailStatusGap() : 0f;
            statusPanel.Left.Set(statusLeft, 0f);
            statusPanel.Top.Set(bottomTop, 0f);
            statusPanel.Width.Set(-statusLeft, 1f);
            statusPanel.Height.Set(GetStatusHeight(), 0f);
        }
        else
        {
            if (statusPanel?.Parent != null)
                RemoveChild(statusPanel);

            statusPanel = null;
        }

        Width.Set(GetPanelWidthWithBottom(GetActivePanelWidth(), showDetail, showDescription), 0f);
        Height.Set(GetPanelHeight(currentContentHeight, showDetail, showDescription), 0f);
    }

    private bool IsShowingDetail()
    {
        return locked >= 0 && locked < Main.maxPlayers && Main.player[locked]?.active == true ||
            lockedNpc >= 0 && lockedNpc < Main.maxNPCs && Main.npc[lockedNpc]?.active == true;
    }

    private bool ShouldShowDetail()
    {
        return SpectateHudClientSettings.ShowPlayerDetails && IsShowingDetail();
    }

    private void UpdatePlayerCardScale()
    {
        float nextScale = GetPlayerCardScale();

        if (Math.Abs(playerCardScale - nextScale) < 0.001f)
            return;

        playerCardScale = nextScale;
        RefreshTargets();
    }

    private void RefreshSettingsIfNeeded()
    {
        if (settingsRevision == SpectateHudClientSettings.Revision)
            return;

        settingsRevision = SpectateHudClientSettings.Revision;
        RefreshTargets();
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
        ScrollToPlayer(playerIndex);
        UpdateStatusText();
    }

    private void ScrollToPlayer(int playerIndex)
    {
        targetGrid?.Goto(element => element is UIPlayerCard card && card.PlayerIndex == playerIndex, center: true);
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
        ScrollToNpc(npcIndex);
        UpdateStatusText();
    }

    private void ScrollToNpc(int npcIndex)
    {
        targetGrid?.Goto(element => element is UINPCCard card && card.NPCIndex == npcIndex, center: true);
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
    private static float GetPlayerCardScale() => 0.8f;
    private static float GetHeaderHeight() => HeaderHeight * GetScale();
    private static float GetTabHeight() => TabHeight * GetScale();
    private static float GetPlayerPanelPadding() => 10f * GetScale();
    private static float GetCardGap() => 6f * GetScale();
    private static float GetContentGap() => ContentGap * GetScale();
    private static float GetDetailStatusGap() => DetailStatusGap * GetScale();
    private static float GetScrollbarWidth() => 20;
    private static float GetCardScale() => GetScale() * GetPlayerCardScale();
    private static float GetCardWidth() => UIPlayerCard.CardWidth * GetCardScale();
    private static float GetCardHeight()
    {
        float scale = GetCardScale();
        float height = 12f * scale;

        if (SpectateHudClientSettings.ShowPlayer)
            height += 90f * scale;

        if (SpectateHudClientSettings.ShowPlayerName)
            height += 24f * scale;

        if (SpectateHudClientSettings.ShowPlayerDistance)
            height += 22f * scale;

        if (SpectateHudClientSettings.ShowPlayer && (SpectateHudClientSettings.ShowPlayerName || SpectateHudClientSettings.ShowPlayerDistance))
            height += scale;

        return Math.Max(34f * scale, height);
    }
    private static float GetDetailWidth() => UIPlayerCard.DetailWidth * GetScale();
    private static float GetDetailHeight() => UIPlayerCard.DetailHeight * GetScale();
    private static int GetVisibleColumns(int count) => Math.Max(MinCardsPerRow, Math.Min(CardsPerRow, count));
    private static int GetVisibleRows(int count) => Math.Max(1, Math.Min(SpectateHudClientSettings.RowsVisible, (count + CardsPerRow - 1) / CardsPerRow));
    private static float GetGridWidth(int columns) => GetGridWidth(columns, GetCardWidth(), GetCardGap());
    private static float GetGridWidth(int columns, float cardWidth, float cardGap) => columns * cardWidth + Math.Max(0, columns - 1) * cardGap;
    private static float GetGridHeight(int rows) => rows * GetCardHeight() + Math.Max(0, rows - 1) * GetCardGap();
    private static float GetGridContentHeight(int count) => GetGridHeight(GetVisibleRows(count)) + GetPlayerPanelPadding() * 2f;

    private static float GetStatusHeight()
    {
        return 44f * GetScale();
    }

    private float GetActiveContentHeight()
    {
        return GetGridContentHeight(currentTab?.Tab == SpectatorTab.NPCs ? GetNpcTargetCount() : GetPlayerTargetCount());
    }

    private static float GetMinimumStatusWidth() => 260f * GetScale();

    private float GetActivePanelWidth()
    {
        return GetGridPanelWidth(currentTab?.Tab == SpectatorTab.NPCs ? GetNpcTargetCount() : GetPlayerTargetCount());
    }

    private static float GetPanelWidthWithBottom(float width, bool showDetail, bool showDescription)
    {
        if (showDetail && showDescription)
            return Math.Max(width, GetDetailWidth() + GetDetailStatusGap() + GetMinimumStatusWidth());

        return showDetail ? Math.Max(width, GetDetailWidth()) : width;
    }

    private static float GetBottomTop(float contentHeight)
    {
        return GetHeaderHeight() + GetTabHeight() + contentHeight + GetContentGap();
    }

    private static float GetBottomHeight(bool showDetail, bool showDescription)
    {
        if (showDetail && showDescription)
            return Math.Max(GetStatusHeight(), GetDetailHeight());

        if (showDetail)
            return GetDetailHeight();

        return showDescription ? GetStatusHeight() : 0f;
    }

    private static float GetPanelHeight(float contentHeight, bool showDetail, bool showDescription)
    {
        float bottomHeight = GetBottomHeight(showDetail, showDescription);
        float height = GetHeaderHeight() + GetTabHeight() + contentHeight;
        return bottomHeight > 0f ? height + GetContentGap() + bottomHeight : height;
    }

    private static float GetGridPanelWidth(int count)
    {
        float contentWidth = GetGridWidth(GetVisibleColumns(count));

        if (count > MaxVisibleCards)
            contentWidth += GetCardGap() + GetScrollbarWidth();

        return contentWidth + GetPlayerPanelPadding() * 2f;
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
