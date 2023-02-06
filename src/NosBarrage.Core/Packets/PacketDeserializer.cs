using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Reflection;

namespace NosBarrage.Core.Packets;

public class PacketDeserializer
{
    private readonly ConcurrentDictionary<string, Delegate> _handlerFactories = new();
    private readonly ConcurrentDictionary<string, Type> _argumentTypes = new();

    public PacketDeserializer(Assembly assembly)
    {
        LoadPacketHandlers(assembly);
    }

    private void LoadPacketHandlers(Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPacketHandler<>)))
            .Where(t => t.GetCustomAttributes(typeof(PacketHandlerAttribute), false).Length > 0);

        foreach (var handlerType in handlerTypes)
        {
            var attribute = (PacketHandlerAttribute)handlerType.GetCustomAttribute(typeof(PacketHandlerAttribute), false)!;
            var constructor = handlerType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                continue;

            var argumentType = GetArgumentType(handlerType);
            var handlerFactory = CreateHandlerFactory(constructor, argumentType);

            _handlerFactories[attribute.PacketName] = handlerFactory;
            _argumentTypes[attribute.PacketName] = argumentType;
        }
    }

    private Delegate CreateHandlerFactory(ConstructorInfo constructor, Type argumentType)
    {
        var newExp = Expression.New(constructor);
        var lambdaType = typeof(Func<>).MakeGenericType(typeof(IPacketHandler<>).MakeGenericType(argumentType));
        var lambda = Expression.Lambda(lambdaType, newExp).Compile();
        return lambda;
    }

    private Type GetArgumentType(Type handlerType)
    {
        var argumentTypes = handlerType.GetInterfaces()
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IPacketHandler<>))
            .Select(t => t.GetGenericArguments()[0]);

        return argumentTypes.FirstOrDefault()!;
    }

    public void Deserialize(string packet, Socket socket)
    {
        var parts = packet.Split(' ');
        var command = parts[0];

        if (_handlerFactories.TryGetValue(command, out var handlerFactory) && _argumentTypes.TryGetValue(command, out var argumentType))
        {
            var handler = ((dynamic)handlerFactory)();
            var args = DeserializeArguments(parts.Skip(1).ToArray(), argumentType);
            var method = handler.GetType().GetMethod("HandleAsync");
            var genericMethod = method!.MakeGenericMethod(argumentType);
            genericMethod.Invoke(handler, new object[] { args!, socket });
            return;
        }

        Console.WriteLine("error (deserialize) : packet handlers wasn't found");
    }

    private object? DeserializeArguments(string[] parts, Type argumentType)
    {
        var constructor = argumentType.GetConstructor(parts.Select(p => typeof(string)).ToArray());
        if (constructor == null)
            return null;

        var arguments = parts.Select(p => (object)p).ToArray();
        return constructor.Invoke(arguments);
    }
}