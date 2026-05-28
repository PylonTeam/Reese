using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Reese.Common.MainMenu;

public sealed class ReplayLaunchSession : IDisposable
{
    private readonly CancellationTokenSource cts = new();
    private int cancelled;

    public ReplayLaunchSession(int generation)
    {
        Generation = generation;
    }

    public int Generation { get; }
    public CancellationToken Token => cts.Token;
    public bool IsCancelled => Volatile.Read(ref cancelled) != 0 || cts.IsCancellationRequested;

    public void Cancel()
    {
        Interlocked.Exchange(ref cancelled, 1);
        cts.Cancel();
    }

    public void Dispose() => cts.Dispose();
}
