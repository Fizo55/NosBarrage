namespace NosBarrage.Core.Packets;

public interface IPacketHandler<T>
{
    Task HandleAsync(T data, ClientSession session);
}