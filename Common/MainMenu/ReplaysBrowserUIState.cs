//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Reese.Common.UI;
//using Reese.Core;
//using ReLogic.Content;
//using System;
//using System.IO;
//using System.Linq;
//using Terraria;
//using Terraria.GameContent.UI.Elements;
//using Terraria.ModLoader.UI;
//using Terraria.UI;

//namespace Reese.Common._Deprecated.MainMenu;

//internal sealed class ReplaysBrowserUIState : UIState
//{
//    private UIList list;
//    private Searchbox searchBox;

//    public override void OnActivate()
//    {
//        RemoveAllChildren();

//        const float width = 540f;
//        const float height = 540f;
//        const float margin = 20f;
//        const float headerHeight = 36f;
//        const float scrollbarWidth = 20f;

//        UIElement root = new();
//        root.Width.Set(width, 0f);
//        root.Height.Set(height, 0f);
//        root.Left.Set(-(width + margin), 1f);
//        root.Top.Set(margin, 0f);
//        Append(root);

//        UITextPanel<string> header = new("Reese", 0.68f, true)
//        {
//            BackgroundColor = new Color(73, 94, 171),
//            BorderColor = new Color(89, 116, 213)
//        };
//        header.Width.Set(0f, 1f);
//        header.Height.Set(headerHeight, 0f);
//        header.SetPadding(6f);
//        root.Append(header);

//        string path = ReeseReplayPaths.GetFolder();
//        string folderName = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
//        ReeseUIHoverImage openFolderButton = new(UICommon.ButtonOpenFolder, $"Open {folderName}")
//        {
//            RemoveFloatingPointsFromDrawPosition = true,
//            UseTooltipMouseText = true,
//            Left = { Pixels = 6f },
//            Top = { Pixels = 0f },
//            VAlign = 0.5f
//        };
//        openFolderButton.OnLeftClick += (_, _) =>
//        {
//            string dir = ReeseReplayPaths.GetFolder();
//            Utils.TryCreatingDirectory(dir);
//            try { Utils.OpenFolder(dir); } catch { }
//        };
//        header.Append(openFolderButton);

//        ReeseUIHoverImage refreshButton = new(UICommon.ButtonDownloadTexture, $"Refresh {folderName}")
//        {
//            RemoveFloatingPointsFromDrawPosition = true,
//            UseTooltipMouseText = true,
//            Left = { Pixels = 36f },
//            VAlign = 0.5f
//        };
//        refreshButton.OnLeftClick += (_, _) => Rebuild();
//        header.Append(refreshButton);

//        searchBox = new("Type to search")
//        {
//            Width = { Pixels = 156f },
//            Height = { Pixels = 24f },
//            Left = { Pixels = -164f, Percent = 1f },
//            VAlign = 0.5f
//        };
//        searchBox.OnTextChanged += Rebuild;
//        header.Append(searchBox);

//        UIPanel container = new()
//        {
//            BackgroundColor = new Color(33, 43, 79) * 0.85f,
//            BorderColor = new Color(89, 116, 213) * 0.75f
//        };
//        container.Width.Set(0f, 1f);
//        container.Height.Set(-headerHeight, 1f);
//        container.Top.Set(headerHeight, 0f);
//        container.SetPadding(6f);
//        root.Append(container);

//        list = new UIList();
//        list.Width.Set(-scrollbarWidth - 4f, 1f);
//        list.Height.Set(0f, 1f);
//        list.ListPadding = 4f;
//        container.Append(list);

//        UIScrollbar scrollbar = new();
//        scrollbar.Width.Set(scrollbarWidth, 0f);
//        scrollbar.Height.Set(0f, 1f);
//        scrollbar.Left.Set(-scrollbarWidth, 1f);
//        container.Append(scrollbar);

//        list.SetScrollbar(scrollbar);
//        Rebuild();
//    }

//    private void Rebuild()
//    {
//        list.Clear();

//        string dir = ReeseReplayPaths.GetFolder();
//        Utils.TryCreatingDirectory(dir);

//        string[] entries = Directory.GetFiles(dir, "*.reese", SearchOption.TopDirectoryOnly)
//            .OrderByDescending(File.GetLastWriteTime)
//            .ToArray();

//        LogReplays(entries);

//        string query = searchBox?.currentString ?? string.Empty;
//        if (!string.IsNullOrWhiteSpace(query))
//            entries = entries.Where(x => Path.GetFileName(x).Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();

//        if (entries.Length == 0)
//        {
//            UIText empty = new("No replays found", 0.7f, false)
//            {
//                HAlign = 0.5f,
//                MarginTop = 10f
//            };
//            list.Add(empty);
//            return;
//        }

//        foreach (string path in entries)
//            list.Add(new ReplayListItem(path));
//    }

//    public static void EnterReplay(string demoPath)
//    {
//        Main.QueueMainThreadAction(() =>
//        {
//            Main.LoadPlayers();
//            var player = Main.PlayerList.FirstOrDefault();
//            if (player == null)
//            {
//                Main.menuMode = 0;
//                return;
//            }

//            Main.SelectPlayer(player);
//            Log.Debug($"Successfully selected {player.Player.name} for replay");

//            if (!File.Exists(demoPath))
//            {
//                Log.Error("Error: No file demo found at: " + demoPath);
//                Main.menuMode = 0;
//                return;
//            }

//            long replayMegaBytes = new FileInfo(demoPath).Length / (1024 * 1024);
//            Log.Debug("Successfully found replay file, size: " + replayMegaBytes + " MB");

//            try
//            {
//                Replayer.BeginPlayback(demoPath);
//            }
//            catch (Exception e)
//            {
//                Log.Error("Failed to start replay: " + e);
//                Main.statusText = "Failed to start replay";
//                ReplaySession.End("playback launch failed");
//            }
//        });
//    }

//    private void LogReplays(string[] entries)
//    {
//        Log.Debug($"Found {entries.Length} replay entries");

//        for (int i = 0; i < entries.Length; i++)
//        {
//            string path = entries[i];
//            long bytes = 0;
//            try { bytes = new FileInfo(path).Length; } catch { }
//            Log.Debug($"Entry {i + 1}: {Path.GetFileName(path)}, bytes: {bytes}");
//        }
//    }
//}
