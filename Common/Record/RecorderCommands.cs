using Terraria.Localization;

namespace Reese.Common.Record;

public class RecordCommand : ModCommand
{
    public override string Command => "record";
    public override string Description => "Start recording a Reese replay";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var rec = ModContent.GetInstance<Recorder>();

        if (rec.IsRecording)
        {
            caller.Reply("[Reese] Already recording!");
            return;
        }

        rec.StartRecording();
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
        var rec = ModContent.GetInstance<Recorder>();

        if (!rec.IsRecording)
        {
            caller.Reply("[Reese] No recording is currently active.");
            return;
        }

        rec.StopRecording(NetworkText.FromKey("Mods.Reese.StopRecordCommand.Reason"));
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
