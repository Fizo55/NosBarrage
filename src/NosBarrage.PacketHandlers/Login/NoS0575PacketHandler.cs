using NosBarrage.Core.Packets;
using NosBarrage.Shared.Args.Login;
using System.Net.Sockets;

namespace NosBarrage.PacketHandlers.Login;

[PacketHandler("NoS0575")]
public class NoS0575PacketHandler : IPacketHandler<NoS0575PacketArgs>
{
    public Task HandleAsync(NoS0575PacketArgs args, Socket socket)
    {
        return Task.CompletedTask;
    }
}
