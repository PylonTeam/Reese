using Terraria.ModLoader;

namespace Reese;

// General FIXMEs in architecture and implementation
// FIXME: We don't handle how servers do updates well at all, because it requires Netplay.HasClients!
// FIXME: Oops, I changed order of operations when abstracting towards ReplayFile, now we get a value from
//        Main.GameUpdateCount too soon! it's contents is from the previous play state. it causes the replay to be
//        delayed by however long you last played.
// FIXME: You probably can't open chests! Could track the data and forge the proper packet responses.
// FIXME: Just playing on a server that's being recorded, NPCs seem to slide around?

public class Reese : Mod;