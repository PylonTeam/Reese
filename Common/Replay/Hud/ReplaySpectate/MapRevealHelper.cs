using Reese.Core.Utilities;
using Terraria.ID;

namespace Reese.Common.Replay.Hud.ReplaySpectate;

internal static class MapRevealHelper
{
    private enum MapRevealOperation
    {
        None,
        Reveal,
        Clear
    }

    private enum MapRevealStage
    {
        Idle,
        Initialize,
        RevealTiles,
        RebuildSections
    }

    private const int ProgressStepCount = 4;
    private const int TilesPerFrame = 100_000;
    private const int SectionsPerFrame = 4;

    private static MapRevealOperation operation;
    private static MapRevealStage stage;

    private static int tileX;
    private static int tileY;
    private static int processedTiles;
    private static int totalTiles;
    private static int nextTileProgressPercent;

    private static int sectionX;
    private static int sectionY;
    private static int maxSectionX;
    private static int maxSectionY;
    private static int processedSections;
    private static int totalSections;
    private static int nextSectionProgressPercent;

    private static bool loggedWaitingForMapRenderer;
    private static bool loggedWaitingForMapEnabled;

    public static bool Enabled => true;
    public static bool Revealed { get; private set; }

    public static void SetRevealed(bool revealed)
    {
        if (revealed)
            RevealLocalMap();
        else
            ClearLocalMap();
    }

    public static void RevealLocalMap()
    {
        BeginOperation(MapRevealOperation.Reveal);
    }

    public static void ClearLocalMap()
    {
        BeginOperation(MapRevealOperation.Clear);
    }

    internal static void Reset(bool resetRevealed = false)
    {
        operation = MapRevealOperation.None;
        stage = MapRevealStage.Idle;
        tileX = 0;
        tileY = 0;
        processedTiles = 0;
        totalTiles = 0;
        sectionX = 0;
        sectionY = 0;
        processedSections = 0;
        totalSections = 0;
        loggedWaitingForMapRenderer = false;
        loggedWaitingForMapEnabled = false;

        if (resetRevealed)
            Revealed = false;
    }

    internal static void UpdateOperation()
    {
        if (operation == MapRevealOperation.None || Main.dedServ)
            return;

        if (!Enabled || Main.gameMenu || Main.maxTilesX <= 0 || Main.maxTilesY <= 0)
        {
            Reset();
            return;
        }

        switch (stage)
        {
            case MapRevealStage.Initialize:
                InitializeOperation();
                break;
            case MapRevealStage.RevealTiles:
                RevealTileChunk();
                break;
        }
    }

    internal static void RebuildRenderedSections(Main self, On_Main.orig_DrawToMap_Section drawSection)
    {
        if (operation == MapRevealOperation.None || stage != MapRevealStage.RebuildSections)
            return;

        if (!Main.mapEnabled)
        {
            if (!loggedWaitingForMapEnabled)
            {
                loggedWaitingForMapEnabled = true;
                LogProgress(3, "Waiting for the map to be enabled before rebuilding rendered sections.");
            }

            return;
        }

        loggedWaitingForMapEnabled = false;

        if (drawSection is null)
        {
            if (!loggedWaitingForMapRenderer)
            {
                loggedWaitingForMapRenderer = true;
                LogProgress(3, "Waiting for the map renderer before rebuilding rendered sections.");
            }

            return;
        }

        loggedWaitingForMapRenderer = false;

        int budget = SectionsPerFrame;

        while (budget-- > 0)
        {
            if (sectionY >= maxSectionY)
            {
                FinishOperation();
                return;
            }

            drawSection(self, sectionX, sectionY);
            processedSections++;
            LogSectionProgress();

            sectionX++;

            if (sectionX >= maxSectionX)
            {
                sectionX = 0;
                sectionY++;
            }
        }
    }

    private static void BeginOperation(MapRevealOperation nextOperation)
    {
        if (!Enabled || Main.dedServ)
            return;

        Reset();

        operation = nextOperation;
        stage = MapRevealStage.Initialize;
        Revealed = nextOperation == MapRevealOperation.Reveal;

        LogProgress(1, nextOperation == MapRevealOperation.Reveal
            ? "Reveal map requested."
            : "Hide map requested.");
    }

    private static void InitializeOperation()
    {
        if (operation == MapRevealOperation.Reveal)
        {
            LoadMultiplayerMapIfNeeded();
            StartRevealTiles();
            return;
        }

        Main.Map.Clear();
        LogProgress(2, "Cleared local map data.");
        StartRenderedSectionRebuild();
    }

