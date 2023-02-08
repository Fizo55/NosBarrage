using System.Net.Sockets;

namespace NosBarrage.Core.Packets;

public interface IPacketDeserializer
{
    void Deserialize(string packet, Socket socket);
}
