using Reese.Common.Replayer.ReplayEvents;
using System;

namespace Reese.Common.Recorder;

internal static class ReplayTimelineModCalls
{
    private const int MaxReplayEventStringLength = 4 * 1024;

    public static bool AddReplayTimelineEvent(object[] args)
    {
        if (!TryReadRequiredString(args, 1, "key", out string key) ||
            !TryReadRequiredString(args, 2, "text", out string text) ||
            !TryReadIconKind(args, 3, out ReplayEventIconKind iconKind) ||
            !TryReadInt(args, 4, out int iconId))
            return false;

        Recorder recorder = ModContent.GetInstance<Recorder>();
        if (recorder == null || !recorder.IsRecording)
            return false;

        ReplayTimelineRecorder.Add(new ReplayTimelineEvent(
            recorder.Ticks,
            ReplayEventCategory.Custom,
            key,
            text,
            iconKind,
            iconId));

        return true;
    }

    private static bool TryReadRequiredString(object[] args, int index, string name, out string value)
    {
        value = null;

        if (args.Length <= index || args[index] is not string text || string.IsNullOrWhiteSpace(text))
        {
            Log.Warn($"AddReplayTimelineEvent expected non-empty string {name} at argument {index}.");
            return false;
        }

        text = text.Trim();
        if (text.Length > MaxReplayEventStringLength)
        {
            Log.Warn($"AddReplayTimelineEvent {name} is too long ({text.Length} chars, max {MaxReplayEventStringLength}).");
            return false;
        }

        value = text;
        return true;
    }

    private static bool TryReadIconKind(object[] args, int index, out ReplayEventIconKind iconKind)
    {
        iconKind = ReplayEventIconKind.None;

        if (args.Length <= index || args[index] == null)
            return true;

        object value = args[index];
        if (value is ReplayEventIconKind typedIconKind)
        {
            iconKind = typedIconKind;
            return IsValidIconKind(iconKind);
        }

        if (value is string text)
        {
            if (!Enum.TryParse(text, ignoreCase: true, out iconKind))
            {
                Log.Warn($"AddReplayTimelineEvent icon kind \"{text}\" is invalid.");
                return false;
            }

            return IsValidIconKind(iconKind);
        }

        if (TryConvertInt(value, out int iconKindValue))
        {
            iconKind = (ReplayEventIconKind)iconKindValue;
            return IsValidIconKind(iconKind);
        }

        Log.Warn("AddReplayTimelineEvent expected icon kind to be ReplayEventIconKind, string, or integer.");
        return false;
    }

    private static bool TryReadInt(object[] args, int index, out int value)
    {
        value = 0;

        if (args.Length <= index || args[index] == null)
            return true;

        if (TryConvertInt(args[index], out value))
            return true;

        Log.Warn($"AddReplayTimelineEvent expected integer icon id at argument {index}.");
        return false;
    }

    private static bool TryConvertInt(object value, out int result)
    {
        switch (value)
        {
            case byte byteValue:
                result = byteValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case uint uintValue when uintValue <= int.MaxValue:
                result = (int)uintValue;
                return true;
            case long longValue when longValue is >= int.MinValue and <= int.MaxValue:
                result = (int)longValue;
                return true;
            case string text when int.TryParse(text, out int parsed):
                result = parsed;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static bool IsValidIconKind(ReplayEventIconKind iconKind)
    {
        if (Enum.IsDefined(iconKind))
            return true;

        Log.Warn($"AddReplayTimelineEvent icon kind {(int)iconKind} is invalid.");
        return false;
    }
}
