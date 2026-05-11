using Reese.Common.Replayer;
using Reese.Common.Replayer.ReplayHud;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Terraria.ID;

namespace Reese.Core.Debug;

#if DEBUG
/// <summary>
/// Layout / content list for the debug stats shown by DebugDrawer. Each stat group has a name, color, toggle function, and list of rows (strings).
/// </summary>
internal static class DebugDrawerStats
{
    internal static DebugDrawer.DebugStatGroup[] BuildStats()
    {
        List<DebugDrawer.DebugStatGroup> groups = [];

        AddRecorderStats(groups);
        AddReplayerStats(groups);
        AddClientNetplayStats(groups);
        AddLocalPlayerStats(groups);
        AddHudUiStats(groups);
        AddEntityStats(groups);
        AddMiscStats(groups);

        return [.. groups];
    }

    private static void AddRecorderStats(List<DebugDrawer.DebugStatGroup> groups)
    {
        List<string> rows = [];

        //rows.Add($"Active: {DebugRecorderDiagnostics.IsActive}");
        //rows.Add($"File: {GetFileNameOrNone(ReplaySession.CurrentPath)}");
        //rows.Add($"Age: {FormatTimeSpan(DebugRecorderDiagnostics.SessionAge)}");

        //rows.Add($"Tick: {DebugRecorderDiagnostics.CurrentTick}");
        //rows.Add($"Last write tick: {DebugRecorderDiagnostics.LastWriteTick}");
        //rows.Add($"Last delta: {DebugRecorderDiagnostics.LastTickDelta}");

        //rows.Add($"Blocks written: {DebugRecorderDiagnostics.BlocksWritten}");
        //rows.Add($"Packets written: {DebugRecorderDiagnostics.PacketsWritten}");
        //rows.Add($"Packet bytes: {DebugRecorderDiagnostics.PacketBytesWritten}");
        //rows.Add($"File bytes: {DebugRecorderDiagnostics.FileBytesWritten}");

        //rows.Add($"Bytes/sec: {DebugRecorderDiagnostics.BytesPerSecond:0.##}");
        //rows.Add($"Packets/sec: {DebugRecorderDiagnostics.PacketsPerSecond:0.##}");
        //rows.Add($"Blocks/sec: {DebugRecorderDiagnostics.BlocksPerSecond:0.##}");

        //rows.Add($"Baseline block: {DebugRecorderDiagnostics.BaselineBlockBytes}");
        //rows.Add($"Last block: {DebugRecorderDiagnostics.LastBlockBytes}");
        //rows.Add($"Max block: {DebugRecorderDiagnostics.MaxBlockBytes}");
        //rows.Add($"Zero-delta blocks: {DebugRecorderDiagnostics.ZeroDeltaBlocks}");

        //rows.Add($"Malformed packets: {DebugRecorderDiagnostics.MalformedPacketData}");
        //rows.Add($"Trailing bytes: {DebugRecorderDiagnostics.TrailingPacketBytes}");
        //rows.Add($"Top packet IDs: {DebugRecorderDiagnostics.GetTopMessages()}");

        //rows.Add($"Last event: {DebugRecorderDiagnostics.LastEvent}");
        //rows.Add($"Last warning: {DebugRecorderDiagnostics.LastWarning}");
        //rows.Add($"Last error: {DebugRecorderDiagnostics.LastError}");

        groups.Add(new DebugDrawer.DebugStatGroup("Debug Recorder Stats", new Color(255, 120, 120), () => DebugDrawer.ShowDebugRecorderStats, [.. rows]));
    }

