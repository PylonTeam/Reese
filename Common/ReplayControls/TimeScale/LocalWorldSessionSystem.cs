using System;
using Terraria.ModLoader;

namespace Reese.Common.ReplayControls.TimeScale;

[Autoload(Side = ModSide.Client)]
internal sealed class LocalWorldSessionSystem : ModSystem
{
    private long simulatedWorldTicks;

    public long SimulatedWorldTicks => simulatedWorldTicks;
    public TimeSpan TimeInWorld => TimeSpan.FromSeconds(simulatedWorldTicks / 60d);

    public override void OnWorldLoad()
    {
        simulatedWorldTicks = 0L;
    }

    public override void OnWorldUnload()
    {
        simulatedWorldTicks = 0L;
    }

    public override void PreSaveAndQuit()
    {
        simulatedWorldTicks = 0L;
    }

    public void AdvanceWorldTick()
    {
        simulatedWorldTicks++;
    }

    public static string FormatElapsedPrecise(TimeSpan elapsed)
    {
        elapsed = elapsed.Duration();

        if (elapsed.TotalHours >= 1d)
            return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds:00}";

        if (elapsed.TotalMinutes >= 1d)
            return $"{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds:000}";

        return $"{elapsed.Seconds:00}.{elapsed.Milliseconds:000}";
    }
}