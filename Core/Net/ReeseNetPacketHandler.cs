using System;
using System.IO;

namespace Reese.Core.Net;

internal static class ReeseNetPacketHandler
{
    internal static void HandlePacket(BinaryReader reader, int sender)
    {
        ReesePacketIdentifier messageType = (ReesePacketIdentifier)reader.ReadByte();
        switch (messageType)
        {
            case ReesePacketIdentifier.RequestToggleSpectateMode:
                Reese.Common.ReplaySpectate.SpectatorMode.SpectatorModeNetHandler.Receive(reader, sender);
                break;
            default:
                throw new Exception($"Unknown {nameof(ReesePacketIdentifier)}: {messageType}");
        }
    }
}
