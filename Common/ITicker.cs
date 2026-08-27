namespace Reese.Common;

/// <summary>
/// Common interface for
/// <see cref="Replayer.Replayer"/>
/// and
/// <see cref="Recorder.Recorder"/>
/// Used for tracking ticks in both recording and replaying, so that we can display it in the UI and use it for syncing.
/// </summary>
public interface ITicker
{
    uint Tick { get; }
}
