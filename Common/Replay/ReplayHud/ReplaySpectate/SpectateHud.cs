using Microsoft.Xna.Framework.Input;
using Reese.Common.Replay.ReplayHud.ReplaySpectate.TeammateOverlay;
using Reese.Common.Replay.ReplayHud.Shared.Tabs;
using Reese.Common.Replay.ReplayHud.Shared.UI;
using Reese.Common.Spectator;
using Reese.Core.Compat;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace Reese.Common.Replay.ReplayHud.ReplaySpectate;

/// <summary>
/// Main spectator HUD in bottom center of the screen.
/// Displays a horizontal list of spectatable players and allows the user to hover and lock onto a target.
/// </summary>
internal sealed class SpectateHud : UIElement
{
    // Layout
    private const float TabHeight = 36f;

    // Card display logic
    private const int MinCardsPerRow = 3;
    private const int MaxCardsPerRow = 5;
    private const int MaxDetailedCardsPerRow = 4;
    private const int MaxHeadCardsPerRow = 6;
    private const uint NpcTargetListRefreshIntervalTicks = 60 * 15;

    // Targeting
    private int locked = -1; // currently locked spectated player index, -1 means no locked target
    private int lockedNpc = -1; // currently locked spectated NPC index, -1 means no locked NPC target
    private int hovered = -1; // currently hovered spectated player index, -1 means no hovered target
    private float currentContentHeight;
    private float playerCardScale = GetPlayerCardScale();
    private int settingsRevision = SpectateHudClientSettings.Revision;
    private int playerTargetsSignature = -1;
    private uint lastNpcTargetListRefreshUpdate;

    // Content
    private readonly List<int> playerTargets = [];
    private readonly List<int> npcTargets = [];
    private readonly List<ITab> tabs = [];
    private ITab currentTab;
    private TabBar tabBar;
    private UIPanel backgroundPanel;
    private UIPanel contentPanel;

    // Save scroll position
    private UIGrid targetGrid;
    private UIScrollbar targetScrollbar;
    private float? pendingScrollRestore;
    private float savedPlayerScrollPosition;
    private float savedNpcScrollPosition;

    // Reflection
    private static readonly FieldInfo elementsField = typeof(UIElement).GetField("Elements", BindingFlags.Instance | BindingFlags.NonPublic);

    public SpectateHud()
    {
        HAlign = 0.5f;
        VAlign = 0f;
        Left.Set(0, 0f);
        bool pvpaOrCtgLoaded = PvPAdventureCompat.IsPvPAdventureLoaded || CTGCompat.IsCTGLoaded;
        Top.Set(GetTopOffset(), 0f);
        Width.Set(ReplayInfo.InfoHud.PanelWidth, 0f);
        Height.Set(GetPanelHeight(GetGridContentHeight(0)), 0f);

        tabs.Add(new PlayersTab(() => playerTargets.Count));
        tabs.Add(new NPCsTab(() => npcTargets.Count));
        currentTab = tabs[0];

        Rebuild();
    }

    // Move down to make room for CTG / PvPAdventure scoreboards
    private int GetTopOffset() => true switch
    {
        _ when PvPAdventureCompat.IsPvPAdventureLoaded => 40,
        _ when CTGCompat.IsCTGLoaded => 65,
        _ => 6 // Default fallback if no UI-altering mods are loaded
    };

