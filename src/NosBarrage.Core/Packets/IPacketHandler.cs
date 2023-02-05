using System.Net.Sockets;

namespace NosBarrage.Core.Packets;

public interface IPacketHandler<T>
{
    Task HandleAsync(T args, Socket socket);
}
