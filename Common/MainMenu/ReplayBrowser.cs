using Reese.Common.MainMenu.UI;
using Reese.Common.Replayer;
using System;
using System.Linq;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Reese.Common.MainMenu;

internal sealed class ReplayBrowser : UIElement
{
    internal const float PanelWidth = 540f;
    internal const float BrowserPanelHeight = 540f;

    private ReplayBrowserPanel browserPanel;

    public event Action OnRefreshStarted;
    public event Action OnRefreshFinished;

    public override void OnActivate()
    {
        Build();
    }

    public void Build()
    {
        RemoveAllChildren();

        Width.Set(PanelWidth, 0f);
        Height.Set(BrowserPanelHeight, 0f);

        browserPanel = new ReplayBrowserPanel();
        browserPanel.Width.Set(0f, 1f);
        browserPanel.Height.Set(BrowserPanelHeight, 0f);
        browserPanel.OnRefreshStarted += () => OnRefreshStarted?.Invoke();
        browserPanel.OnRefreshFinished += () => OnRefreshFinished?.Invoke();
        Append(browserPanel);
        browserPanel.Build();

        Log.Chat("Build ReplayBrowser");
    }

    public void Refresh()
    {
        browserPanel?.Refresh();
    }
}

internal sealed class ReplayBrowserPanel : UIElement
{
    private enum SortColumn
    {
        None, // (default) Sort by date, newest first
        Name,
        Date,
        Length,
        Mods,
        Size,
    }

    private UIList list;
    private Searchbox searchBox;
    private SortColumn sortColumn = SortColumn.None;
    private bool sortAscending;

    public event Action OnRefreshStarted;
    public event Action OnRefreshFinished;

    // Cache entries
    private ReplayMetadata[] cachedEntries = [];
    private int refreshGeneration;

    public void Build()
    {
        ReplayLayout.Update();

        const float headerHeight = 46f;
        const float headerButtonSize = 36f * 0.85f;
        const float headerButtonGap = 4f;

        RemoveAllChildren();
        SetPadding(0f);
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);

        UIPanel container = new()
        {
            BackgroundColor = new Color(33, 43, 79) * 0.8f,
            BorderColor = Color.Black
        };
        container.Width.Set(0f, 1f);
        container.Height.Set(-headerHeight, 1f);
        container.Top.Set(headerHeight, 0f);
        container.SetPadding(ReplayLayout.ContentPadding);
        Append(container);

        UIElement tableHeader = new();
        tableHeader.Width.Set(ReplayLayout.TableWidth, 0f);
        tableHeader.Height.Set(ReplayLayout.TableColumnHeight, 0f);
        container.Append(tableHeader);

        UISortableTableColumn nameColumn = UISortableTableColumn.AppendHeader(tableHeader, "Name", 0f, ReplayLayout.NameColumnWidth);
        UISortableTableColumn dateColumn = UISortableTableColumn.AppendHeader(tableHeader, "Date", ReplayLayout.NameColumnWidth, ReplayLayout.DateColumnWidth);
        UISortableTableColumn lengthColumn = UISortableTableColumn.AppendHeader(tableHeader, "Length", ReplayLayout.NameColumnWidth + ReplayLayout.DateColumnWidth, ReplayLayout.LengthColumnWidth);
        UISortableTableColumn modsColumn = UISortableTableColumn.AppendHeader(tableHeader, "Mods", ReplayLayout.ModsLeft, ReplayLayout.ModsColumnWidth);
        UISortableTableColumn sizeColumn = UISortableTableColumn.AppendHeader(tableHeader, "Size", ReplayLayout.SizeLeft, ReplayLayout.SizeColumnWidth);

        void RefreshColumnStates()
        {
            nameColumn.SetSortState(sortColumn == SortColumn.Name, sortAscending);
            dateColumn.SetSortState(sortColumn == SortColumn.Date, sortAscending);
            lengthColumn.SetSortState(sortColumn == SortColumn.Length, sortAscending);
            modsColumn.SetSortState(sortColumn == SortColumn.Mods, sortAscending);
            sizeColumn.SetSortState(sortColumn == SortColumn.Size, sortAscending);
        }

