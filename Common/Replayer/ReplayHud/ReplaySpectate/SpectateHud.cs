using Microsoft.Xna.Framework.Input;
using Reese.Common.Replayer.ReplayHud.Shared.Tabs;
using Reese.Core.Configs;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace Reese.Common.Replayer.ReplayHud.ReplaySpectate;

/// <summary>
/// Main spectator HUD in bottom center of the screen. 
/// Displays a horizontal list of spectatable players and allows the user to hover and lock onto a target.
/// </summary>
internal sealed class SpectateHud : UIElement
{
    private const float HeaderHeight = 32f;
    private const float TabHeight = 36f;
    private const float StatusTextScale = 1.4f;
    private const float StatusPanelHeight = 44f;
    private const float StatusPanelPadding = 6f;
    private const float ContentGap = 4f;
    private const float NavButtonGap = 6f;
    private const float PlayerHudOpenOffset = 200f;

    private const int MinShownPlayerCards = 1;
    private const int MaxShownPlayerCards = 3;
    private static int requestedShownPlayerCards = 1;
    private static int cardCountRevision;
    private static int lastShownPlayerCards = 1;
    private static bool userChangedShownPlayerCards;

    private int shownPlayerCards = 1; // number of player cards shown in the panel
    private int visibleTargetStart; // first target index shown in the player card window
    private int observedCardCountRevision;

    private int locked = -1; // currently locked spectated player index, -1 means no locked target
    private int lockedNpc = -1; // currently locked spectated NPC index, -1 means no locked NPC target
    private int hovered = -1; // currently hovered spectated player index, -1 means no hovered target
    private UIText statusText; // UI element for displaying the current status like "Spectating: PlayerName" or "Free camera" or "Auto-director"
    private string statusTextRaw = string.Empty;
    private float panelWidth;
    private int visibleNpcStart;
    private int shownNpcCards = 1;
    private int shownNpcListHash;

    private readonly List<ITab> tabs = [];
    private ITab currentTab;
    private TabBar tabBar;
    private UIPanel headerPanel;
    private UIPanel contentPanel;
    private UIPanel statusPanel;

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

