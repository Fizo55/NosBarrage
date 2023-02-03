using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection;

namespace NosBarrage.Core.Packets;

public class PacketDeserializer
{
    private readonly ConcurrentDictionary<string, Type> _handlerTypes = new();

    public PacketDeserializer(Assembly assembly)
    {
        LoadPacketHandlers(assembly);
    }

    private void LoadPacketHandlers(Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IPacketHandler)) && t.GetCustomAttributes(typeof(PacketHandlerAttribute), false).Length > 0);

        foreach (var handlerType in handlerTypes)
        {
            var attribute = (PacketHandlerAttribute)handlerType.GetCustomAttribute(typeof(PacketHandlerAttribute), false)!;
            _handlerTypes[attribute.PacketName] = handlerType;
        }
    }

    public void Deserialize(string packet, Socket socket)
    {
        var parts = packet.Split(' ');
        var command = parts[0];

        if (_handlerTypes.TryGetValue(command, out var handlerType))
        {
            var args = parts.Skip(1).ToArray();
            var handler = (IPacketHandler)Activator.CreateInstance(handlerType)!;
            handler.HandleAsync(args, socket);
            return;
        }

        Console.WriteLine("No handler found");
    }
}