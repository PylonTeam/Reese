using Terraria.ModLoader;

namespace Reese;

// General FIXMEs in architecture and implementation
// FIXME: We don't handle how servers do updates well at all, because it requires Netplay.HasClients!
// FIXME: Oops, I changed order of operations when abstracting towards ReplayFile, now we get a value from
//        Main.GameUpdateCount too soon! it's contents is from the previous play state. it causes the replay to be
//        delayed by however long you last played.

public class Reese : Mod;