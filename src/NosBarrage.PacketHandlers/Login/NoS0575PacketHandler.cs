using NosBarrage.Core.Packets;
using System.Net.Sockets;

namespace NosBarrage.PacketHandlers.Login;

[PacketHandler("NoS0575")]
public class NoS0575PacketHandler : IPacketHandler
{
    public Task HandleAsync(string[] args, Socket socket)
    {
        Console.WriteLine(args[1]);
        return Task.CompletedTask;
    }
}