    private void Rebuild()
    {
        //Log.Chat("Rebuilding SpectateHud...");

        RemoveAllChildren();
        targetGrid = null;
        backgroundPanel = null;

        currentTab ??= tabs.Count > 0 ? tabs[0] : null;
        RefreshPlayerTargetCache();
        RefreshNpcTargetCache();

        // Layout
        float tabHeight = GetTabHeight();
        float playerPanelPadding = GetPlayerPanelPadding();
        float contentHeight = currentContentHeight = GetActiveContentHeight();

        Height.Set(GetPanelHeight(contentHeight), 0f);

        backgroundPanel = new UIPanel
        {
            Width = new StyleDimension(0f, 1f),
            Height = new StyleDimension(0f, 1f),
            BackgroundColor = new Color(12, 18, 42) * 0.96f,
            BorderColor = Color.Black,
            IgnoresMouseInteraction = true
        };
        backgroundPanel.SetPadding(0f);
        Append(backgroundPanel);

        // Tabs
        tabBar = new TabBar();
        tabBar.Top.Set(0f, 0f);
        tabBar.Width.Set(0f, 1f);
        tabBar.Height.Set(tabHeight, 0f);
        tabBar.BuildTabs(tabs, () => currentTab, ShowTab, 1f);
        Append(tabBar);

        // Content
        contentPanel = new UIPanel();
        contentPanel.SetPadding(playerPanelPadding);
        contentPanel.Top.Set(tabHeight, 0f);
        contentPanel.Width.Set(0f, 1f);
        contentPanel.Height.Set(contentHeight, 0f);
        contentPanel.BackgroundColor = Color.Transparent;
        contentPanel.BorderColor = Color.Transparent;
        Append(contentPanel);

        RebuildTargetsFromCache();
    }

    private void RefreshTargets(bool refreshPlayers = true, bool refreshNpcs = true)
    {
        if (contentPanel == null)
            return;

        if (refreshPlayers)
            RefreshPlayerTargetCache();

        if (refreshNpcs)
            RefreshNpcTargetCache();

        RebuildTargetsFromCache();
    }

    private void RebuildTargetsFromCache()
    {
        if (contentPanel == null)
            return;

        if (targetScrollbar != null)
        {
            if (currentTab?.Tab == SpectatorTab.NPCs)
                savedNpcScrollPosition = targetScrollbar.ViewPosition;
            else
                savedPlayerScrollPosition = targetScrollbar.ViewPosition;
        }

        targetScrollbar = null;

        float playerPanelPadding = GetPlayerPanelPadding();

        if (!playerTargets.Contains(locked))
            locked = -1;

        if (!playerTargets.Contains(hovered))
            hovered = -1;

        if (!npcTargets.Contains(lockedNpc))
            lockedNpc = -1;

        int slotCount = GetActiveSlotCount();
        currentContentHeight = GetGridContentHeight(slotCount);

        Width.Set(GetGridPanelWidth(slotCount), 0f);
        Height.Set(GetPanelHeight(currentContentHeight), 0f);
        contentPanel.Height.Set(currentContentHeight, 0f);
        contentPanel.SetPadding(playerPanelPadding);


        tabBar?.RefreshHeaders();
        BuildContent();
    }

