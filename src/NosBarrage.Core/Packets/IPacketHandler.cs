using System.Net.Sockets;

namespace NosBarrage.Core.Packets;

public interface IPacketHandler
{
    Task HandleAsync(string[] args, Socket socket);
}
