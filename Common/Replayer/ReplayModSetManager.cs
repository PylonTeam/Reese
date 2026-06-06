using Reese.Common.MainMenu;
using Reese.Core.Configs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Terraria.ModLoader.Core;

namespace Reese.Common.Replayer;

[Autoload(Side = ModSide.Client)]
internal sealed class ReplayModSetSystem : ModSystem
{
    public override void PostSetupContent()
    {
        if (Main.dedServ)
            return;

        ReplayModSetManager.OnPostSetupContent();
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;

        ReplayModSetManager.PostUpdate();
    }
}

internal static class ReplayModSetManager
{
    private const int SessionVersion = 1;
    private const string SessionFileName = "mod_session.json";
    private const string PhaseLaunchAfterReload = "LaunchAfterReload";
    private const string PhasePlayback = "Playback";
    private const string PhaseRestorePending = "RestorePending";
    private const string PhaseRestoreAfterReload = "RestoreAfterReload";
    private const int MainMenuId = 0;
    private const int ReloadModsMenuId = 10006; // tML Interface.reloadModsID.

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static bool restoreStarted;
    private static bool launchReloadRescueStarted;

    private static string SessionPath => Path.Combine(ReplayPaths.GetFolder(), SessionFileName);

    public static (bool ShouldContinue, string ForcedModMismatchReason) PrepareReplayModsOrContinue(string replayPath)
    {
        Log.Info($"Replay play requested: {Path.GetFileName(replayPath)}");

        bool tryLoadReplayMods = ModContent.GetInstance<ClientConfig>()?.TryLoadModsUsedInReplay ?? true;

        if (!ReplayFile.TryReadModBundle(replayPath, out ReplayModBundle bundle))
        {
            Log.Warn($"Replay {Path.GetFileName(replayPath)} has no embedded .tmod bundle; Reese cannot auto-prepare exact replay mods.");

            bool hasNameMismatch = HasNameMismatchWithoutBundle(ReplayMetadata.FromFile(replayPath));
            if (hasNameMismatch)
            {
                Log.Warn($"Replay {Path.GetFileName(replayPath)} has a mod mismatch but does not contain bundled .tmod files; playing with the current loaded mods.");

                if (!tryLoadReplayMods)
                    return ForceLoadWithCurrentMods("enabled mods differ from replay metadata");
            }

            return (true, null);
        }

        ReplayModSessionEntry[] requiredMods = ToSessionEntries(bundle.Mods);
        Log.Info($"Replay embedded mod manifest: {requiredMods.Length} mods: {DescribeSessionMods(requiredMods)}");
        Log.Info($"Current loaded replay-relevant mods: {DescribeCurrentMods()}");

        if (CurrentModSetMatches(requiredMods, out string currentMismatch))
        {
            Log.Info("Current loaded mod set already matches replay manifest; starting playback without reload.");
            return (true, null);
        }

        if (!tryLoadReplayMods)
            return ForceLoadWithCurrentMods(currentMismatch);

        Log.Info($"Current loaded mod set does not match replay manifest: {currentMismatch}. Preparing embedded replay mods.");

        ReplayModSetSession session = null;

        try
        {
            session = CreateSession(replayPath, bundle);
            Log.Info($"Replay mod session created. Cache={session.ReplayModCachePath}; previous modpack={session.PreviousModPackActive ?? "<none>"}; previous enabled={string.Join(", ", session.PreviousEnabledMods ?? [])}");

            ExtractBundle(session.ReplayModCachePath, bundle);
            session.Phase = PhaseLaunchAfterReload;
            SaveSession(session);

            ApplyReplayModSet(session);
            SetReplaySyncHeaders(session.Mods);
            RequestReload($"Preparing replay mods for {Path.GetFileNameWithoutExtension(replayPath)}...");
            return (false, null);
        }
        catch (Exception e)
        {
            RestorePreparationFailure(session);
            Log.Error($"Failed to prepare replay mods for {Path.GetFileName(replayPath)}: {e}");
            Main.statusText = "Failed to prepare replay mods";
            Main.menuMode = MainMenuId;
            return (false, null);
        }
    }

    private static (bool ShouldContinue, string ForcedModMismatchReason) ForceLoadWithCurrentMods(string mismatch)
    {
        string reason = string.IsNullOrWhiteSpace(mismatch) ? "unknown mod mismatch" : mismatch;
        Log.Warn($"TryLoadModsUsedInReplay is disabled; force-loading replay with the current mod set despite mismatch: {reason}");
        return (true, reason);
    }

