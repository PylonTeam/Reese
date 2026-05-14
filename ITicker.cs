namespace Reese;

/// <summary>
/// Common interface for 
/// <see cref="Common.Replayer.Replayer"/>
/// and
/// <see cref="Common.Recorder.Recorder"/>
/// Used for tracking ticks in both recording and replaying, so that we can display it in the UI and use it for syncing.
/// </summary>
public interface ITicker
{
    uint Ticks { get; }
}