    private static void AddReplayerStats(List<DebugDrawer.DebugStatGroup> groups)
    {
        List<string> rows = [];

        //rows.Add($"Active: {DebugReplayerDiagnostics.IsActive}");
        //rows.Add($"Socket active: {Replayer.IsPlaybackSocketActive}");
        //rows.Add($"File: {GetFileNameOrNone(ReplaySession.CurrentPath)}");
        //rows.Add($"Age: {FormatTimeSpan(DebugReplayerDiagnostics.SessionAge)}");

        //rows.Add($"Tick: {DebugReplayerDiagnostics.CurrentTick} / {DebugReplayerDiagnostics.DurationTicks} ({DebugReplayerDiagnostics.Progress * 100f:0.##}%)");
        //rows.Add($"Time: {FormatTicks(DebugReplayerDiagnostics.CurrentTick)} / {FormatTicks(DebugReplayerDiagnostics.DurationTicks)}");
        //rows.Add($"Ticks/sec: {DebugReplayerDiagnostics.TicksPerSecond:0.##}");

        //rows.Add($"Waiting: {DebugReplayerDiagnostics.WaitingForTick}");
        //rows.Add($"Waiting for tick: {DebugReplayerDiagnostics.WaitingForReplayTick}");
        //rows.Add($"Reached EOF: {DebugReplayerDiagnostics.ReachedEof}");

        //rows.Add($"Receive calls: {DebugReplayerDiagnostics.ReceiveCalls}");
        //rows.Add($"Zero-byte receives: {DebugReplayerDiagnostics.ZeroByteReceives}");
        //rows.Add($"Incoming packets: {DebugReplayerDiagnostics.IncomingPackets}");
        //rows.Add($"Incoming bytes: {DebugReplayerDiagnostics.IncomingPacketBytes}");
        //rows.Add($"Incoming malformed: {DebugReplayerDiagnostics.IncomingMalformedPacketData}");
        //rows.Add($"Top incoming IDs: {DebugReplayerDiagnostics.GetTopIncomingMessages()}");

        //rows.Add($"Ignored outgoing packets: {DebugReplayerDiagnostics.IgnoredOutgoingPackets}");
        //rows.Add($"Ignored outgoing malformed: {DebugReplayerDiagnostics.IgnoredOutgoingMalformedPacketData}");
        //rows.Add($"Last ignored outgoing: {DebugReplayerDiagnostics.LastIgnoredOutgoingSummary}");
        //rows.Add($"Top ignored outgoing IDs: {DebugReplayerDiagnostics.GetTopIgnoredOutgoingMessages()}");

        //rows.Add($"Seek count: {DebugReplayerDiagnostics.SeekCount}");
        //rows.Add($"Last seek target: {DebugReplayerDiagnostics.LastSeekTargetTick}");
        //rows.Add($"Reset-to-start count: {DebugReplayerDiagnostics.ResetToStartCount}");

        //rows.Add($"Last event: {DebugReplayerDiagnostics.LastEvent}");
        //rows.Add($"Last warning: {DebugReplayerDiagnostics.LastWarning}");
        //rows.Add($"Last error: {DebugReplayerDiagnostics.LastError}");

        groups.Add(new DebugDrawer.DebugStatGroup("Debug Replayer Stats", new Color(120, 220, 255), () => DebugDrawer.ShowDebugReplayerStats, [.. rows]));
    }

    private static void AddClientNetplayStats(List<DebugDrawer.DebugStatGroup> groups)
    {
        List<string> rows = [];

        rows.Add($"Netmode: {Main.netMode} ({NetmodeID.Search.GetName(Main.netMode)})");
        rows.Add($"Player Index: {Main.myPlayer}");
        //rows.Add($"myPlayer is record client: {Main.myPlayer == ReplaySession.RecordClientIndex}");
        //rows.Add($"Netplay disconnect: {Netplay.Disconnect}");
        //rows.Add($"Connection: {GetConnectionText()}");
        //rows.Add($"Connection socket: {GetSocketText()}");
        //rows.Add($"Connection status: {GetConnectionStatusText()}");

        groups.Add(new DebugDrawer.DebugStatGroup("Debug Netplay", new Color(120, 200, 255), () => DebugDrawer.ShowDebugClientNetplayStats, [.. rows]));
    }

