using Reese.Common.Replayer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Reese.Common.MainMenu;

internal sealed class ReplayCatalogService
{
    public static ReplayCatalogService Shared { get; } = new();

    private const int CacheVersion = 1;
    private const string CacheFileName = ".reese-replay-cache.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly object sync = new();
    private readonly Dictionary<string, CacheEntry> cache = new(StringComparer.OrdinalIgnoreCase);

    private ReplayMetadata[] currentEntries = [];
    private bool cacheLoaded;
    private bool cacheDirty;

    private static string CachePath => Path.Combine(ReplayPaths.GetFolder(), CacheFileName);

    public ReplayCatalogLoadResult Load(string directory)
    {
        var watch = Stopwatch.StartNew();
        Snapshot[] snapshots = Enumerate(directory);
        ReplayMetadata[] entries = new ReplayMetadata[snapshots.Length];
        List<(int Index, Snapshot Snapshot)> misses = [];
        int cacheHits = FillFromCache(snapshots, entries, misses);
        ConcurrentBag<ReadDiagnostic> diagnostics = ReadMissing(entries, misses);

        SaveCache(PruneTo(snapshots.Select(x => x.FullPath)));

        ReplayMetadata[] loadedEntries = entries.Where(x => x != null).ToArray();
        SetCurrent(loadedEntries);

        watch.Stop();
        Log.Info($"Loaded {loadedEntries.Length} replays in {watch.ElapsedMilliseconds} ms.");

        foreach (ReadDiagnostic diagnostic in diagnostics.OrderBy(x => x.Index))
            Log.Info($"[{diagnostic.Index + 1}] {diagnostic.FileName} loaded in {diagnostic.ElapsedMilliseconds} ms. Top 3 metadata fields: {diagnostic.TopFields}");

        return new ReplayCatalogLoadResult(loadedEntries, snapshots.Length, cacheHits, misses.Count, watch.ElapsedMilliseconds);
    }

    public ReplayMetadata[] GetCachedEntries(string directory)
    {
        ReplayMetadata[] entries = Enumerate(directory)
            .Select(snapshot => TryGetCached(snapshot, out ReplayMetadata metadata) ? metadata : null)
            .Where(metadata => metadata != null)
            .ToArray();

        if (entries.Length > 0)
            SetCurrent(entries);

        return entries;
    }

    public ReplayMetadata[] GetCurrentEntries()
    {
        lock (sync)
            return currentEntries.ToArray();
    }

    public bool TryGetFlags(string path, out ReplayFileFlags flags)
    {
        lock (sync)
        {
            ReplayMetadata metadata = currentEntries.FirstOrDefault(x => SamePath(x.FullPath, path));
            if (metadata != null)
            {
                flags = metadata.Flags;
                return true;
            }
        }

        if (Snapshot.TryFromPath(path, out Snapshot snapshot) && TryGetCached(snapshot, out ReplayMetadata cachedMetadata))
        {
            flags = cachedMetadata.Flags;
            return true;
        }

        flags = ReplayFileFlags.None;
        return false;
    }

    public void UpdateFlags(string path, ReplayFileFlags flags)
    {
        if (!Snapshot.TryFromPath(path, out Snapshot snapshot))
            return;

        Update(path, entry => WithSnapshot(entry, snapshot, flags));
        SaveCache();
    }

    public void Remove(string path)
    {
        lock (sync)
        {
            LoadCache();
            cacheDirty |= cache.Remove(path);
            currentEntries = currentEntries.Where(x => !SamePath(x.FullPath, path)).ToArray();
        }

        SaveCache();
    }

    public void Move(string oldPath, string newPath)
    {
        if (!Snapshot.TryFromPath(newPath, out Snapshot snapshot))
        {
            Remove(oldPath);
            return;
        }

        lock (sync)
        {
            LoadCache();
            ReplayMetadata metadata = currentEntries.FirstOrDefault(x => SamePath(x.FullPath, oldPath));
            ReplayMetadata movedMetadata;

            if (cache.Remove(oldPath, out CacheEntry entry))
                movedMetadata = WithSnapshot(entry.Metadata, snapshot);
            else if (metadata != null)
                movedMetadata = WithSnapshot(metadata, snapshot);
            else
                movedMetadata = Read(snapshot, -1, out _);

            cache[snapshot.FullPath] = CacheEntry.From(snapshot, movedMetadata);
            cacheDirty = true;
            currentEntries = currentEntries
                .Where(x => !SamePath(x.FullPath, oldPath))
                .Append(movedMetadata)
                .OrderByDescending(x => x.DateCreated)
                .ToArray();
        }

        SaveCache();
    }

