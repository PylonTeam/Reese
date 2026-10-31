using Reese.Common.Spectator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Terraria.ID;
using Terraria.IO;
using Terraria.Map;
using Terraria.Net.Sockets;

namespace Reese.Common.Replayer;

internal static class MapRevealHelper
{
    private const int TilesPerUpdate = 12_000;
    private const long ReceiveTimeoutMilliseconds = 30_000;
    private static ClientReveal clientReveal;

    private sealed class ClientReveal
    {
        internal readonly Guid Operation = Guid.NewGuid();
        internal readonly WorldMap Map = Main.Map;
        internal readonly PlayerFileData File = Main.ActivePlayerFileData;
        internal readonly Player Player = Main.LocalPlayer;
        internal readonly int WorldId = Main.worldID;
        internal readonly Guid WorldGuid = Main.ActiveWorldFileData.UniqueId;
        internal readonly int Mode = Main.netMode;
        internal readonly bool IsReplay = ReplayPlayback.IsReplayPlayback;
        internal readonly ISocket Socket = Netplay.Connection?.Socket;
        internal readonly int Width = Main.maxTilesX;
        internal readonly int Height = Main.maxTilesY;
        internal int SectionsX => (Width + 199) / 200;
        internal int SectionCount => SectionsX * ((Height + 149) / 150);
        internal int Section;
        internal int Tile;
        internal int RequestedThrough;
        internal long RequestedAt;
        internal int Progress;
        internal int MissingReplaySections;

        internal bool IsCurrent => !Main.gameMenu && Mode == Main.netMode &&
            IsReplay == ReplayPlayback.IsReplayPlayback && ReferenceEquals(Map, Main.Map) &&
            ReferenceEquals(File, Main.ActivePlayerFileData) && ReferenceEquals(Player, Main.LocalPlayer) &&
            WorldId == Main.worldID && WorldGuid == Main.ActiveWorldFileData?.UniqueId &&
            Width == Main.maxTilesX && Height == Main.maxTilesY &&
            (Mode != NetmodeID.MultiplayerClient || ReferenceEquals(Socket, Netplay.Connection?.Socket));

        internal bool DownloadsTerrain => Mode == NetmodeID.MultiplayerClient && !IsReplay &&
            Socket is not Replayer.ReplaySocket;

        internal bool SectionAvailable => Mode != NetmodeID.MultiplayerClient ||
            Main.sectionManager?.SectionLoaded(Section % SectionsX, Section / SectionsX) == true;

        internal void Advance()
        {
            Stopwatch budget = Stopwatch.StartNew();
            int remaining = TilesPerUpdate;

            while (Section < SectionCount && remaining > 0 && budget.ElapsedMilliseconds < 4)
            {
                if (!SectionAvailable)
                {
                    if (!IsReplay)
                        break;

                    // A replay socket cannot answer requests. Older recordings may lack terrain.
                    MissingReplaySections++;
                    Section++;
                    continue;
                }

                int startX = Section % SectionsX * 200;
                int startY = Section / SectionsX * 150;
                int width = Math.Min(200, Width - startX);
                int height = Math.Min(150, Height - startY);
                int x = startX + Tile % width;
                int y = startY + Tile / width;

                if (x >= WorldMap.BlackEdgeWidth && y >= WorldMap.BlackEdgeWidth &&
                    x < Width - WorldMap.BlackEdgeWidth && y < Height - WorldMap.BlackEdgeWidth)
                    Map.Update(x, y, byte.MaxValue);

                Tile++;
                remaining--;

                if (Tile == width * height)
                {
                    MapRevealSystem.QueueSectionRedraw(Section % SectionsX, Section / SectionsX);
                    Section++;
                    Tile = 0;
                }
            }
        }
    }

    public static bool Revealed { get; private set; }

