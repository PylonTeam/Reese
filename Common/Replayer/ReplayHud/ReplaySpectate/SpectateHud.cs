using Microsoft.Xna.Framework.Input;
using Reese.Common.Replayer.ReplayHud.ReplaySpectate.TeammateOverlay;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using Reese.Common.Replayer.ReplayHud.Shared.UI;
using Reese.Core.Compat;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
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
    private const float TabHeight = 36f;

    // Card display logic
    private const int MinCardsPerRow = 3;
    private const int MaxCardsPerRow = 5;

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

        tabs.Add(new PlayersTab());
        tabs.Add(new NPCsTab());
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

        // Layout
        float tabHeight = GetTabHeight();
        float playerPanelPadding = GetPlayerPanelPadding();
        float contentHeight = currentContentHeight = GetActiveContentHeight();

        currentTab ??= tabs.Count > 0 ? tabs[0] : null;

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

        RefreshTargets();
    }

    private void RefreshTargets()
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

        List<int> playerTargets = GetPlayerTargets();
        List<int> npcTargets = GetNpcTargets();

        if (!playerTargets.Contains(locked))
            locked = -1;

        if (!playerTargets.Contains(hovered))
            hovered = -1;

        if (!npcTargets.Contains(lockedNpc))
            lockedNpc = -1;

        int slotCount = GetActiveSlotCount(playerTargets, npcTargets);
        int detailIndex = GetActiveDetailIndex(playerTargets, npcTargets);
        currentContentHeight = GetGridContentHeight(slotCount);

        Width.Set(GetGridPanelWidth(slotCount), 0f);
        Height.Set(GetPanelHeight(currentContentHeight), 0f);
        contentPanel.Height.Set(currentContentHeight, 0f);
        contentPanel.SetPadding(playerPanelPadding);


        tabBar?.RefreshHeaders();
        BuildContent(playerTargets, npcTargets);
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

    private void BuildContent(List<int> playerTargets, List<int> npcTargets)
    {
        if (contentPanel == null)
            return;

        contentPanel.RemoveAllChildren();
        targetGrid = null;

        float scale = GetScale();
        float cardScale = scale * playerCardScale;

        switch (currentTab?.Tab)
        {
            case SpectatorTab.Players:
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

                BuildEntityGrid(playerTargets.Count, playerTargets.IndexOf(locked), i =>
                {
                    int playerIndex = playerTargets[i];

                    if (playerIndex == locked)
                    {
                        UIPlayerDetailPanel detail = new(playerIndex, GetInlineDetailScale());
                        detail.OnLeftClick += (evt, _) =>
                        {
                            if (evt.Target != detail || !IsMouseInTargetGridViewport())
                                return;

                            SpectatorTargetSystem.TogglePlayerTarget(playerIndex);
                            UpdateTarget();
                        };

                        return detail;
                    }

                    UIPlayerCard card = new(playerIndex, i, cardScale);
                    card.OnLeftClick += (evt, _) =>
                    {
                        if (evt.Target != card || !IsMouseInTargetGridViewport())
                            return;

                        SpectatorTargetSystem.TogglePlayerTarget(playerIndex);
                        if (TeammateHudOverlay.IsAnyOpen)
                            TeammateHudOverlay.Open(playerIndex);
                        UpdateTarget();
                    };

                    return card;
                });
                return;

            case SpectatorTab.NPCs:
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

                BuildEntityGrid(npcTargets.Count, npcTargets.IndexOf(lockedNpc), i =>
                {
                    int npcIndex = npcTargets[i];

                    if (npcIndex == lockedNpc)
                    {
                        UINPCDetailPanel detail = new(npcIndex, GetInlineDetailScale());
                        detail.OnLeftClick += (evt, _) =>
                        {
                            if (evt.Target != detail || !IsMouseInTargetGridViewport())
                                return;

                            SpectatorTargetSystem.ToggleNPCTarget(npcIndex);
                            UpdateTarget();
                        };

                        return detail;
                    }

                    UINPCCard card = new(npcIndex, i, cardScale);
                    card.OnLeftClick += (evt, _) =>
                    {
                        if (evt.Target != card || !IsMouseInTargetGridViewport())
                            return;

                        SpectatorTargetSystem.ToggleNPCTarget(npcIndex);
                        UpdateTarget();
                    };

                    return card;
                });
                return;

            default:
                System.Diagnostics.Debug.Fail($"Unexpected spectate tab: {currentTab?.Tab}");
                return;
        }
    }

    private void BuildEntityGrid(int targetCount, int detailIndex, Func<int, UIElement> buildItem)
    {
        detailIndex = detailIndex >= 0 && detailIndex < targetCount ? detailIndex : -1;
        float cardWidth = GetCardWidth();
        float cardHeight = GetCardHeight();
        float cardGap = GetCardGap();
        int slotCount = GetSlotCount(targetCount, detailIndex);
        int columns = GetVisibleColumns(slotCount);
        bool showScrollbar = slotCount > GetMaxVisibleCards(columns);
        int visibleRows = GetVisibleRows(slotCount, columns);
        float gridWidth = GetGridWidth(columns);
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
        gridHost.Width.Set(showScrollbar ? scrollbarLeft + GetScrollbarWidth() : gridWidth, 0f);
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
            bool isDetail = i == detailIndex;
            int span = isDetail ? 2 : 1;
            float width = isDetail ? GetInlineDetailWidth() : cardWidth;

            if (isDetail && rowSlots == columns - 1 && rowItems.Count > 0)
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
        int oldLocked = locked;
        int oldLockedNpc = lockedNpc;

        Player target = SpectatorTargetSystem.GetLockedPlayerTarget();
        locked = target?.active == true ? target.whoAmI : -1;

        NPC npcTarget = SpectatorTargetSystem.GetLockedNPCTarget();
        lockedNpc = npcTarget?.active == true ? npcTarget.whoAmI : -1;

        if (locked != oldLocked || lockedNpc != oldLockedNpc)
            RefreshTargets();
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

        if (element is UIPlayerDetailPanel detail && detail.ContainsPoint(Main.MouseScreen))
            return detail.PlayerIndex;

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
        RefreshTargets();
    }

    private void RefreshSettingsIfNeeded()
    {
        if (settingsRevision == SpectateHudClientSettings.Revision)
            return;

        //Log.Chat($"RefreshSettingsIfNeeded update={Main.GameUpdateCount} revision {settingsRevision} -> {SpectateHudClientSettings.Revision}");
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
        RefreshTargets();
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
        List<int> targets = GetNpcTargets();

        if (targets.Count == 0)
        {
            SpectatorTargetSystem.ClearTarget();
            UpdateTarget();
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
        RefreshTargets();
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

        if (element is UIPlayerDetailPanel detail && detail.PlayerIndex == playerIndex)
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

        if (element is UINPCDetailPanel detail && detail.NPCIndex == npcIndex)
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

    #region Rebuild if player list changes
    private int shownPlayerListHash; // cached active Main.player list hash to detect joins/leaves
    private int shownNpcListHash; // cached active Main.npc list hash to detect joins/leaves

    private void RefreshTargetsIfNeeded()
    {
            bool showingNpcs = currentTab?.Tab == SpectatorTab.NPCs;

            if (showingNpcs)
            {
                int npcListHash = GetActiveNpcListHash();

                if (npcListHash == shownNpcListHash)
                    return;

                shownNpcListHash = npcListHash;
                RefreshTargets();
                return;
            }

            int playerListHash = GetActivePlayerListHash();

            if (playerListHash == shownPlayerListHash)
                return;

            shownPlayerListHash = playerListHash;
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
        return GetPlayerTargets().Count;
    }

    private static int GetNpcTargetCount()
    {
        return GetNpcTargets().Count;
    }

    private int GetActiveDetailIndex(List<int> playerTargets, List<int> npcTargets)
    {
        return currentTab?.Tab == SpectatorTab.NPCs
            ? npcTargets.IndexOf(lockedNpc)
            : playerTargets.IndexOf(locked);
    }

    private int GetActiveSlotCount(List<int> playerTargets, List<int> npcTargets)
    {
        return GetSlotCount(
            currentTab?.Tab == SpectatorTab.NPCs ? npcTargets.Count : playerTargets.Count,
            GetActiveDetailIndex(playerTargets, npcTargets));
    }
    #endregion

    #region Layout Helpers
    private static float GetScale() => 0.75f;
    private static float GetPlayerCardScale() => 0.8f;
    private static float GetTabHeight() => TabHeight;
    private static float GetPlayerPanelPadding() => 10f * GetScale();
    private static float GetCardGap() => 6f * GetScale();
    private static float GetScrollbarWidth() => 20;
    private static float GetCardScale() => GetScale() * GetPlayerCardScale();
    private static float GetCardWidth() => UIPlayerCard.CardWidth * GetCardScale();
    private static float GetInlineDetailWidth() => GetCardWidth() * 2f;
    private static float GetInlineDetailScale() => GetCardHeight() / UIPlayerCard.DetailHeight;
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
    private static int GetSlotCount(int count, int detailIndex) => count + (detailIndex >= 0 ? 1 : 0);
    private static int GetVisibleColumns(int count) => Math.Clamp(count, MinCardsPerRow, MaxCardsPerRow);
    private static int GetMaxVisibleCards(int columns) => columns * SpectateHudClientSettings.RowsVisible;
    private static int GetVisibleRows(int count) => GetVisibleRows(count, GetVisibleColumns(count));
    private static int GetVisibleRows(int count, int columns) => Math.Max(1, Math.Min(SpectateHudClientSettings.RowsVisible, (count + columns - 1) / columns));
    private static float GetGridWidth(int columns) => GetGridWidth(columns, GetCardWidth(), GetCardGap());
    private static float GetGridWidth(int columns, float cardWidth, float cardGap) => columns * cardWidth + Math.Max(0, columns - 1) * cardGap;
    private static float GetGridHeight(int rows) => rows * GetCardHeight() + Math.Max(0, rows - 1) * GetCardGap();
    private static float GetGridContentHeight(int count) => GetGridHeight(GetVisibleRows(count)) + GetPlayerPanelPadding() * 2f;

    private float GetActiveContentHeight()
        => GetGridContentHeight(GetActiveSlotCount(GetPlayerTargets(), GetNpcTargets()));

    private static float GetPanelHeight(float contentHeight)
        => GetTabHeight() + contentHeight;

    private static float GetGridPanelWidth(int count)
    {
        int columns = GetVisibleColumns(count);
        float contentWidth = GetGridWidth(columns);

        if (count > GetMaxVisibleCards(columns))
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
        public Vector2 IconOffset => new(2, 2);
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
        public Vector2 IconOffset => new(-2, -2);
        public Vector2 TextOffset => new(-2, 0);
        public void Refresh()
        {
        }
    }

}