    public void Rebuild()
    {
        RemoveAllChildren();

        float headerHeight = GetHeaderHeight();
        float tabHeight = GetTabHeight();
        float playerPanelPadding = GetPlayerPanelPadding();
        float navButtonWidth = GetNavButtonWidth();
        float cardGap = GetCardGap();
        float cardWidth = GetCardWidth();
        float cardHeight = GetCardHeight();

        currentTab ??= tabs.Count > 0 ? tabs[0] : null;

        // Target filtering
        List<int> playerTargets = GetPlayerTargets();
        List<int> npcTargets = GetNpcTargets();

        shownPlayerListHash = GetActivePlayerListHash();
        shownNpcListHash = GetActiveNpcListHash();

        if (!playerTargets.Contains(locked))
            locked = -1;

        if (!playerTargets.Contains(hovered))
            hovered = -1;

        if (!npcTargets.Contains(lockedNpc))
            lockedNpc = -1;

        int maxByCount = Math.Min(MaxShownPlayerCards, playerTargets.Count);
        int desired = userChangedShownPlayerCards ? requestedShownPlayerCards : playerTargets.Count;
        shownPlayerCards = Math.Clamp(desired, MinShownPlayerCards, Math.Max(MinShownPlayerCards, maxByCount));
        shownNpcCards = MaxShownPlayerCards;
        lastShownPlayerCards = shownPlayerCards;
        if (!userChangedShownPlayerCards)
            requestedShownPlayerCards = shownPlayerCards;

        int activeCards = currentTab?.Tab == SpectatorTab.NPCs ? MaxShownPlayerCards : shownPlayerCards;
        panelWidth = GetPanelWidth(activeCards, cardWidth);
        Width.Set(panelWidth, 0f);
        Height.Set(GetPanelHeight(), 0f);

        // Update the number of visible targets based on the number of cards to show
        visibleTargetStart = Math.Clamp(visibleTargetStart, 0, Math.Max(0, playerTargets.Count - shownPlayerCards));
        int visibleTargets = Math.Min(Math.Max(0, playerTargets.Count - visibleTargetStart), shownPlayerCards);

        visibleNpcStart = Math.Clamp(visibleNpcStart, 0, Math.Max(0, npcTargets.Count - shownNpcCards));
        int visibleNpcTargets = Math.Min(Math.Max(0, npcTargets.Count - visibleNpcStart), shownNpcCards);

        // Layout
        float cardsStart = navButtonWidth + NavButtonGap;

        headerPanel = BuildHeaderPanel(headerHeight);
        Append(headerPanel);

        tabBar = new TabBar();
        tabBar.Top.Set(headerHeight, 0f);
        tabBar.Width.Set(0f, 1f);
        tabBar.Height.Set(tabHeight, 0f);
        tabBar.BuildTabs(tabs, () => currentTab, ShowTab, GetScale());
        Append(tabBar);

        contentPanel = new UIPanel();
        contentPanel.SetPadding(playerPanelPadding);
        contentPanel.Top.Set(headerHeight + tabHeight, 0f);
        contentPanel.Width.Set(0f, 1f);
        float contentHeight = GetContentHeight();
        contentPanel.Height.Set(contentHeight, 0f);
        contentPanel.BackgroundColor = UICommon.DefaultUIBlueMouseOver * 0.3f;
        contentPanel.BorderColor = Color.Black;
        Append(contentPanel);

        BuildContent(playerTargets, npcTargets, visibleTargets, visibleNpcTargets, playerPanelPadding, navButtonWidth, cardGap, cardWidth, cardHeight, cardsStart);

        statusPanel = new UIPanel();
        statusPanel.SetPadding(GetStatusPanelPadding());
        statusPanel.Width.Set(0f, 1f);
        statusPanel.BackgroundColor = new Color(35, 54, 96) * 0.85f;
        statusPanel.BorderColor = Color.Black;
        statusPanel.Height.Set(StatusPanelHeight, 0f);
        statusPanel.Top.Set(headerHeight + tabHeight + contentHeight + ContentGap, 0f);
        Append(statusPanel);

        statusText = new UIText("", textScale: GetStatusTextScale())
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

        UIPanel closePanel = new()
        {
            Height = new StyleDimension(0f, 1f),
            Width = new StyleDimension(40f * scale, 0f),
            HAlign = 1f,
            VAlign = 0.5f,
            BackgroundColor = normalColor,
            BorderColor = Color.Black
        };

        closePanel.SetPadding(0f);

        closePanel.OnMouseOver += (_, _) =>
        {
            closePanel.BackgroundColor = hoverColor;
            SoundEngine.PlaySound(SoundID.MenuTick);
        };

        closePanel.OnMouseOut += (_, _) =>
        {
            closePanel.BackgroundColor = normalColor;
        };

        closePanel.OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            ModContent.GetInstance<ReplayHudSystem>().CloseSpectateHud();
        };