    private static bool CanReveal => !Main.dedServ && Main.netMode != NetmodeID.Server &&
        (SpectatorMode.CanUseReplayHud || SpectatorMode.CanUseGhostHud) &&
        (Main.netMode != NetmodeID.MultiplayerClient || Netplay.Connection?.Socket is ISocket socket &&
            (ReplayPlayback.IsReplayPlayback || socket is not Replayer.ReplaySocket));

    public static void SetRevealed(bool revealed)
    {
        if (revealed)
            RevealLocalMap();
        else
            ClearLocalMap();
    }

    public static void RevealLocalMap()
    {
        if (!CanReveal)
            return;

        if (!Main.mapEnabled || Main.Map == null || Main.ActivePlayerFileData == null ||
            Main.ActiveWorldFileData == null || Main.maxTilesX <= 0 || Main.maxTilesY <= 0)
        {
            ShowMessage("EnableMap");
            return;
        }

        if (clientReveal?.IsCurrent == true)
            return;

        Cancel();
        clientReveal = new ClientReveal();
        Revealed = true;
        ShowMessage("Started");
    }

    public static void ClearLocalMap()
    {
        if (!CanReveal || Main.Map == null)
            return;

        Cancel();
        lock (MapHelper.IOLock)
            Main.Map.Clear();
        MapRevealSystem.RequestRebuild();
        ShowMessage("Cleared");
    }

    internal static void Cancel()
    {
        ClientReveal previous = clientReveal;
        clientReveal = null;
        Revealed = false;

        if (previous?.IsCurrent == true && previous.DownloadsTerrain)
            MapRevealNetHandler.Cancel(previous.Operation);
    }

    internal static void Reject(Guid operation)
    {
        if (clientReveal?.Operation != operation || !clientReveal.IsCurrent)
            return;

        Cancel();
        ShowMessage("Rejected");
    }

    internal static void MapLoaded(WorldMap map)
    {
        ClientReveal previous = clientReveal;
        if (previous?.IsCurrent != true || !ReferenceEquals(previous.Map, map))
            return;

        // Another mod can load its character's map after joining. Restart against that data.
        Cancel();
        clientReveal = new ClientReveal();
        Revealed = true;
        MapRevealSystem.RequestRebuild();
    }

    internal static void UpdateOperation()
    {
        ClientReveal job = clientReveal;
        if (job == null)
            return;

        if (!job.IsCurrent || !CanReveal || !Main.mapEnabled)
        {
            Cancel();
            return;
        }

        if (job.IsReplay && (ReplayPlayback.IsSeeking || !ReplayPlayback.HasEnteredReplayWorld))
            return;

        if (job.Section < job.SectionCount && !job.SectionAvailable && job.DownloadsTerrain)
        {
            if (job.Section >= job.RequestedThrough)
            {
                job.RequestedThrough = Math.Min(job.Section + MapRevealNetHandler.SectionsPerBatch, job.SectionCount);
                job.RequestedAt = Environment.TickCount64;
                MapRevealNetHandler.RequestSections(job.Operation, job.WorldId, job.WorldGuid, job.Section);
            }
            else if (Environment.TickCount64 - job.RequestedAt >= ReceiveTimeoutMilliseconds)
            {
                Cancel();
                ShowMessage("TimedOut");
            }

            return;
        }

        if (!Monitor.TryEnter(MapHelper.IOLock))
            return;

        try
        {
            job.Advance();
            int progress = job.Section * 4 / job.SectionCount;

            if (job.Section == job.SectionCount)
            {
                job.Map.ClearEdges();
                MapRevealSystem.RequestRebuild();
                job.Map.Save();
                Cancel();
                Revealed = true;
                if (job.MissingReplaySections > 0)
                    ShowMessage("IncompleteRecording", job.MissingReplaySections);
                else
                    ShowMessage("Completed");
            }
            else if (progress > job.Progress)
            {
                job.Progress = progress;
                ShowMessage("Progress", progress * 25);
            }
        }
        catch (Exception error)
        {
            Cancel();
            Log.Error($"Map reveal failed: {error}");
            ShowMessage("Failed");
        }
        finally
        {
            Monitor.Exit(MapHelper.IOLock);
        }
    }