    private static void RestorePreparationFailure(ReplayModSetSession session)
    {
        if (session == null)
            return;

        try
        {
            Log.Warn("Replay mod preparation failed after creating a session; restoring the previous enabled mod set.");
            ApplyPreviousModSet(session);
            DeleteSession();
        }
        catch (Exception restoreError)
        {
            Log.Warn($"Failed to restore previous mod set after replay preparation failure: {restoreError}");
        }
    }

    public static void OnPostSetupContent()
    {
        Main.QueueMainThreadAction(() =>
        {
            ReplayModSetSession session = LoadSession();
            if (session == null)
                return;

            Log.Info($"Replay mod session detected after mod setup. Phase={session.Phase}; replay={Path.GetFileName(session.ReplayPath)}; cache={session.ReplayModCachePath}");

            switch (session.Phase)
            {
                case PhaseLaunchAfterReload:
                    CompleteLaunchAfterReload(session);
                    break;
                case PhaseRestoreAfterReload:
                    CompleteRestoreAfterReload();
                    break;
                case PhaseRestorePending:
                    TryBeginRestore();
                    break;
            }
        });
    }

    public static void PostUpdate()
    {
        if (!Main.gameMenu)
            return;

        ReplayModSetSession session = LoadSession();
        switch (session?.Phase)
        {
            case PhaseLaunchAfterReload:
                TryRescueLaunchReload(session);
                break;
            case PhaseRestorePending:
                TryBeginRestore();
                break;
        }
    }

    public static void RequestRestoreAfterReplay(string reason)
    {
        ReplayModSetSession session = LoadSession();
        if (session?.Phase != PhasePlayback)
            return;

        Log.Info($"Replay mod set restore requested: {reason ?? "replay ended"}");
        session.Phase = PhaseRestorePending;
        SaveSession(session);

        if (!Main.gameMenu)
        {
            Log.Info("Replay ended in world; quitting to menu before restoring previous mod set.");
            WorldGen.JustQuit();
        }
        else
            TryBeginRestore();
    }

    private static void TryRescueLaunchReload(ReplayModSetSession session)
    {
        if (launchReloadRescueStarted || Main.menuMode == ReloadModsMenuId)
            return;

        launchReloadRescueStarted = true;
        Log.Warn($"Replay mod session is waiting for launch reload while still in menuMode {Main.menuMode}; re-requesting tML reload.");
        RequestReload($"Preparing replay mods for {Path.GetFileNameWithoutExtension(session.ReplayPath)}...");
    }

    private static ReplayModSetSession CreateSession(string replayPath, ReplayModBundle bundle)
    {
        string[] previousEnabledMods = ModLoader.EnabledMods?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        string previousModPackActive = ModOrganizer.ModPackActive;
        if (IsReplayCachePath(previousModPackActive))
        {
            Log.Warn($"Current active modpack is a Reese replay cache ({previousModPackActive}); treating previous user modpack as <none> for restore.");
            previousModPackActive = null;
        }

        return new ReplayModSetSession
        {
            Version = SessionVersion,
            ReplayPath = replayPath,
            PreviousEnabledMods = previousEnabledMods,
            PreviousModPackActive = previousModPackActive,
            ReplayModCachePath = GetReplayCachePath(replayPath),
            Mods = ToSessionEntries(bundle.Mods)
        };
    }