    private static void LoadMultiplayerMapIfNeeded()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        try
        {
            Main.Map.Load();
            LogProgress(2, "Loaded multiplayer map data before reveal.");
        }
        catch (System.Exception e)
        {
            Log.Chat($"2/{ProgressStepCount} Progress: Multiplayer map load failed before reveal: {e.Message}");
        }
    }

    private static void StartRevealTiles()
    {
        tileX = 0;
        tileY = 0;
        processedTiles = 0;
        totalTiles = Main.maxTilesX * Main.maxTilesY;
        nextTileProgressPercent = 10;
        stage = MapRevealStage.RevealTiles;

        LogProgress(2, $"Revealing local map tiles 0/{totalTiles}.");
    }

    private static void RevealTileChunk()
    {
        int budget = TilesPerFrame;

        while (budget-- > 0 && tileY < Main.maxTilesY)
        {
            Main.Map.Update(tileX, tileY, 255);

            processedTiles++;
            tileX++;

            if (tileX >= Main.maxTilesX)
            {
                tileX = 0;
                tileY++;
            }
        }

        LogTileProgress();

        if (tileY < Main.maxTilesY)
            return;

        LogProgress(2, $"Finished revealing local map tiles {processedTiles}/{totalTiles}.");
        StartRenderedSectionRebuild();
    }

    private static void StartRenderedSectionRebuild()
    {
        sectionX = 0;
        sectionY = 0;
        maxSectionX = (Main.maxTilesX + 199) / 200;
        maxSectionY = (Main.maxTilesY + 149) / 150;
        processedSections = 0;
        totalSections = maxSectionX * maxSectionY;
        nextSectionProgressPercent = 10;
        loggedWaitingForMapRenderer = false;
        loggedWaitingForMapEnabled = false;

        Main.mapReady = false;
        Main.clearMap = true;
        Main.loadMap = true;
        Main.loadMapLock = true;
        Main.loadMapLastX = 0;
        Main.refreshMap = false;

        stage = MapRevealStage.RebuildSections;

        LogProgress(3, $"Rebuilding rendered map sections 0/{totalSections}.");
    }

    private static void FinishOperation()
    {
        MapRevealOperation finishedOperation = operation;

        Main.mapReady = true;
        Main.clearMap = false;
        Main.loadMap = false;
        Main.loadMapLock = false;
        Main.refreshMap = false;

        Reset();

        LogProgress(4, finishedOperation == MapRevealOperation.Reveal
            ? "Reveal map complete."
            : "Hide map complete.");
    }

    private static void LogTileProgress()
    {
        if (totalTiles <= 0)
            return;

        int percent = processedTiles * 100 / totalTiles;

        while (percent >= nextTileProgressPercent && nextTileProgressPercent <= 90)
        {
            LogProgress(2, $"Revealed {nextTileProgressPercent}% of local map tiles.");
            nextTileProgressPercent += 10;
        }
    }

    private static void LogSectionProgress()
    {
        if (totalSections <= 0)
            return;

        int percent = processedSections * 100 / totalSections;

        while (percent >= nextSectionProgressPercent && nextSectionProgressPercent <= 90)
        {
            LogProgress(3, $"Rebuilt {processedSections}/{totalSections} rendered map sections ({nextSectionProgressPercent}%).");
            nextSectionProgressPercent += 10;
        }
    }

    private static void LogProgress(int step, string message)
    {
        Log.Chat($"{step}/{ProgressStepCount} Progress: {message}");
    }
}

[Autoload(Side = ModSide.Client)]
internal sealed class MapRevealSystem : ModSystem
{
    private static On_Main.orig_DrawToMap_Section drawToMapSection;

    public override void Load()
    {
        On_Main.DrawToMap += HookDrawToMap;
        On_Main.DrawToMap_Section += HookDrawToMapSection;
    }

    public override void Unload()
    {
        On_Main.DrawToMap -= HookDrawToMap;
        On_Main.DrawToMap_Section -= HookDrawToMapSection;

        drawToMapSection = null;
        MapRevealHelper.Reset(resetRevealed: true);
    }

    public override void OnWorldUnload()
    {
        MapRevealHelper.Reset(resetRevealed: true);
    }

    public override void PostUpdateEverything()
    {
        MapRevealHelper.UpdateOperation();
    }

    private static void HookDrawToMap(On_Main.orig_DrawToMap orig, Main self)
    {
        orig(self);
        MapRevealHelper.RebuildRenderedSections(self, drawToMapSection);
    }

    private static void HookDrawToMapSection(On_Main.orig_DrawToMap_Section orig, Main self, int x, int y)
    {
        drawToMapSection ??= orig;
        orig(self, x, y);
    }
}