    private static void ShowMessage(string key, params object[] args)
    {
        string text = Loc.Get("ReplayHud.MapReveal." + key, args);
        Log.Info(text);
        Main.NewText(text, Color.White);
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class MapRevealSystem : ModSystem
{
    private const int SectionsPerFrame = 4;
    private static readonly Queue<Point> redrawSections = new();
    private static readonly HashSet<Point> queuedSections = new();
    private static bool rebuildPending;
    private static int sectionX;
    private static int sectionY;
    private TimeSpan? lastUpdateTime;

    public override void Load()
    {
        On_Main.DrawToMap += HookDrawToMap;
        On_WorldMap.Load += HookLoadMap;
        ReplayPlaybackEvents.ReplayStateReset += OnReplayStateReset;
    }

    public override void Unload()
    {
        On_Main.DrawToMap -= HookDrawToMap;
        On_WorldMap.Load -= HookLoadMap;
        ReplayPlaybackEvents.ReplayStateReset -= OnReplayStateReset;
        Reset();
    }

    public override void OnWorldLoad() => Reset();
    public override void OnWorldUnload() => Reset();
    public override void PreSaveAndQuit() => MapRevealHelper.Cancel();

    public override void UpdateUI(GameTime gameTime)
    {
        // UI updates still run while playback is paused. Extra replay updates reuse this time.
        if (lastUpdateTime == gameTime.TotalGameTime)
            return;

        lastUpdateTime = gameTime.TotalGameTime;
        MapRevealHelper.UpdateOperation();
    }

    internal static void QueueSectionRedraw(int x, int y)
    {
        Point section = new(x, y);
        if (queuedSections.Add(section))
            redrawSections.Enqueue(section);
        Main.updateMap = true;
    }

    internal static void RequestRebuild()
    {
        redrawSections.Clear();
        queuedSections.Clear();
        rebuildPending = true;
        sectionX = sectionY = 0;
        Main.mapReady = false;
        Main.clearMap = true;
        Main.loadMap = true;
        Main.loadMapLock = true;
        Main.loadMapLastX = 0;
        Main.refreshMap = false;
    }

    private static void HookLoadMap(On_WorldMap.orig_Load orig, WorldMap self)
    {
        lock (MapHelper.IOLock)
            orig(self);
        MapRevealHelper.MapLoaded(self);
    }

    private static void HookDrawToMap(On_Main.orig_DrawToMap orig, Main self)
    {
        orig(self);

        if (!Main.mapEnabled)
            return;

        int sectionsX = (Main.maxTilesX + 199) / 200;
        int sectionsY = (Main.maxTilesY + 149) / 150;

        for (int remaining = SectionsPerFrame; remaining > 0; remaining--)
        {
            if (rebuildPending)
            {
                if (sectionY >= sectionsY)
                {
                    FinishRebuild();
                    continue;
                }

                self.DrawToMap_Section(sectionX, sectionY);
                if (++sectionX >= sectionsX)
                {
                    sectionX = 0;
                    sectionY++;
                }
            }
            else if (redrawSections.TryDequeue(out Point section))
            {
                queuedSections.Remove(section);
                self.DrawToMap_Section(section.X, section.Y);
            }
            else
                break;
        }

        if (rebuildPending && sectionY >= sectionsY)
            FinishRebuild();
    }

    private static void FinishRebuild()
    {
        rebuildPending = false;
        Main.mapReady = true;
        Main.clearMap = false;
        Main.loadMap = false;
        Main.loadMapLock = false;
        Main.refreshMap = false;
    }

    private static void OnReplayStateReset(ReplayStateResetEventArgs args) => Reset();

    private static void Reset()
    {
        MapRevealHelper.Cancel();
        if (rebuildPending)
            FinishRebuild();
        redrawSections.Clear();
        queuedSections.Clear();
        sectionX = sectionY = 0;
    }
}