        closePanel.Append(new UIText("X", large: true, textScale: 0.55f * scale)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            TextColor = Color.White
        });

        panel.Append(closePanel);

        return panel;
    }

    private void BuildContent(
        List<int> playerTargets,
        List<int> npcTargets,
        int visiblePlayerTargets,
        int visibleNpcTargets,
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

        if (currentTab?.Tab == SpectatorTab.Players)
        {
            if (playerTargets.Count == 0)
            {
                UIText noPlayersText = new("No players are available\n to spectate.", 0.9f)
                {
                    HAlign = 0.5f,
                    VAlign = 0.5f,
                    TextColor = Color.LightGray
                };

                contentPanel.Append(noPlayersText);
                return;
            }

            AddPrevButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, () => NavigateTarget(-1), "Go to previous player");

            for (int i = 0; i < visiblePlayerTargets; i++)
            {
                int targetIndex = visibleTargetStart + i;
                int playerIndex = playerTargets[targetIndex];

                float scale = GetScale();
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

            AddNextButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, () => NavigateTarget(1), "Go to next player");
            return;
        }

        if (currentTab?.Tab != SpectatorTab.NPCs)
            return;

        if (npcTargets.Count == 0)
        {
            UIText noNpcsText = new("No NPCs are available\n to spectate.", 0.9f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                TextColor = Color.LightGray
            };

            contentPanel.Append(noNpcsText);
            return;
        }

        AddPrevButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, () => NavigateNpcTarget(-1), "Go to previous NPC");

        for (int i = 0; i < visibleNpcTargets; i++)
        {
            int targetIndex = visibleNpcStart + i;
            int npcIndex = npcTargets[targetIndex];

            UINPCCard npcCard = new(npcIndex, i, 1f);
            npcCard.Width.Set(cardWidth, 0f);
            npcCard.Height.Set(cardHeight, 0f);
            npcCard.Left.Set(cardsStart + i * (cardWidth + cardGap), 0f);
            npcCard.OnLeftClick += (evt, element) =>
            {
                UpdateTarget();
                UpdateStatusText();
            };
            contentPanel.Append(npcCard);
        }

        AddNextButton(contentPanel, playerPanelPadding, navButtonWidth, cardHeight, () => NavigateNpcTarget(1), "Go to next NPC");
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        RebuildIfNeeded();
        RebuildIfCardCountChanged();

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
        if (locked < 0 && lockedNpc < 0 && Main.LocalPlayer?.ghost == true)
            return "You are in ghost mode";

        if (hovered >= 0 && Main.player[hovered]?.active == true)
        {
            if (locked == hovered)
                return $"Spectating {Main.player[hovered].name}. Click to cancel";

            return $"Previewing {Main.player[hovered].name}. Click to spectate";
        }

        if (locked >= 0 && Main.player[locked]?.active == true)
            return $"Spectating {Main.player[locked].name}";

        if (lockedNpc >= 0 && Main.npc[lockedNpc]?.active == true)
            return $"Spectating {Main.npc[lockedNpc].FullName}";

        return "You are not spectating any target";
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

        float textScale = GetStatusTextScale();
        string wrappedText = WrapStatusText(text, GetStatusTextMaxWidth(), textScale, out _);
        statusText.SetText(wrappedText, textScale, false);
        UpdateStatusPanelLayout(wrappedText);
    }

    private void UpdateStatusPanelLayout(string wrappedText)
    {
        float padding = GetStatusPanelPadding();
        float maxTextWidth = panelWidth - padding * 2f;
        float maxTextHeight = GetStatusPanelHeight() - padding * 2f;
        float fittedScale = FitTextScale(wrappedText, GetStatusTextScale(), maxTextWidth, maxTextHeight);

        statusText.SetText(wrappedText, fittedScale, false);
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

        requestedShownPlayerCards = next;
        userChangedShownPlayerCards = true;
        cardCountRevision++;
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

        int oldVisibleTargetStart = visibleNpcStart;
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

        MakeNpcVisible(nextIndex, targets.Count);

        if (visibleNpcStart != oldVisibleTargetStart)
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

    private void MakeNpcVisible(int targetIndex, int targetCount)
    {
        int maxVisibleTargetStart = Math.Max(0, targetCount - shownNpcCards);

        visibleNpcStart = Math.Clamp(visibleNpcStart, 0, maxVisibleTargetStart);

        if (targetIndex < visibleNpcStart)
            visibleNpcStart = targetIndex;
        else if (targetIndex >= visibleNpcStart + shownNpcCards)
            visibleNpcStart = targetIndex - shownNpcCards + 1;

        visibleNpcStart = Math.Clamp(visibleNpcStart, 0, maxVisibleTargetStart);
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
            int npcListHash = GetActiveNpcListHash();

            if (playerListHash != shownPlayerListHash || npcListHash != shownNpcListHash)
            Rebuild();
    }

    private void RebuildIfCardCountChanged()
    {
        if (observedCardCountRevision == cardCountRevision)
            return;
        if (locked < 0 && lockedNpc < 0 && Main.LocalPlayer?.ghost == true)
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
    private static float GetScale()
    {
        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();

        return clientConfig.replayHudSize switch
        {
            ClientConfig.ReplayHudSize.Small => 0.7f,
            ClientConfig.ReplayHudSize.Medium => 0.9f,
            ClientConfig.ReplayHudSize.Large => 1.1f,
            _ => 1f
        };
    }

    private static float GetHeaderHeight() => HeaderHeight * GetScale();

    private static float GetTabHeight() => TabHeight * GetScale();

    private static float GetPlayerPanelPadding() => 4f * GetScale();

    private static float GetNavButtonWidth() => 24f * GetScale();

    private static float GetCardGap() => 6f * GetScale();

    private static float GetContentGap() => ContentGap * GetScale();

    private static float GetStatusPanelHeight() => StatusPanelHeight * GetScale();

    private static float GetStatusPanelPadding() => StatusPanelPadding * GetScale();

    private static float GetStatusTextScale() => StatusTextScale * GetScale();

    private static float GetCardWidth() => UIPlayerCard.CardWidth * GetScale();

    private static float GetCardHeight() => UIPlayerCard.CardHeight * GetScale();

    private static float GetContentHeight()
    {
        return GetCardHeight() + GetPlayerPanelPadding() * 2f;
    }

    private static float GetPanelHeight()
    {
        return GetHeaderHeight() + GetTabHeight() + GetContentHeight() + GetContentGap() + GetStatusPanelHeight();
    }

    private static float GetPanelWidth(int shownCards, float cardWidth)
    {
        float cardsWidth = shownCards * cardWidth + Math.Max(0, shownCards - 1) * GetCardGap();
        float contentWidth = GetNavButtonWidth() * 2f + NavButtonGap * GetScale() * 2f + cardsWidth;
        return contentWidth + GetPlayerPanelPadding() * 2f;
    }

    private float GetStatusTextMaxWidth()
    {
        float statusPadding = GetStatusPanelPadding();
        float width = (panelWidth > 0f ? panelWidth : GetPanelWidth(MaxShownPlayerCards, GetCardWidth())) - statusPadding * 2f;
        return Math.Max(40f * GetScale(), width * 1.35f);
    }

    private static float FitTextScale(string text, float baseScale, float maxWidth, float maxHeight)
    {
        if (string.IsNullOrWhiteSpace(text))
            return baseScale;

        string[] lines = text.Split('\n');
        float maxLineWidth = 0f;

        foreach (string line in lines)
            maxLineWidth = Math.Max(maxLineWidth, FontAssets.MouseText.Value.MeasureString(line).X);

        float totalHeight = FontAssets.MouseText.Value.LineSpacing * Math.Max(1, lines.Length);
        float widthScale = maxLineWidth <= 0f ? baseScale : maxWidth / maxLineWidth;
        float heightScale = totalHeight <= 0f ? baseScale : maxHeight / totalHeight;
        float fitted = Math.Min(baseScale, Math.Min(widthScale, heightScale));

        return Math.Max(baseScale * 0.6f, fitted);
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
        prevButton.OnLeftClick += (evt, element) => onClick();
        prevButton.OnMouseOver += (evt, element) =>
        {
            prevButton.BorderColor = Color.Yellow;
            SetStatusText(hoverText);
        };
        prevButton.OnMouseOut += (evt, element) =>
        {
            prevButton.BorderColor = Color.Black;
            UpdateStatusText();
        };

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
        nextButton.OnLeftClick += (evt, element) => onClick();
        nextButton.OnMouseOver += (evt, element) =>
        {
            nextButton.BorderColor = Color.Yellow;
            SetStatusText(hoverText);
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
        public string HeaderText => $"Players ({GetPlayerTargetCount()})";
        public string TooltipText => "Spectate players";
        public Asset<Texture2D> Icon => Ass.Icon_Player;
        public float IconScale => 1.2f;
        public Vector2 IconOffset => new(0f, 1f);

        public void Refresh()
        {
        }
    }

    private sealed class NPCsTab : ITab
    {
        public SpectatorTab Tab => SpectatorTab.NPCs;
        public string HeaderText => $"NPCs ({GetNpcTargetCount()})";
        public string TooltipText => "Spectate NPCs";
        public Asset<Texture2D> Icon => Ass.Icon_NPC;
        public float IconScale => 1f;
        public Vector2 IconOffset => new(0f, -2f);

        public void Refresh()
        {
        }
    }

}
