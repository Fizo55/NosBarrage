using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Reflection;

namespace NosBarrage.Core.Packets;

public class PacketDeserializer
{
    private readonly ConcurrentDictionary<string, Func<IPacketHandler>> _handlerFactories = new ConcurrentDictionary<string, Func<IPacketHandler>>();

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
            var constructor = handlerType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                continue;

            var newExp = Expression.New(constructor);
            var lambda = Expression.Lambda<Func<IPacketHandler>>(newExp);
            _handlerFactories[attribute.PacketName] = lambda.Compile();
        }
    }

    public void Deserialize(string packet, Socket socket)
    {
        var parts = packet.Split(' ');
        var command = parts[0];

        if (_handlerFactories.TryGetValue(command, out var handlerFactory))
        {
            var handler = handlerFactory();
            var args = parts.Skip(1).ToArray();
            handler.HandleAsync(args, socket);
            return;
        }

        Console.WriteLine("error (deserialize) : packet handlers wasn't found");
    }
}