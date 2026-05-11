//using System;

//namespace Reese.Core.Debug;

//internal static class ProfileProbe
//{
//    private static int mainThreadId = -1;

//    [System.ThreadStatic]
//    private static int depth;

//    public static void MarkMainThread(string label = "main")
//    {
//        mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
//        Log.Debug($"[Profile] Marked {label} thread as #{mainThreadId}");
//        DumpThreadPool("thread snapshot");
//    }

//    public static ProfileScope Measure(string name, int warnMs = 10)
//    {
//        return new ProfileScope(name, warnMs);
//    }

//    public static void DumpThreadPool(string label)
//    {
//        System.Threading.ThreadPool.GetAvailableThreads(out int workerAvailable, out int ioAvailable);
//        System.Threading.ThreadPool.GetMaxThreads(out int workerMax, out int ioMax);

//        Log.Debug($"[Profile] {label}: thread={ThreadLabel()}, processors={System.Environment.ProcessorCount}, workers={workerAvailable}/{workerMax}, io={ioAvailable}/{ioMax}, gc0={System.GC.CollectionCount(0)}, gc1={System.GC.CollectionCount(1)}, gc2={System.GC.CollectionCount(2)}");
//    }

//    public readonly struct ProfileScope : System.IDisposable
//    {
//        private readonly string name;
//        private readonly int warnMs;
//        private readonly int startDepth;
//        private readonly long startAllocated;
//        private readonly long startMemory;
//        private readonly System.Diagnostics.Stopwatch watch;

//        public ProfileScope(string name, int warnMs)
//        {
//            this.name = name;
//            this.warnMs = warnMs;
//            startDepth = depth++;
//            startAllocated = System.GC.GetAllocatedBytesForCurrentThread();
//            startMemory = System.GC.GetTotalMemory(false);
//            watch = System.Diagnostics.Stopwatch.StartNew();

//            Log.Debug($"[Profile] {Indent(startDepth)}BEGIN {name} on {ThreadLabel()}");
//        }

//        public void Dispose()
//        {
//            watch.Stop();
//            depth = System.Math.Max(0, depth - 1);

//            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - startAllocated;
//            long memoryDelta = System.GC.GetTotalMemory(false) - startMemory;
//            string level = watch.ElapsedMilliseconds >= warnMs ? "SLOW" : "OK";

//            Log.Debug($"[Profile] {Indent(startDepth)}{level} {name}: {watch.ElapsedMilliseconds} ms, alloc={allocated / 1024f:0.0} KB, heapDelta={memoryDelta / 1024f:0.0} KB, on {ThreadLabel()}");
//        }
//    }

//    private static string ThreadLabel()
//    {
//        System.Threading.Thread thread = System.Threading.Thread.CurrentThread;
//        string main = thread.ManagedThreadId == mainThreadId ? "main" : "not-main";
//        string pool = thread.IsThreadPoolThread ? "pool" : "non-pool";
//        string name = string.IsNullOrWhiteSpace(thread.Name) ? "-" : thread.Name;

//        return $"#{thread.ManagedThreadId} {main} {pool} name={name}";
//    }

//    private static string Indent(int count)
//    {
//        return new string(' ', count * 2);
//    }

//    public static string ThreadInfo()
//    {
//        System.Threading.Thread thread = System.Threading.Thread.CurrentThread;
//        return $"#{thread.ManagedThreadId}, pool={thread.IsThreadPoolThread}";
//    }

//    public static string FormatBytes(long bytes)
//    {
//        return bytes >= 1024L * 1024L
//            ? $"{bytes / 1024d / 1024d:0.0} MB"
//            : $"{Math.Max(1, bytes / 1024L):N0} KB";
//    }
//}