    private static void AddLocalPlayerStats(List<DebugDrawer.DebugStatGroup> groups)
    {
        Player local = Main.LocalPlayer;
        List<string> rows = [];

        //rows.Add($"Local valid: {local?.active == true}");
        rows.Add($"Name: {local?.name ?? "<null>"}");
        //rows.Add($"Local whoAmI: {local?.whoAmI ?? -1}");
        //rows.Add($"ReplayMode.IsInReplayMode: {ReplayMode.IsInReplayMode(local)}");
        //rows.Add($"ReplayMode.IsInPlayerMode: {ReplayMode.IsInPlayerMode(local)}");
        //rows.Add($"active/dead/ghost: {local?.active} / {local?.dead} / {local?.ghost}");
        //rows.Add($"active/dead/ghost: {local?.active} / {local?.dead} / {local?.ghost}");
        //rows.Add($"respawnTimer: {local?.respawnTimer ?? -1}");
        //rows.Add($"Life: {local?.statLife ?? -1} / {local?.statLifeMax2 ?? -1}");
        //rows.Add($"Mana: {local?.statMana ?? -1} / {local?.statManaMax2 ?? -1}");
        //rows.Add($"difficulty/team/hostile: {local?.difficulty ?? -1} / {local?.team ?? -1} / {local?.hostile}");
        //rows.Add($"Position: {FormatVector2(local?.position ?? Vector2.Zero)}");
        //rows.Add($"Velocity: {FormatVector2(local?.velocity ?? Vector2.Zero)}");
        rows.Add($"Position: {FormatPoint(Utils.ToTileCoordinates(local?.position ?? Vector2.Zero))}");
        //rows.Add($"Direction: {local?.direction ?? 0}");
        //rows.Add($"Held item: {GetSelectedItemText(local)}");

        groups.Add(new DebugDrawer.DebugStatGroup("Debug Local Player", new Color(140, 255, 160), () => DebugDrawer.ShowDebugLocalPlayerStats, [.. rows]));
    }

    private static void AddHudUiStats(List<DebugDrawer.DebugStatGroup> groups)
    {
        ReplayHudSystem replayHudSystem = ModContent.GetInstance<ReplayHudSystem>();
        Player local = Main.LocalPlayer;

        List<string> rows = [];

        rows.Add($"Replay HUD open: {replayHudSystem?.IsReplayHudOpen() == true}");
        //rows.Add($"Config UI open: {ConfigHelper.IsAnyConfigUIOpen()}");
        //rows.Add($"drawingPlayerChat: {Main.drawingPlayerChat}");
        //rows.Add($"chatText length: {Main.chatText?.Length ?? 0}");
        //rows.Add($"playerInventory: {Main.playerInventory}");
        //rows.Add($"gameMenu: {Main.gameMenu}");
        //rows.Add($"ingameOptionsWindow: {Main.ingameOptionsWindow}");
        //rows.Add($"mouseInterface: {local?.mouseInterface}");
        //rows.Add($"hasFocus: {Main.hasFocus}");
        //rows.Add($"editSign/editChest: {Main.editSign} / {Main.editChest}");
        //rows.Add($"npcShop: {Main.npcShop}");
        //rows.Add($"npcChatText length: {Main.npcChatText?.Length ?? 0}");
        //rows.Add($"Mouse screen: {Main.mouseX}, {Main.mouseY}");
        //rows.Add($"Mouse world: {FormatVector2(Main.MouseWorld)}");
        //rows.Add($"Mouse tile: {FormatPoint(Utils.ToTileCoordinates(Main.MouseWorld))}");
        //rows.Add($"Screen world: {FormatVector2(Main.screenPosition)}");
        //rows.Add($"Screen size: {Main.screenWidth}x{Main.screenHeight}");

        groups.Add(new DebugDrawer.DebugStatGroup("Debug HUD", new Color(210, 160, 255), () => DebugDrawer.ShowDebugHudUiStats, [.. rows]));
    }

    private static void AddEntityStats(List<DebugDrawer.DebugStatGroup> groups)
    {
        List<string> rows = [];

        rows.Add($"Active players: {CountActivePlayers()}");
        rows.Add($"Active NPCs: {CountActiveNpcs()}");
        rows.Add($"Active projectiles: {CountActiveProjectiles()}");
        rows.Add($"Active items: {CountActiveItems()}");

        groups.Add(new DebugDrawer.DebugStatGroup("Debug Entities", new Color(255, 170, 120), () => DebugDrawer.ShowDebugEntityStats, [.. rows]));
    }