        void SortBy(SortColumn column)
        {
            if (sortColumn == column)
                sortAscending = !sortAscending;
            else
            {
                sortColumn = column;
                sortAscending = true;
            }

            RefreshColumnStates();
            ApplyCurrentFilter();
        }

        void ClearSort()
        {
            sortColumn = SortColumn.None;
            sortAscending = false;
            RefreshColumnStates();
        }

        nameColumn.OnLeftClick += (_, _) => SortBy(SortColumn.Name);
        dateColumn.OnLeftClick += (_, _) => SortBy(SortColumn.Date);
        lengthColumn.OnLeftClick += (_, _) => SortBy(SortColumn.Length);
        sizeColumn.OnLeftClick += (_, _) => SortBy(SortColumn.Size);
        modsColumn.OnLeftClick += (_, _) => SortBy(SortColumn.Mods);
        RefreshColumnStates();

        list = new UIList();
        list.Width.Set(-ReplayLayout.ScrollbarWidth - 4f, 1f);
        list.Height.Set(-ReplayLayout.ListTop, 1f);
        list.Top.Set(ReplayLayout.ListTop, 0f);
        list.ListPadding = 4f;
        container.Append(list);

        UIScrollbar scrollbar = new();
        scrollbar.Width.Set(ReplayLayout.ScrollbarWidth, 0f);
        scrollbar.Height.Set(-ReplayLayout.ListTop - 6, 1f);
        scrollbar.Left.Set(-ReplayLayout.ScrollbarWidth, 1f);
        scrollbar.Top.Set(ReplayLayout.ListTop, 0f);
        container.Append(scrollbar);
        list.SetScrollbar(scrollbar);

        // Header
        string headerText = GetHeaderText();
        Vector2 textSize = ChatManager.GetStringSize(FontAssets.DeathText.Value, headerText, new Vector2(0.66f));
        const float iconWidth = 30f;
        const float iconGap = 6f;
        float iconLeft = (540 / 2f) - (textSize.X / 2f) - iconWidth - iconGap - 6f;
        UITextPanel<string> header = new(GetHeaderText(), 0.66f, true)
        {
            BackgroundColor = new Color(73, 94, 171),
            BorderColor = Color.Black
        };
        header.Top.Set(0, 0);
        header.Width.Set(0f, 1f);
        header.Height.Set(headerHeight, 0f);
        header.SetPadding(6f);
        Append(header);

        UIImage cameraIcon = new(Ass.IconCameraSmall)
        {
            VAlign = 0f,
            Top = { Pixels = 0f },
            Left = { Pixels = iconLeft },
            ImageScale = 1.5f
        };
        cameraIcon.Width.Set(30f, 0f);
        cameraIcon.Height.Set(30f, 0f);
        header.Append(cameraIcon);

        // Buttons
        UIElement buttonStrip = new()
        {
            Left = { Pixels = 0f },
            VAlign = 0
        };
        buttonStrip.Width.Set(headerButtonSize * 3f + headerButtonGap * 2f, 0f);
        buttonStrip.Height.Set(headerButtonSize, 0f);
        header.Append(buttonStrip);

        UIHoverImage openFolderButton = new(Ass.ButtonOpenFolder, "Open folder")
        {
            ImageScale = 0.9f,
            RemoveFloatingPointsFromDrawPosition = true,
            UseTooltipMouseText = true
        };
        openFolderButton.Width.Set(headerButtonSize, 0f);
        openFolderButton.Height.Set(headerButtonSize, 0f);
        openFolderButton.OnLeftClick += (_, _) =>
        {
            string dir = ReplayPaths.GetFolder();
            Utils.TryCreatingDirectory(dir);
            try { Utils.OpenFolder(dir); } catch { }
        };
        buttonStrip.Append(openFolderButton);

