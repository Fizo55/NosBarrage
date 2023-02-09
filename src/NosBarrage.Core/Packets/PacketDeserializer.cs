using Autofac;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Reflection;

namespace NosBarrage.Core.Packets;

public class PacketDeserializer
{
    private readonly ConcurrentDictionary<string, Delegate> _handlerFactories = new();
    private readonly ConcurrentDictionary<string, Type> _argumentTypes = new();
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public PacketDeserializer(Assembly assembly, ILogger logger, IServiceProvider serviceProvider)
    {
        LoadPacketHandlers(assembly);
        _logger = logger;
        _serviceProvider = serviceProvider;
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
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
        var lambda = Expression.Lambda(lambdaType, newExp, serviceProviderParam).Compile();
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
            var handlerType = typeof(IPacketHandler<>).MakeGenericType(argumentType);
            var handler = ((Func<object>)handlerFactory)();
            var args = DeserializeArguments(parts.Skip(1).ToArray(), argumentType);
            var method = handlerType.GetMethod("HandleAsync")!;
            method.Invoke(handler, new object[] { args!, socket });
            return;
        }

        _logger.Error("error (deserialize) : packet handlers wasn't found");
    }

    private object? DeserializeArguments(string[] parts, Type argumentType)
    {
        var constructorParameters = argumentType.GetConstructors()[0].GetParameters();
        if (constructorParameters.Length != parts.Length)
        {
            _logger.Error("error (deserialize_arguments) : outdated packet");
            return null;
        }

        var types = new Type[constructorParameters.Length];
        for (int i = 0; i < constructorParameters.Length; i++)
        {
            types[i] = constructorParameters[i].ParameterType;
        }

        var constructor = argumentType.GetConstructor(types);
        if (constructor == null)
            return null;

        var arguments = new object[constructorParameters.Length];
        for (int i = 0; i < constructorParameters.Length; i++)
        {
            arguments[i] = Convert.ChangeType(parts[i], constructorParameters[i].ParameterType);
        }

        return constructor.Invoke(arguments);
    }
}