    private static void AddMiscStats(List<DebugDrawer.DebugStatGroup> groups)
    {
        List<string> rows = [];

        //rows.Add($"World: {Main.worldName} (ID {Main.worldID})");
        rows.Add($"World: {Main.worldName}");
        //rows.Add($"Seed: {WorldGen.currentWorldSeed}");
        //rows.Add($"Tiles: {Main.maxTilesX}x{Main.maxTilesY}");
        //rows.Add($"Spawn tile: {Main.spawnTileX}, {Main.spawnTileY}");
        //rows.Add($"GameMode: {Main.GameMode}");
        //rows.Add($"Hardmode: {Main.hardMode} | Expert: {Main.expertMode} | Master: {Main.masterMode}");
        rows.Add($"Hardmode: {Main.hardMode}");
        //rows.Add($"Day/time: day={Main.dayTime}, time={Main.time:0}");
        //rows.Add($"Raining: {Main.raining}, rainTime={Main.rainTime}, maxRaining={Main.maxRaining:0.###}");
        //rows.Add($"Halloween: {Main.halloween}");
        rows.Add($"Debugger: {Debugger.IsAttached}");

        groups.Add(new DebugDrawer.DebugStatGroup("Debug Misc", new Color(190, 190, 190), () => DebugDrawer.ShowDebugMiscStats, [.. rows]));
    }

    private static string GetFileNameOrNone(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? "<none>" : Path.GetFileName(path);
    }

    private static string GetReplayProgressText()
    {
        return "null";
        //if (Replayer.ActiveDurationTicks == 0)
        //    return "0%";

        //float progress = Replayer.CurrentTick / (float)Replayer.ActiveDurationTicks;
        //return $"{progress * 100f:0.##}%";
    }

    private static string FormatTicks(uint ticks)
    {
        TimeSpan span = TimeSpan.FromSeconds(ticks / 60d);
        return span.TotalHours >= 1d ? $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}" : $"{span.Minutes:00}:{span.Seconds:00}";
    }

    private static string GetReplayMetadataText()
    {
        return "null";

        //ReplayMetadata metadata = Replayer.ActiveMetadata;

        //if (metadata == null)
        //    return "<none>";

        //string player = string.IsNullOrWhiteSpace(metadata.PlayerName) ? "?" : metadata.PlayerName;
        //string world = string.IsNullOrWhiteSpace(metadata.WorldName) ? "?" : metadata.WorldName;

        //return $"player={player}, world={world}, duration={metadata.DurationTicks} ticks";
    }

    private static string GetConnectionText()
    {
        if (Netplay.Connection == null)
            return "<null>";

        return $"active={Netplay.Connection.IsActive}, state={Netplay.Connection.State}";
    }

    private static string GetSocketText()
    {
        if (Netplay.Connection?.Socket == null)
            return "<null>";

        return $"{Netplay.Connection.Socket.GetType().Name}, connected={Netplay.Connection.Socket.IsConnected()}";
    }

    private static string GetConnectionStatusText()
    {
        string status = Netplay.Connection?.StatusText;
        return string.IsNullOrWhiteSpace(status) ? "<empty>" : status;
    }

    private static string GetSelectedItemText(Player player)
    {
        if (player == null || player.selectedItem < 0 || player.selectedItem >= player.inventory.Length)
            return "<none>";

        Item item = player.inventory[player.selectedItem];

        if (item == null || item.IsAir)
            return $"{player.selectedItem}: <air>";

        return $"{player.selectedItem}: {item.Name} ({item.type}) x{item.stack}";
    }

    private static int CountActivePlayers()
    {
        int count = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (Main.player[i]?.active == true)
                count++;
        }

        return count;
    }

    private static int CountActiveNpcs()
    {
        int count = 0;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (Main.npc[i]?.active == true)
                count++;
        }

        return count;
    }

    private static int CountActiveProjectiles()
    {
        int count = 0;

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            if (Main.projectile[i]?.active == true)
                count++;
        }

        return count;
    }

    private static int CountActiveItems()
    {
        int count = 0;

        for (int i = 0; i < Main.maxItems; i++)
        {
            if (Main.item[i]?.active == true)
                count++;
        }

        return count;
    }
    // Formatters
    private static string FormatTimeSpan(TimeSpan span)
    {
        return span.TotalHours >= 1d
            ? $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes:00}:{span.Seconds:00}";
    }
    private static string FormatVector2(Vector2 value)
    {
        return $"{value.X:0.##}, {value.Y:0.##}";
    }

    private static string FormatPoint(Point value)
    {
        return $"{value.X}, {value.Y}";
    }
}
#endif