    private void ShowTab(SpectatorTab tab)
    {
        ITab nextTab = GetTab(tab);

        if (nextTab == null)
            return;

        if (currentTab == nextTab)
        {
            RefreshTargets(
                refreshPlayers: nextTab.Tab == SpectatorTab.Players,
                refreshNpcs: nextTab.Tab == SpectatorTab.NPCs);
            return;
        }

        currentTab = nextTab;
        RefreshTargets(
            refreshPlayers: nextTab.Tab == SpectatorTab.Players,
            refreshNpcs: nextTab.Tab == SpectatorTab.NPCs);
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

    private void BuildContent()
    {
        if (contentPanel == null)
            return;

        contentPanel.RemoveAllChildren();
        targetGrid = null;

        float cardScale = GetScale() * playerCardScale;

        switch (currentTab?.Tab)
        {
            case SpectatorTab.Players:
                BuildTargetContent(
                    playerTargets,
                    Loc.Get("ReplayHud.Spectate.NoPlayersAvailable"),
                    (playerIndex, listIndex) => new UIPlayerCard(playerIndex, listIndex, GetEntityCardScale(cardScale)),
                    TogglePlayerTarget);
                return;

            case SpectatorTab.NPCs:
                BuildTargetContent(
                    npcTargets,
                    Loc.Get("ReplayHud.Spectate.NoNpcsAvailable"),
                    (npcIndex, listIndex) => new UINPCCard(npcIndex, listIndex, GetEntityCardScale(cardScale)),
                    ToggleNpcTarget);
                return;

            default:
                System.Diagnostics.Debug.Fail($"Unexpected spectate tab: {currentTab?.Tab}");
                return;
        }
    }

    private void BuildTargetContent(List<int> targets, string emptyText, Func<int, int, UIElement> buildItem, Action<int> toggleTarget)
    {
        if (targets.Count == 0)
        {
            contentPanel.Append(new UIText(emptyText, 0.9f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                TextColor = Color.LightGray
            });

            return;
        }

        BuildEntityGrid(targets.Count, i =>
        {
            int target = targets[i];
            UIElement item = buildItem(target, i);

            item.OnLeftClick += (evt, _) =>
            {
                if (evt.Target != item || !IsMouseInTargetGridViewport())
                    return;

                toggleTarget(target);
                UpdateTarget();
            };

            return item;
        }, GetEntityItemSpan, GetEntityItemWidth);
    }

    private void TogglePlayerTarget(int playerIndex)
    {
        SpectatorTargetSystem.TogglePlayerTarget(playerIndex);

        if (TeammateHudOverlay.IsAnyOpen)
            TeammateHudOverlay.Open(playerIndex);
    }

    private static void ToggleNpcTarget(int npcIndex)
    {
        SpectatorTargetSystem.ToggleNPCTarget(npcIndex);
    }

    private static float GetEntityCardScale(float cardScale)
    {
        return SpectateHudClientSettings.EntityHudMode == EntityHudMode.Detailed ? GetInlineDetailScale() : cardScale;
    }

    private static int GetEntityItemSpan(int _)
    {
        return 1;
    }

    private static float GetEntityItemWidth(int _)
    {
        return GetCardWidth();
    }

    private void BuildEntityGrid(int targetCount, Func<int, UIElement> buildItem, Func<int, int> getSpan = null, Func<int, float> getWidth = null)
    {
        getSpan ??= _ => 1;
        getWidth ??= _ => GetCardWidth();
        float cardHeight = GetCardHeight();
        float cardGap = GetCardGap();
        int slotCount = GetSlotCount(targetCount, getSpan);
        int columns = GetVisibleColumns(slotCount);
        bool showScrollbar = slotCount > GetMaxVisibleCards(columns);
        int visibleRows = GetVisibleRows(slotCount, columns);
        float gridWidth = GetGridWidth();
        float gridHeight = GetGridHeight(visibleRows);
        float scrollbarLeft = gridWidth + cardGap + 2f;

        //Log.Chat($"BuildEntityGrid update={Main.GameUpdateCount} targetCount={targetCount} columns={columns} rows={visibleRows} showScrollbar={showScrollbar} gridHeight={gridHeight:0.##}");

        targetGrid = new UIGrid
        {
            ListPadding = cardGap,
            OverflowHidden = true,
            ManualSortMethod = static _ => { }
        };
        targetGrid.Width.Set(gridWidth, 0f);
        targetGrid.Height.Set(gridHeight, 0f);

        UIElement gridHost = new() { HAlign = 0.5f };
        gridHost.Width.Set(GetGridHostWidth(), 0f);
        gridHost.Height.Set(gridHeight, 0f);
        gridHost.Append(targetGrid);
        contentPanel.Append(gridHost);

        List<UIElement> rows = [];
        List<(UIElement Element, int Span, float Width)> rowItems = [];
        int rowSlots = 0;

        void FinishRow()
        {
            if (rowItems.Count == 0)
                return;

            float itemsWidth = Math.Max(0, rowItems.Count - 1) * cardGap;

            for (int i = 0; i < rowItems.Count; i++)
                itemsWidth += rowItems[i].Width;

            float left = (gridWidth - itemsWidth) * 0.5f;
            UIElement row = new();
            row.Width.Set(gridWidth, 0f);
            row.Height.Set(cardHeight, 0f);

            for (int i = 0; i < rowItems.Count; i++)
            {
                UIElement item = rowItems[i].Element;
                item.Left.Set(left, 0f);
                item.Width.Set(rowItems[i].Width, 0f);
                item.Height.Set(cardHeight, 0f);
                row.Append(item);
                left += rowItems[i].Width + cardGap;
            }

            rows.Add(row);
            rowItems.Clear();
            rowSlots = 0;
        }

        for (int i = 0; i < targetCount; i++)
        {
            int span = Math.Clamp(getSpan(i), 1, columns);
            float width = getWidth(i);

            if (span > 1 && rowSlots == columns - 1 && rowItems.Count > 0 && rowItems[^1].Span == 1)
            {
                (UIElement Element, int Span, float Width) moved = rowItems[^1];
                rowItems.RemoveAt(rowItems.Count - 1);
                rowSlots -= moved.Span;
                rowItems.Add((buildItem(i), span, width));
                rowSlots += span;
                FinishRow();
                rowItems.Add(moved);
                rowSlots = moved.Span;
                continue;
            }

            if (rowSlots + span > columns)
                FinishRow();

            rowItems.Add((buildItem(i), span, width));
            rowSlots += span;
        }

        FinishRow();
        targetGrid.AddRange(rows);

        if (!showScrollbar)
        {
            targetScrollbar = null;
            pendingScrollRestore = null;
            return;
        }

        UIScrollbar scrollbar = new();
        scrollbar.Width.Set(GetScrollbarWidth(), 0f);
        scrollbar.Height.Set(gridHeight, 0f);
        scrollbar.Left.Set(scrollbarLeft, 0f);
        gridHost.Append(scrollbar);
        targetGrid.SetScrollbar(scrollbar);
        targetScrollbar = scrollbar;
        pendingScrollRestore = currentTab?.Tab == SpectatorTab.NPCs
            ? savedNpcScrollPosition
            : savedPlayerScrollPosition;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (pendingScrollRestore.HasValue && targetScrollbar != null)
        {
            targetScrollbar.ViewPosition = pendingScrollRestore.Value;
            pendingScrollRestore = null;
        }

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

        // Keep the locked target in sync every frame in case the world state changes externally.
        UpdateTarget();
    }

    public void UpdateTarget()
    {
        Player target = SpectatorTargetSystem.GetLockedPlayerTarget();
        locked = target?.active == true ? target.whoAmI : -1;

        NPC npcTarget = SpectatorTargetSystem.GetLockedNPCTarget();
        lockedNpc = npcTarget?.active == true ? npcTarget.whoAmI : -1;
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
        if (element is UIPlayerCard card && card.ContainsPoint(Main.MouseScreen))
            return card.PlayerIndex;

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
    }

    private void EndHover()
    {
        hovered = -1;
        SpectatorTargetSystem.ClearPreviewTarget();
    }

    private string GetStatusText()
    {
        if (hovered >= 0 && Main.player[hovered]?.active == true)
        {
            if (locked == hovered)
                return Loc.Get("ReplayHud.Spectate.StopSpectatingPlayer", Main.player[hovered].name);

            return Loc.Get("ReplayHud.Spectate.SpectatePlayer", Main.player[hovered].name);
        }

        if (locked >= 0 && Main.player[locked]?.active == true)
            return Loc.Get("ReplayHud.Spectate.SpectatingPlayer", Main.player[locked].name);

        if (lockedNpc >= 0 && Main.npc[lockedNpc]?.active == true)
            return Loc.Get("ReplayHud.Spectate.SpectatingNpc", Main.npc[lockedNpc].FullName);

        if (SpectatorMode.IsLocalGhost)
            return Loc.Get("ReplayHud.Spectate.GhostModeEnabled");

        return Loc.Get("ReplayHud.Spectate.NotSpectatingAnyone");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (!SpectateHudClientSettings.ShowDescription)
            return;

        DrawStatusText(spriteBatch);
    }

    private void DrawStatusText(SpriteBatch spriteBatch)
    {
        CalculatedStyle dimensions = GetDimensions();
        string text = GetStatusText();
        float scale = FitStatusText(text, 0.45f * GetScale(), dimensions.Width);
        Vector2 size = FontAssets.DeathText.Value.MeasureString(text) * scale;
        Vector2 position = new(dimensions.X + (dimensions.Width - size.X) * 0.5f, dimensions.Y + dimensions.Height + 3f * GetScale());
        Utils.DrawBorderStringBig(spriteBatch, text, position, Color.White, scale);
    }

    private static float FitStatusText(string text, float scale, float width)
    {
        if (string.IsNullOrEmpty(text))
            return scale;

        return Math.Min(scale, (width - 12f * GetScale()) / FontAssets.DeathText.Value.MeasureString(text).X);
    }

    private void UpdatePlayerCardScale()
    {
        float nextScale = GetPlayerCardScale();

        if (Math.Abs(playerCardScale - nextScale) < 0.001f)
            return;

        playerCardScale = nextScale;
        RefreshTargets(refreshPlayers: currentTab?.Tab == SpectatorTab.Players, refreshNpcs: false);
    }

    private void RefreshSettingsIfNeeded()
    {
        if (settingsRevision == SpectateHudClientSettings.Revision)
            return;

        //Log.Chat($"RefreshSettingsIfNeeded update={Main.GameUpdateCount} revision {settingsRevision} -> {SpectateHudClientSettings.Revision}");
        settingsRevision = SpectateHudClientSettings.Revision;
        RefreshTargets(refreshPlayers: true, refreshNpcs: false);
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
        if (playerTargets.Count == 0)
        {
            SpectatorTargetSystem.ClearTarget();
            UpdateTarget();
            return;
        }

        int currentIndex = playerTargets.IndexOf(locked);
        int nextIndex = currentIndex < 0 ? direction < 0 ? playerTargets.Count - 1 : 0 : currentIndex + direction;

        if (nextIndex < 0)
            nextIndex = playerTargets.Count - 1;

        if (nextIndex >= playerTargets.Count)
            nextIndex = 0;

        int playerIndex = playerTargets[nextIndex];

        SpectatorTargetSystem.SetPlayerTarget(playerIndex);
        locked = playerIndex;
        lockedNpc = -1;
        ScrollToPlayer(playerIndex);
    }

    private void ScrollToPlayer(int playerIndex)
    {
        //NRE?
        targetGrid?.Goto(element => ContainsPlayerCard(element, playerIndex), center: true);

        pendingScrollRestore = null;
    }

    private void NavigateNpcTarget(int direction)
    {
        if (npcTargets.Count == 0)
        {
            SpectatorTargetSystem.ClearTarget();
            UpdateTarget();
            return;
        }

        int currentIndex = npcTargets.IndexOf(lockedNpc);
        int nextIndex = currentIndex < 0 ? direction < 0 ? npcTargets.Count - 1 : 0 : currentIndex + direction;

        if (nextIndex < 0)
            nextIndex = npcTargets.Count - 1;

        if (nextIndex >= npcTargets.Count)
            nextIndex = 0;

        int npcIndex = npcTargets[nextIndex];

        SpectatorTargetSystem.SetNPCTarget(npcIndex);
        lockedNpc = npcIndex;
        locked = -1;
        ScrollToNpc(npcIndex);
    }

    private void ScrollToNpc(int npcIndex)
    {
        targetGrid?.Goto(element => ContainsNpcCard(element, npcIndex), center: true);

        pendingScrollRestore = null;
    }

    private static bool ContainsPlayerCard(UIElement element, int playerIndex)
    {
        if (element is UIPlayerCard card && card.PlayerIndex == playerIndex)
            return true;

        if (elementsField?.GetValue(element) is not List<UIElement> children)
            return false;

        for (int i = 0; i < children.Count; i++)
        {
            if (ContainsPlayerCard(children[i], playerIndex))
                return true;
        }

        return false;
    }

    private static bool ContainsNpcCard(UIElement element, int npcIndex)
    {
        if (element is UINPCCard card && card.NPCIndex == npcIndex)
            return true;

        if (elementsField?.GetValue(element) is not List<UIElement> children)
            return false;

        for (int i = 0; i < children.Count; i++)
        {
            if (ContainsNpcCard(children[i], npcIndex))
                return true;
        }

        return false;
    }

    #endregion

    #region Target list refresh
    private void RefreshTargetsIfNeeded()
    {
        RefreshPlayerTargetsIfNeeded();
        RefreshNpcTargetsIfNeeded();
    }

    private void RefreshPlayerTargetsIfNeeded()
    {
        if (!RefreshPlayerTargetCacheIfChanged())
            return;

        if (currentTab?.Tab == SpectatorTab.Players)
            RebuildTargetsFromCache();
        else
        {
            SyncTargetSelectionWithCache();
            tabBar?.RefreshHeaders();
        }
    }

    private void RefreshNpcTargetsIfNeeded()
    {
        uint elapsed = Main.GameUpdateCount - lastNpcTargetListRefreshUpdate;

        if (elapsed < NpcTargetListRefreshIntervalTicks)
            return;

        RefreshNpcTargetCache();

        if (currentTab?.Tab == SpectatorTab.NPCs)
            RebuildTargetsFromCache();
        else
        {
            SyncTargetSelectionWithCache();
            tabBar?.RefreshHeaders();
        }
    }

    private bool RefreshPlayerTargetCacheIfChanged()
    {
        List<int> nextTargets = SpectatorTargetSystem.GetTargets(Main.myPlayer);
        int nextSignature = GetTargetSignature(nextTargets);

        if (nextSignature == playerTargetsSignature)
            return false;

        SetPlayerTargetCache(nextTargets, nextSignature);
        return true;
    }

    private void RefreshPlayerTargetCache()
    {
        List<int> nextTargets = SpectatorTargetSystem.GetTargets(Main.myPlayer);
        SetPlayerTargetCache(nextTargets, GetTargetSignature(nextTargets));
    }

    private void SetPlayerTargetCache(List<int> nextTargets, int nextSignature)
    {
        SortPlayerTargets(nextTargets);

        playerTargets.Clear();
        playerTargets.AddRange(nextTargets);
        playerTargetsSignature = nextSignature;
    }

    private void RefreshNpcTargetCache()
    {
        npcTargets.Clear();
        npcTargets.AddRange(GetNpcTargets());
        lastNpcTargetListRefreshUpdate = Main.GameUpdateCount;
    }

    private void SyncTargetSelectionWithCache()
    {
        if (!playerTargets.Contains(locked))
            locked = -1;

        if (!playerTargets.Contains(hovered))
            hovered = -1;

        if (!npcTargets.Contains(lockedNpc))
            lockedNpc = -1;
    }

    private static int GetTargetSignature(List<int> targets)
    {
        unchecked
        {
            int signature = targets.Count;

            for (int i = 0; i < targets.Count; i++)
                signature = signature * 397 ^ targets[i];

            return signature;
        }
    }

    private static void SortPlayerTargets(List<int> targets)
    {
        if (targets.Count <= 1)
            return;

        targets.Sort(SpectateHudClientSettings.SortMode switch
        {
            SpectateHudSortMode.Teams => ComparePlayerTargetsByTeam,
            SpectateHudSortMode.Id => ComparePlayerTargetsById,
            SpectateHudSortMode.Alphabetical => ComparePlayerTargetsByName,
            SpectateHudSortMode.Distance => ComparePlayerTargetsByDistance,
            SpectateHudSortMode.Health => ComparePlayerTargetsByHealth,
            _ => ComparePlayerTargetsByTeam
        });
    }

    private static int ComparePlayerTargetsByTeam(int left, int right)
    {
        int result = GetTeamSortKey(left).CompareTo(GetTeamSortKey(right));
        return result != 0 ? result : ComparePlayerTargetsById(left, right);
    }

    private static int ComparePlayerTargetsById(int left, int right)
    {
        return left.CompareTo(right);
    }

    private static int ComparePlayerTargetsByName(int left, int right)
    {
        string leftName = Main.player[left]?.name ?? string.Empty;
        string rightName = Main.player[right]?.name ?? string.Empty;
        int result = string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
        return result != 0 ? result : ComparePlayerTargetsById(left, right);
    }

    private static int ComparePlayerTargetsByDistance(int left, int right)
    {
        int result = GetDistanceSortKey(left).CompareTo(GetDistanceSortKey(right));
        return result != 0 ? result : ComparePlayerTargetsById(left, right);
    }

    private static int ComparePlayerTargetsByHealth(int left, int right)
    {
        int result = GetHealthSortKey(left).CompareTo(GetHealthSortKey(right));
        return result != 0 ? result : ComparePlayerTargetsById(left, right);
    }

    private static int GetTeamSortKey(int playerIndex)
    {
        Player player = Main.player[playerIndex];
        int lastTeamIndex = Math.Max(0, Main.teamColor.Length - 1);
        return player?.active == true ? Math.Clamp(player.team, 0, lastTeamIndex) : 0;
    }

    private static float GetDistanceSortKey(int playerIndex)
    {
        Player local = Main.LocalPlayer;
        Player player = Main.player[playerIndex];

        if (local?.active != true || player?.active != true)
            return float.MaxValue;

        return Vector2.DistanceSquared(local.Center, player.Center);
    }

    private static int GetHealthSortKey(int playerIndex)
    {
        Player player = Main.player[playerIndex];
        return player?.active == true ? Math.Max(0, player.statLife) : int.MaxValue;
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

    private int GetActiveSlotCount()
    {
        int count = currentTab?.Tab == SpectatorTab.NPCs ? npcTargets.Count : playerTargets.Count;
        return GetSlotCount(count, GetEntityItemSpan);
    }
    #endregion

    #region Layout Helpers
    private static float GetScale() => 0.75f;
    private static float GetPlayerCardScale() => 0.8f;
    private static float GetTabHeight() => TabHeight;
    private static float GetPlayerPanelPadding() => 10f * GetScale();
    private static float GetCardGap() => 6f * GetScale();
    private static float GetScrollbarWidth() => 20f;
    private static float GetCardScale() => GetScale() * GetPlayerCardScale();

    private static float GetCardWidth()
    {
        return SpectateHudClientSettings.EntityHudMode switch
        {
            EntityHudMode.Detailed => GetDetailedCardWidth(),
            EntityHudMode.Head => GetHeadCardWidth(),
            _ => GetRegularCardWidth()
        };
    }

    private static float GetCardHeight()
    {
        return SpectateHudClientSettings.EntityHudMode == EntityHudMode.Head ? GetRegularCardHeight() / 2f : GetRegularCardHeight();
    }

    private static int GetSlotCount(int count, Func<int, int> getSpan)
    {
        int slots = 0;

        for (int i = 0; i < count; i++)
            slots += Math.Max(1, getSpan(i));

        return slots;
    }

    private static int GetVisibleColumns(int count)
    {
        if (count <= 0)
            return MinCardsPerRow;

        int max = SpectateHudClientSettings.EntityHudMode switch
        {
            EntityHudMode.Detailed => MaxDetailedCardsPerRow,
            EntityHudMode.Head => MaxHeadCardsPerRow,
            _ => MaxCardsPerRow
        };

        int min = Math.Min(count, MinCardsPerRow);
        return Math.Clamp(count, min, max);
    }
    private static int GetMaxVisibleCards(int columns) => columns * GetMaxVisibleRows();
    private static int GetVisibleRows(int count) => GetVisibleRows(count, GetVisibleColumns(count));
    private static int GetVisibleRows(int count, int columns) => Math.Max(1, Math.Min(GetMaxVisibleRows(), (count + columns - 1) / columns));
    private static float GetGridWidth() => GetRegularGridWidth();
    private static float GetGridHostWidth() => GetRegularGridWidth() + GetCardGap() + 2f + GetScrollbarWidth();
    private static float GetRegularGridWidth() => GetGridWidth(MaxCardsPerRow, GetRegularCardWidth(), GetCardGap());
    private static float GetRegularCardWidth() => UIEntityCard<Player>.CardWidth * GetCardScale();
    private static float GetRegularCardHeight() => UIEntityCard<Player>.DetailHeight * GetCardScale();
    private static float GetDetailedCardWidth() => (GetRegularGridWidth() - (MaxDetailedCardsPerRow - 1) * GetCardGap()) / MaxDetailedCardsPerRow;
    private static float GetHeadCardWidth() => (GetRegularGridWidth() - (MaxHeadCardsPerRow - 1) * GetCardGap()) / MaxHeadCardsPerRow;
    private static float GetInlineDetailScale() => GetCardScale();
    private static int GetMaxVisibleRows() => SpectateHudClientSettings.EffectiveRowsVisible;
    private static float GetGridWidth(int columns, float cardWidth, float cardGap) => columns * cardWidth + Math.Max(0, columns - 1) * cardGap;
    private static float GetGridHeight(int rows) => rows * GetCardHeight() + Math.Max(0, rows - 1) * GetCardGap();
    private static float GetGridContentHeight(int count) => GetGridHeight(GetVisibleRows(count)) + GetPlayerPanelPadding() * 2f;

    private float GetActiveContentHeight()
    {
        return GetGridContentHeight(GetActiveSlotCount());
    }

    private static float GetPanelHeight(float contentHeight)
    {
        return GetTabHeight() + contentHeight;
    }

    private static float GetGridPanelWidth(int _)
    {
        return GetGridHostWidth() + GetPlayerPanelPadding() * 2f;
    }
    #endregion

    private sealed class PlayersTab : ITab
    {
        private readonly Func<int> getTargetCount;

        public PlayersTab(Func<int> getTargetCount)
        {
            this.getTargetCount = getTargetCount;
        }

        public SpectatorTab Tab => SpectatorTab.Players;
        public string HeaderText => Loc.Get("ReplayHud.Spectate.PlayersTab", getTargetCount());
        public string TooltipText => Loc.Get("ReplayHud.Spectate.PlayersTooltip");
        public Asset<Texture2D> Icon => Ass.IconPlayer;
        public float IconScale => 1.2f;
        public Vector2 IconOffset => new(2, 2);
        public Vector2 TextOffset => new(-12, 0);
        public void Refresh()
        {
        }
    }

    private sealed class NPCsTab : ITab
    {
        private readonly Func<int> getTargetCount;

        public NPCsTab(Func<int> getTargetCount)
        {
            this.getTargetCount = getTargetCount;
        }

        public SpectatorTab Tab => SpectatorTab.NPCs;
        public string HeaderText => Loc.Get("ReplayHud.Spectate.NpcsTab", getTargetCount());
        public string TooltipText => Loc.Get("ReplayHud.Spectate.NpcsTooltip");
        public Asset<Texture2D> Icon => Ass.IconNPC;
        public float IconScale => 1f;
        public Vector2 IconOffset => new(-2, -2);
        public Vector2 TextOffset => new(-2, 0);
        public void Refresh()
        {
        }
    }

}