        UIHoverImage configButton = new(UICommon.ButtonModConfigTexture, "Open config")
        {
            ImageScale = 0.9f,
            RemoveFloatingPointsFromDrawPosition = true,
            UseTooltipMouseText = true,
            Left = { Pixels = headerButtonSize + headerButtonGap }
        };
        configButton.Width.Set(headerButtonSize, 0f);
        configButton.Height.Set(headerButtonSize, 0f);
        configButton.OnLeftClick += (_, _) => OpenReeseClientConfig();
        buttonStrip.Append(configButton);

        UIHoverImage refreshButton = new(Ass.ButtonRefresh, "Refresh")
        {
            ImageScale = 0.9f,
            RemoveFloatingPointsFromDrawPosition = true,
            UseTooltipMouseText = true,
            Left = { Pixels = headerButtonSize * 2f + headerButtonGap * 2f }
        };
        refreshButton.Width.Set(headerButtonSize, 0f);
        refreshButton.Height.Set(headerButtonSize, 0f);
        refreshButton.OnLeftClick += (_, _) =>
        {
            ClearSort();
            Refresh();
        };
        buttonStrip.Append(refreshButton);

        searchBox = new("Type to search")
        {
            Width = { Pixels = 156f },
            Height = { Pixels = 28f },
            Left = { Pixels = -168f, Percent = 1f },
            VAlign = 0.5f
        };
        searchBox.OnTextChanged += ApplyCurrentFilter;
        header.Append(searchBox);