    private int FillFromCache(Snapshot[] snapshots, ReplayMetadata[] entries, List<(int, Snapshot)> misses)
    {
        int hits = 0;

        for (int i = 0; i < snapshots.Length; i++)
        {
            if (TryGetCached(snapshots[i], out ReplayMetadata metadata))
            {
                entries[i] = metadata;
                hits++;
            }
            else
            {
                misses.Add((i, snapshots[i]));
            }
        }

        return hits;
    }

    private ConcurrentBag<ReadDiagnostic> ReadMissing(ReplayMetadata[] entries, List<(int Index, Snapshot Snapshot)> misses)
    {
        ConcurrentBag<ReadDiagnostic> diagnostics = [];
        if (misses.Count == 0)
            return diagnostics;

        Parallel.ForEach(misses, new ParallelOptions { MaxDegreeOfParallelism = Math.Min(4, Math.Max(1, Environment.ProcessorCount / 2)) }, miss =>
        {
            try
            {
                ReplayMetadata metadata = Read(miss.Snapshot, miss.Index, out ReadDiagnostic diagnostic);
                entries[miss.Index] = metadata;
                Store(miss.Snapshot, metadata);
                diagnostics.Add(diagnostic);
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to build replay list entry for {Path.GetFileName(miss.Snapshot.FullPath)}: {e}");
            }
        });

        return diagnostics;
    }

    private static ReplayMetadata Read(Snapshot snapshot, int index, out ReadDiagnostic diagnostic)
    {
        Log.Info($"Reading {snapshot.FileName}...");

        var watch = Stopwatch.StartNew();
        List<FieldTiming> timings = [];

        uint ticks = 0;
        string world = null;
        string[] mods = null;
        ReplayFileFlags replayFlags = ReplayFileFlags.None;

        string replayName = Time("ReplayName", timings, () => EmptyToError(Path.GetFileNameWithoutExtension(snapshot.FileName)));
        bool hasSummary = Time("DurationTicks", timings, () => ReplayFile.TryReadCatalogInfo(snapshot.FullPath, out ticks, out world, out mods, out replayFlags));

        ReplayMetadata metadata = new()
        {
            FullPath = snapshot.FullPath,
            FileName = snapshot.FileName,
            ReplayName = replayName,
            WorldName = Time("WorldName", timings, () => EmptyToUnknown(world)),
            DurationTicks = hasSummary ? ticks : 0,
            DateCreated = Time("DateCreated", timings, () => snapshot.LastWriteTime),
            LastWriteTimeUtc = snapshot.LastWriteTimeUtc,
            SizeBytes = Time("SizeBytes", timings, () => snapshot.SizeBytes),
            ModNames = Time("ModNames", timings, () => mods),
            Flags = Time("Flags", timings, () => replayFlags)
        };

        watch.Stop();
        Log.Info($"Finished reading {snapshot.FileName}");
        diagnostic = new ReadDiagnostic(index, snapshot.FileName, watch.ElapsedMilliseconds, timings);
        return metadata;
    }

    private static T Time<T>(string name, List<FieldTiming> timings, Func<T> read)
    {
        var watch = Stopwatch.StartNew();
        T value = read();
        watch.Stop();
        timings.Add(new FieldTiming(name, watch.ElapsedMilliseconds));
        return value;
    }

    private bool TryGetCached(Snapshot snapshot, out ReplayMetadata metadata)
    {
        lock (sync)
        {
            LoadCache();

            if (cache.TryGetValue(snapshot.FullPath, out CacheEntry entry) && entry.Matches(snapshot))
            {
                metadata = WithSnapshot(entry.Metadata, snapshot);
                return true;
            }

            cacheDirty |= cache.Remove(snapshot.FullPath);
            metadata = null;
            return false;
        }
    }

    private void Store(Snapshot snapshot, ReplayMetadata metadata)
    {
        lock (sync)
        {
            LoadCache();
            cache[snapshot.FullPath] = CacheEntry.From(snapshot, metadata);
            cacheDirty = true;
        }
    }

    private void Update(string path, Func<ReplayMetadata, ReplayMetadata> update)
    {
        lock (sync)
        {
            LoadCache();

            if (cache.TryGetValue(path, out CacheEntry entry))
                cache[path] = CacheEntry.From(new Snapshot(path), update(entry.Metadata));

            currentEntries = currentEntries.Select(x => SamePath(x.FullPath, path) ? update(x) : x).ToArray();
            cacheDirty = true;
        }
    }

    private bool PruneTo(IEnumerable<string> paths)
    {
        HashSet<string> livePaths = paths.ToHashSet(StringComparer.OrdinalIgnoreCase);

        lock (sync)
        {
            LoadCache();
            string[] stalePaths = cache.Keys.Where(path => !livePaths.Contains(path)).ToArray();

            foreach (string path in stalePaths)
                cache.Remove(path);

            cacheDirty |= stalePaths.Length > 0;
            return cacheDirty;
        }
    }

