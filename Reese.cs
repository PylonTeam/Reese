using Reese.Common.Recorder;
using System.IO;

namespace Reese;

// General FIXMEs in architecture and implementation
// FIXME: Wait, the server never hibernates now and does dumb shit because our replay client will force Netplay.HasClients!
// FIXME: Graceful replay ending? handle errors and eof? what do?
// FIXME: Oops, I changed order of operations when abstracting towards ReplayFile, now we get a value from
//        Main.GameUpdateCount too soon! it's contents is from the previous play state. it causes the replay to be
//        delayed by however long you last played.
// FIXME: You probably can't open chests! Could track the data and forge the proper packet responses.
// FIXME: Just playing on a server that's being recorded, NPCs seem to slide around?
// FIXME: Something is wrong with our baseline, because we for sure see trees change style randomly in the middle of a replay -- proving that our WorldData packet is wrong probably?
// FIXME: At some point the left side of the world just ends! The right side is all good though.
//        I also saw tile rects get synced over inside of the void, which probably indicates that the server believes
//        the client has that section (because tile rects only happen for players that have synced that section). probably.
// FIXME: Obviously we need a better interface for interacting with a replay that isn't just dropping a player into the same world (allowing some influence)
// FIXME: Sleeping is fucked up, causes mispredictions, because the client needs EVERYONE sleeping to advance time, and the replay player isn't sleeping but has influence.

public class Reese : Mod
{
    public override object Call(params object[] args)
    {
        if (args is null || args.Length == 0 || args[0] is not string command)
            return null;

        switch (command)
        {
            case "StartRecording":
                ModContent.GetInstance<Recorder>().StartRecording();
                return true;

            case "StopRecording":
                // allow them to pass a reason, default to "Cross-mod call"
                string reason = args.Length > 1 && args[1] is string r ? r : "Cross-mod call";
                ModContent.GetInstance<Recorder>().StopRecording(reason);
                return true;
        }

        return null;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        ReesePacketType packetType = (ReesePacketType)reader.ReadByte();

        switch (packetType)
        {
            case ReesePacketType.RecorderStatus:
                RecorderStatus.Receive(reader);
                break;

            default:
                Log.Warn($"Unknown Reese packet type: {(byte)packetType}");
                break;
        }
    }
}