        Refresh();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!ReplayLayout.Update())
            return;

        Build();
        ApplyCurrentFilter();
    }

    private void OpenReeseClientConfig()
    {
        ModContent.GetInstance<MainMenuSystem>().OpenClientConfig();
    }

    private static string GetHeaderText()
    {
        string version = ModContent.GetInstance<global::Reese.Reese>()?.Version?.ToString();
        return string.IsNullOrWhiteSpace(version) ? "Reese" : $"Reese v{version}";
    }

    public void Refresh(bool showLoading = true)
    {
        if (list == null)
            return;

        int generation = ++refreshGeneration;

        list.Clear();
        list.Recalculate();

        string dir = ReplayPaths.GetFolder();
        ReplayMetadata[] warmEntries = ReplayCatalogService.Shared.GetCachedEntries(dir);
        if (warmEntries.Length > 0)
        {
            cachedEntries = warmEntries;
            ApplyCurrentFilterCore();
        }

        if (showLoading)
            OnRefreshStarted?.Invoke();

        Task.Run(() => ReplayCatalogService.Shared.Load(dir)).ContinueWith(task =>
        {
            Main.QueueMainThreadAction(() =>
            {
                if (generation != refreshGeneration || list == null)
                    return;

                if (task.IsFaulted)
                {
                    Log.Error("Failed to read replay folder: " + task.Exception);
                    cachedEntries = [];
                    AddMessage("Failed to read replay folder");
                    list.Recalculate();
                    OnRefreshFinished?.Invoke();
                    return;
                }

                ReplayCatalogLoadResult result = task.Result;
                cachedEntries = result.Entries;

                var applyWatch = System.Diagnostics.Stopwatch.StartNew();
                int visibleCount = ApplyCurrentFilterCore();
                applyWatch.Stop();
                Log.Info(
                    $"Replay catalog load finished: entries={cachedEntries.Length}/{result.FileCount}, " +
                    $"cacheHits={result.CacheHitCount}, loaded={result.LoadedCount}, loadMs={result.ElapsedMilliseconds}, " +
                    $"applyVisible={visibleCount}, applyMs={applyWatch.ElapsedMilliseconds}");

                OnRefreshFinished?.Invoke();
            });
        });
    }

    private void ApplyCurrentFilter()
    {
        ApplyCurrentFilterCore();
    }

    private int ApplyCurrentFilterCore()
    {
        if (list == null)
            return 0;

        list.Clear();

        bool hasAnyReplays = cachedEntries.Length > 0;
        string query = searchBox?.currentString ?? string.Empty;
        ReplayMetadata[] entries = cachedEntries;

        if (!string.IsNullOrWhiteSpace(query))
            entries = entries.Where(x => x.ReplayName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (entries.Length == 0)
        {
            if (hasAnyReplays)
                AddMessage("No replays found", "0 replays filtered by enabled search filter.");
            else
                AddMessage("No replays yet", "Host a multiplayer world to create one, or place a .reese file in the folder");

            list.Recalculate();
            return 0;
        }

        ReplayMetadata[] sortedEntries = SortEntries(entries);
        foreach (ReplayMetadata entry in sortedEntries)
            list.Add(new ReplayListItem(entry, () => Refresh(showLoading: false), HandleFavoriteToggled));

        list.Recalculate();
        return sortedEntries.Length;
    }

    private void HandleFavoriteToggled(string fullPath, ReplayFileFlags flags)
    {
        ReplayMetadata[] serviceEntries = ReplayCatalogService.Shared.GetCurrentEntries();
        if (serviceEntries.Length > 0)
        {
            cachedEntries = serviceEntries;
            ApplyCurrentFilterCore();
            return;
        }

        for (int i = 0; i < cachedEntries.Length; i++)
        {
            if (!string.Equals(cachedEntries[i].FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
                continue;

            cachedEntries[i] = cachedEntries[i].WithFlags(flags);
            break;
        }

        ApplyCurrentFilterCore();
    }

    private ReplayMetadata[] SortEntries(ReplayMetadata[] entries)
    {
        IOrderedEnumerable<ReplayMetadata> sorted = sortColumn switch
        {
            SortColumn.Name => sortAscending
                ? entries.OrderBy(x => x.ReplayName).ThenByDescending(x => x.IsFavorite).ThenByDescending(x => x.DateCreated)
                : entries.OrderByDescending(x => x.ReplayName).ThenByDescending(x => x.IsFavorite).ThenByDescending(x => x.DateCreated),

            SortColumn.Length => sortAscending
                ? entries.OrderBy(x => x.DurationTicks).ThenByDescending(x => x.IsFavorite).ThenByDescending(x => x.DateCreated)
                : entries.OrderByDescending(x => x.DurationTicks).ThenByDescending(x => x.IsFavorite).ThenByDescending(x => x.DateCreated),

            SortColumn.Mods => sortAscending
                ? entries.OrderBy(GetModCount).ThenByDescending(x => x.IsFavorite).ThenByDescending(x => x.DateCreated)
                : entries.OrderByDescending(GetModCount).ThenByDescending(x => x.IsFavorite).ThenByDescending(x => x.DateCreated),

            SortColumn.Size => sortAscending
                ? entries.OrderBy(x => x.SizeBytes).ThenByDescending(x => x.IsFavorite).ThenByDescending(x => x.DateCreated)
                : entries.OrderByDescending(x => x.SizeBytes).ThenByDescending(x => x.IsFavorite).ThenByDescending(x => x.DateCreated),

            SortColumn.Date => sortAscending
                ? entries.OrderBy(x => x.DateCreated).ThenByDescending(x => x.IsFavorite).ThenBy(x => x.ReplayName)
                : entries.OrderByDescending(x => x.DateCreated).ThenByDescending(x => x.IsFavorite).ThenBy(x => x.ReplayName),

            _ => entries.OrderByDescending(x => x.DateCreated).ThenByDescending(x => x.IsFavorite).ThenBy(x => x.ReplayName)
        };

        return sorted.ToArray();
    }

    private static int GetModCount(ReplayMetadata metadata)
    {
        return metadata.ModNames?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() ?? 0;
    }

    private void AddMessage(string line1, string line2 = null)
    {
        UIElement container = new();
        container.Width.Set(0f, 1f);
        container.Height.Set(string.IsNullOrWhiteSpace(line2) ? 42f : 72f, 0f);

        UIText firstLine = new(line1, 1f, false)
        {
            HAlign = 0.5f,
            Top = { Pixels = 8f },
            TextColor = new Color(200, 200, 200, 200)
        };
        container.Append(firstLine);

        if (!string.IsNullOrWhiteSpace(line2))
        {
            UIText secondLine = new(line2, 0.85f, false)
            {
                HAlign = 0.5f,
                Top = { Pixels = 38f },
                TextColor = new Color(170, 170, 170, 200)
            };
            container.Append(secondLine);
        }

        list.Add(container);
    }

}