    private void SaveCache(bool shouldSave = true)
    {
        if (!shouldSave)
            return;

        lock (sync)
        {
            LoadCache();
            if (!cacheDirty)
                return;

            try
            {
                Directory.CreateDirectory(ReplayPaths.GetFolder());
                File.WriteAllText(CachePath + ".tmp", JsonSerializer.Serialize(new CacheDocument { Entries = [.. cache.Values] }, JsonOptions));
                if (File.Exists(CachePath))
                    File.Delete(CachePath);
                File.Move(CachePath + ".tmp", CachePath);
                cacheDirty = false;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to save replay metadata cache: {e}");
            }
        }
    }

    private void LoadCache()
    {
        if (cacheLoaded)
            return;

        cacheLoaded = true;
        if (!File.Exists(CachePath))
            return;

        try
        {
            CacheDocument document = JsonSerializer.Deserialize<CacheDocument>(File.ReadAllText(CachePath), JsonOptions);
            if (document?.Version != CacheVersion || document.Entries == null)
                return;

            foreach (CacheEntry entry in document.Entries.Where(x => x?.Metadata != null && !string.IsNullOrWhiteSpace(x.FullPath)))
                cache[entry.FullPath] = entry;
        }
        catch (Exception e)
        {
            cache.Clear();
            Log.Warn($"Failed to read replay metadata cache: {e}");
        }
    }

    private static Snapshot[] Enumerate(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return [];

        Directory.CreateDirectory(directory);
        return Directory.EnumerateFiles(directory, "*.reese", SearchOption.TopDirectoryOnly)
            .Select(path => new Snapshot(path))
            .OrderByDescending(x => x.LastWriteTime)
            .ToArray();
    }

    private void SetCurrent(ReplayMetadata[] entries)
    {
        lock (sync)
            currentEntries = entries.ToArray();
    }

    private static ReplayMetadata WithSnapshot(ReplayMetadata metadata, Snapshot snapshot, ReplayFileFlags? flags = null)
    {
        return new ReplayMetadata
        {
            FullPath = snapshot.FullPath,
            FileName = snapshot.FileName,
            ReplayName = EmptyToError(Path.GetFileNameWithoutExtension(snapshot.FileName)),
            WorldName = EmptyToUnknown(metadata?.WorldName),
            DurationTicks = metadata?.DurationTicks ?? 0,
            DateCreated = snapshot.LastWriteTime,
            LastWriteTimeUtc = snapshot.LastWriteTimeUtc,
            SizeBytes = snapshot.SizeBytes,
            ModNames = metadata?.ModNames,
            Flags = flags ?? metadata?.Flags ?? ReplayFileFlags.None
        };
    }

    private static bool SamePath(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    private static string EmptyToUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
    private static string EmptyToError(string value) => string.IsNullOrWhiteSpace(value) ? "Error" : value.Trim();

    private readonly record struct FieldTiming(string Name, long Milliseconds);

    private readonly record struct ReadDiagnostic(int Index, string FileName, long ElapsedMilliseconds, IReadOnlyList<FieldTiming> Timings)
    {
        public string TopFields => string.Join(", ", Timings.OrderByDescending(x => x.Milliseconds).ThenBy(x => x.Name).Take(3).Select(x => $"{x.Name} ({x.Milliseconds} ms)"));
    }

    private readonly record struct Snapshot(string FullPath, string FileName, DateTime LastWriteTime, DateTime LastWriteTimeUtc, long SizeBytes)
    {
        public Snapshot(string path) : this(path, Path.GetFileName(path), File.GetLastWriteTime(path), File.GetLastWriteTimeUtc(path), new FileInfo(path).Length)
        {
        }

        public static bool TryFromPath(string path, out Snapshot snapshot)
        {
            snapshot = default;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            snapshot = new Snapshot(path);
            return true;
        }
    }

    private sealed class CacheDocument
    {
        public int Version { get; set; } = CacheVersion;
        public List<CacheEntry> Entries { get; set; } = [];
    }

    private sealed class CacheEntry
    {
        public string FullPath { get; set; }
        public long SizeBytes { get; set; }
        public long LastWriteTimeUtcTicks { get; set; }
        public ReplayMetadata Metadata { get; set; }

        public bool Matches(Snapshot snapshot) => SizeBytes == snapshot.SizeBytes && LastWriteTimeUtcTicks == snapshot.LastWriteTimeUtc.Ticks;

        public static CacheEntry From(Snapshot snapshot, ReplayMetadata metadata) => new()
        {
            FullPath = snapshot.FullPath,
            SizeBytes = snapshot.SizeBytes,
            LastWriteTimeUtcTicks = snapshot.LastWriteTimeUtc.Ticks,
            Metadata = metadata
        };
    }
}

internal readonly record struct ReplayCatalogLoadResult(
    ReplayMetadata[] Entries,
    int FileCount,
    int CacheHitCount,
    int LoadedCount,
    long ElapsedMilliseconds);
