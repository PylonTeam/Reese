using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reese;

public class RecordCommand : ModCommand
{
    public override string Command => "record";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        ModContent.GetInstance<Recorder>().StartRecordingPublic();
    }
}

public class StopRecordCommand : ModCommand
{
    public override string Command => "stoprecord";
    public override CommandType Type => CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        ModContent.GetInstance<Recorder>().StopRecordingPublic();
    }
}