    private static ReplayModSessionEntry[] ToSessionEntries(IEnumerable<ReplayModFile> mods)
    {
        return mods?
            .Where(x => x != null && !ReplayModBundle.ShouldIgnoreMod(x.Name))
            .Select(ReplayModSessionEntry.From)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static void ExtractBundle(string cachePath, ReplayModBundle bundle)
    {
        Directory.CreateDirectory(cachePath);
        int extractedCount = 0;
        long extractedBytes = 0;

        Log.Info($"Extracting embedded replay mods to {cachePath}");

        foreach (ReplayModFile mod in bundle.Mods)
        {
            if (mod == null || ReplayModBundle.ShouldIgnoreMod(mod.Name))
                continue;

            if (!mod.HasPayload)
            {
                Log.Warn($"Replay bundle entry {mod.Name ?? "<unknown>"} has no payload; skipping extraction.");
                continue;
            }

            string destination = Path.Combine(cachePath, mod.FileName);
            string temp = destination + ".tmp";

            File.WriteAllBytes(temp, mod.Payload);

            if (File.Exists(destination))
                File.Delete(destination);

            File.Move(temp, destination);
            ValidateExtractedMod(destination, mod);

            extractedCount++;
            extractedBytes += mod.PayloadLength;
            Log.Info($"Extracted replay mod {mod.Name} v{mod.Version} ({ReplayModFile.FormatBytes(mod.PayloadLength)}, hash {mod.ShortHashText}) -> {destination}");
        }

        Log.Info($"Replay mod extraction complete: {extractedCount} .tmod files, {ReplayModFile.FormatBytes(extractedBytes)}.");
    }

    private static void ValidateExtractedMod(string path, ReplayModFile expected)
    {
        var tmodFile = new TmodFile(path);
        using (tmodFile.Open())
        {
            if (!string.Equals(tmodFile.Name, expected.Name, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Extracted replay mod file {path} is {tmodFile.Name}, expected {expected.Name}.");

            if (!string.Equals(tmodFile.Version?.ToString(), expected.Version, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Extracted replay mod {expected.Name} has version {tmodFile.Version}, expected {expected.Version}.");

            if (tmodFile.Hash == null || !tmodFile.Hash.SequenceEqual(expected.Hash))
                throw new InvalidDataException($"Extracted replay mod {expected.Name} has hash {ReplayModFile.ShortHash(tmodFile.Hash)}, expected {expected.ShortHashText}.");
        }
    }

    private static void ApplyReplayModSet(ReplayModSetSession session)
    {
        ModOrganizer.ModPackActive = session.ReplayModCachePath;

        string[] enabledMods =
        [
            .. session.Mods.Select(x => x.Name),
            "Reese"
        ];

        Log.Info($"Applying replay mod set. Active modpack={ModOrganizer.ModPackActive}; enabled={string.Join(", ", enabledMods)}");
        ApplyEnabledMods(enabledMods);
    }

    private static void ApplyPreviousModSet(ReplayModSetSession session)
    {
        string previousModPackActive = IsReplayCachePath(session.PreviousModPackActive)
            ? null
            : session.PreviousModPackActive;

        ModOrganizer.ModPackActive = string.IsNullOrWhiteSpace(previousModPackActive)
            ? null
            : previousModPackActive;

        Log.Info($"Restoring previous mod set. Active modpack={ModOrganizer.ModPackActive ?? "<none>"}; enabled={string.Join(", ", session.PreviousEnabledMods ?? [])}");
        ApplyEnabledMods(session.PreviousEnabledMods ?? []);
        ClearReplaySyncHeaders();
    }

    private static void ApplyEnabledMods(IEnumerable<string> modNames)
    {
        string[] enabledMods = modNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Log.Info($"Writing enabled mods: {string.Join(", ", enabledMods)}");
        ModLoader.DisableAllMods();

        foreach (string modName in enabledMods)
            ModLoader.EnableMod(modName);

        ModOrganizer.SaveEnabledMods();
        Log.Info("Enabled mod list saved.");
    }

    private static void CompleteLaunchAfterReload(ReplayModSetSession session)
    {
        restoreStarted = false;
        launchReloadRescueStarted = false;

        Log.Info($"Completing replay mod launch after reload. Expected={DescribeSessionMods(session.Mods)}; current={DescribeCurrentMods()}");

        if (!CurrentModSetMatches(session.Mods, out string mismatch))
        {
            Log.Error($"Replay mod load verification failed: {mismatch}");
            Main.statusText = $"Replay mod verification failed: {mismatch}";
            session.Phase = PhaseRestorePending;
            SaveSession(session);
            TryBeginRestore();
            return;
        }

        ClearReplaySyncHeaders();
        session.Phase = PhasePlayback;
        SaveSession(session);

        Log.Info($"Replay mod set loaded; launching replay {Path.GetFileName(session.ReplayPath)}.");
        ReplayActions.EnterReplayPrepared(session.ReplayPath);
    }

    private static void TryBeginRestore()
    {
        if (restoreStarted || !Main.gameMenu)
            return;

        ReplayModSetSession session = LoadSession();
        if (session?.Phase != PhaseRestorePending)
            return;

        restoreStarted = true;
        session.Phase = PhaseRestoreAfterReload;
        SaveSession(session);

        try
        {
            Log.Info($"Beginning replay mod restore reload for {Path.GetFileName(session.ReplayPath)}.");
            ApplyPreviousModSet(session);
            RequestReload("Restoring previous mods...");
        }
        catch (Exception e)
        {
            restoreStarted = false;
            Log.Error($"Failed to restore previous mod set: {e}");
            Main.statusText = "Failed to restore previous mods";
        }
    }

    private static void CompleteRestoreAfterReload()
    {
        ClearReplaySyncHeaders();
        DeleteSession();
        restoreStarted = false;
        launchReloadRescueStarted = false;
        Log.Info("Previous mod set restored after replay playback.");
    }

    private static bool CurrentModSetMatches(IReadOnlyList<ReplayModSessionEntry> requiredMods, out string mismatch)
    {
        mismatch = null;

        Dictionary<string, ReplayModSessionEntry> required = (requiredMods ?? [])
            .Where(x => x != null && !ReplayModBundle.ShouldIgnoreMod(x.Name))
            .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

        Mod[] currentMods = ModLoader.Mods
            .Where(x => x != null && !ReplayModBundle.ShouldIgnoreMod(x.Name))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (Mod mod in currentMods)
        {
            if (!required.TryGetValue(mod.Name, out ReplayModSessionEntry requiredMod))
            {
                mismatch = $"extra loaded mod {mod.Name}";
                return false;
            }

            byte[] currentHash = mod.File?.Hash;
            if (currentHash == null || !currentHash.SequenceEqual(requiredMod.Hash))
            {
                mismatch = $"mod {mod.Name} has the wrong hash";
                return false;
            }

            if (!string.Equals(mod.Version?.ToString(), requiredMod.Version, StringComparison.OrdinalIgnoreCase))
            {
                mismatch = $"mod {mod.Name} has version {mod.Version}, expected {requiredMod.Version}";
                return false;
            }
        }

        foreach (ReplayModSessionEntry requiredMod in required.Values)
        {
            if (currentMods.All(x => !string.Equals(x.Name, requiredMod.Name, StringComparison.OrdinalIgnoreCase)))
            {
                mismatch = $"missing mod {requiredMod.Name}";
                return false;
            }
        }

        return true;
    }

    private static bool HasNameMismatchWithoutBundle(ReplayMetadata metadata)
    {
        string[] replayMods = NormalizeModNames(metadata?.ModNames);
        string[] currentMods = NormalizeModNames(ModLoader.Mods.Select(x => x?.Name).ToArray());
        return replayMods.Length > 0 && !replayMods.SequenceEqual(currentMods, StringComparer.OrdinalIgnoreCase);
    }

    private static string[] NormalizeModNames(IEnumerable<string> modNames)
    {
        return modNames?
            .Where(x => !ReplayModBundle.ShouldIgnoreMod(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static void SetReplaySyncHeaders(IReadOnlyList<ReplayModSessionEntry> mods)
    {
        Type headerType = typeof(ModNet).GetNestedType("ModHeader", BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo field = typeof(ModNet).GetField("SyncModHeaders", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo netReloadActiveField = typeof(ModNet).GetField("NetReloadActive", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (headerType == null || field == null || netReloadActiveField == null)
            throw new InvalidOperationException("Could not access tModLoader ModNet sync headers.");

        Type listType = typeof(List<>).MakeGenericType(headerType);
        IList headers = (IList)Activator.CreateInstance(listType);
        ConstructorInfo constructor = headerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [typeof(string), typeof(System.Version), typeof(byte[])], null);

        if (constructor == null)
            throw new InvalidOperationException("Could not access tModLoader ModNet.ModHeader constructor.");

        foreach (ReplayModSessionEntry mod in mods ?? [])
            headers.Add(constructor.Invoke([mod.Name, mod.ParsedVersion, mod.Hash]));

        field.SetValue(null, headers);
        netReloadActiveField.SetValue(null, headers.Count > 0);

        Log.Info($"Replay ModNet sync headers set: {headers.Count} entries; NetReloadActive={headers.Count > 0}.");
    }

    private static void ClearReplaySyncHeaders()
    {
        try
        {
            SetReplaySyncHeaders([]);
        }
        catch (Exception e)
        {
            Log.Warn($"Could not clear replay sync headers: {e.Message}");
        }
    }

    private static void RequestReload(string statusText)
    {
        Log.Info($"Closing Reese menu UI before tML reload. Current menuMode={Main.menuMode}; gameMenu={Main.gameMenu}.");
        ModContent.GetInstance<MainMenuSystem>()?.CloseForReplayLaunch();
        Main.MenuUI.SetState(null);

        Log.Info($"Requesting tML mod reload via menuMode {ReloadModsMenuId}. Current menuMode={Main.menuMode}; gameMenu={Main.gameMenu}; status=\"{statusText}\"");
        Main.statusText = statusText;
        Main.menuMode = ReloadModsMenuId;
        Log.Info($"tML reload menuMode set. New menuMode={Main.menuMode}; Main.MenuUI state={Main.MenuUI.CurrentState?.GetType().Name ?? "<none>"}.");
    }

    private static string GetReplayCachePath(string replayPath)
    {
        FileInfo file = new(replayPath);
        string name = SanitizePathPart(Path.GetFileNameWithoutExtension(replayPath));
        string keySource = $"{file.FullName}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(keySource));
        string suffix = ReplayModFile.ToHex(hash)[..12];
        return Path.Combine(ReplayPaths.GetFolder(), "ModCache", $"{name}_{suffix}");
    }

    private static bool IsReplayCachePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string cacheRoot = Path.Combine(ReplayPaths.GetFolder(), "ModCache");

        try
        {
            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullCacheRoot = Path.GetFullPath(cacheRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return fullPath.Equals(fullCacheRoot, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(fullCacheRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(fullCacheRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string SanitizePathPart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Replay";

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidChar, '_');

        return value.Trim();
    }

    private static ReplayModSetSession LoadSession()
    {
        try
        {
            if (!File.Exists(SessionPath))
                return null;

            ReplayModSetSession session = JsonSerializer.Deserialize<ReplayModSetSession>(File.ReadAllText(SessionPath), JsonOptions);
            if (session?.Version != SessionVersion || string.IsNullOrWhiteSpace(session.ReplayPath))
                return null;

            return session;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to read replay mod session: {e}");
            return null;
        }
    }

    private static void SaveSession(ReplayModSetSession session)
    {
        Directory.CreateDirectory(ReplayPaths.GetFolder());
        File.WriteAllText(SessionPath, JsonSerializer.Serialize(session, JsonOptions));
        Log.Info($"Replay mod session saved. Phase={session.Phase}; path={SessionPath}");
    }

    private static void DeleteSession()
    {
        try
        {
            if (File.Exists(SessionPath))
            {
                File.Delete(SessionPath);
                Log.Info($"Replay mod session deleted: {SessionPath}");
            }
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to delete replay mod session: {e}");
        }
    }

    private static string DescribeSessionMods(IEnumerable<ReplayModSessionEntry> mods)
    {
        string[] entries = mods?
            .Where(x => x != null)
            .Select(x => $"{x.Name} v{x.Version}#{ShortHash(x.HashHex)}")
            .ToArray() ?? [];

        return entries.Length == 0 ? "<none>" : string.Join(", ", entries);
    }

    private static string DescribeCurrentMods()
    {
        string[] entries = ModLoader.Mods
            .Where(x => x != null && !ReplayModBundle.ShouldIgnoreMod(x.Name))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{x.Name} v{x.Version}#{ReplayModFile.ShortHash(x.File?.Hash)}")
            .ToArray();

        return entries.Length == 0 ? "<none>" : string.Join(", ", entries);
    }

    private static string ShortHash(string hashHex)
    {
        if (string.IsNullOrWhiteSpace(hashHex))
            return string.Empty;

        return hashHex.Length <= 12 ? hashHex : hashHex[..12];
    }

    private sealed class ReplayModSetSession
    {
        public int Version { get; set; } = SessionVersion;
        public string Phase { get; set; }
        public string ReplayPath { get; set; }
        public string PreviousModPackActive { get; set; }
        public string[] PreviousEnabledMods { get; set; } = [];
        public string ReplayModCachePath { get; set; }
        public ReplayModSessionEntry[] Mods { get; set; } = [];
    }

    private sealed class ReplayModSessionEntry
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string TModLoaderVersion { get; set; }
        public string HashHex { get; set; }

        public byte[] Hash => ReplayModFile.FromHex(HashHex);
        public System.Version ParsedVersion => System.Version.TryParse(Version, out System.Version version) ? version : new System.Version(0, 0);

        public static ReplayModSessionEntry From(ReplayModFile mod)
        {
            return new ReplayModSessionEntry
            {
                Name = mod.Name,
                DisplayName = mod.DisplayName,
                Version = mod.Version,
                TModLoaderVersion = mod.TModLoaderVersion,
                HashHex = mod.HashText
            };
        }
    }
}
