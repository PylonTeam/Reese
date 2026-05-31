using Terraria.ModLoader;

namespace Reese.Common.Recorder;

public class RecordCommand : ModCommand
{
    public override string Command => "record";
    public override string Description => "Start recording a Reese replay";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (RecorderStatus.IsRecording)
        {
            caller.Reply("[Reese] Already recording!");
            return;
        }

        ModContent.GetInstance<Recorder>().StartRecording();
        caller.Reply("[Reese] Recording started successfully.");
    }
}

public class StopRecordCommand : ModCommand
{
    public override string Command => "stoprecording";
    public override string Description => "Stop recording a Reese replay";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!RecorderStatus.IsRecording)
        {
            caller.Reply("[Reese] No recording is currently active.");
            return;
        }

        ModContent.GetInstance<Recorder>().StopRecording("Stopped via command");
        caller.Reply("[Reese] Recording stopped.");
    }
}

public class RecordStatusCommand : ModCommand
{
    public override string Command => "recordstatus";
    public override string Description => "Show the status of a Reese replay recording";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        RecorderStatus.PrintStatus(caller);
    }
}
