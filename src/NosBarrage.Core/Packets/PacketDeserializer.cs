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
            var constructor = handlerType.GetConstructors().FirstOrDefault();
            if (constructor == null)
                continue;

            var argumentType = GetArgumentType(handlerType);
            var handlerFactory = CreateHandlerFactory(constructor);

            _handlerFactories[attribute.PacketName] = handlerFactory;
            _argumentTypes[attribute.PacketName] = argumentType;
        }
    }

    private Delegate CreateHandlerFactory(ConstructorInfo constructor)
    {
        var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
        var constructorParams = constructor.GetParameters().Select(param =>
        {
            var getServiceCall = Expression.Call(serviceProviderParam, "GetService", null, Expression.Constant(param.ParameterType));
            return Expression.Convert(getServiceCall, param.ParameterType);
        }).ToArray();
        var newExp = Expression.New(constructor, constructorParams);
        var lambda = Expression.Lambda(typeof(Func<IServiceProvider, object>), newExp, serviceProviderParam).Compile();
        return lambda;
    }

    private Type GetArgumentType(Type handlerType)
    {
        var argumentTypes = handlerType.GetInterfaces()
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IPacketHandler<>))
            .Select(t => t.GetGenericArguments()[0]);

        return argumentTypes.FirstOrDefault()!;
    }

    public async Task Deserialize(string packet, Socket socket)
    {
        var parts = packet.Split(' ');
        var command = parts[0];

        if (_handlerFactories.TryGetValue(command, out var handlerFactory) && _argumentTypes.TryGetValue(command, out var argumentType))
        {
            var handler = ((Func<IServiceProvider, object>)handlerFactory)(_serviceProvider);
            var args = DeserializeArguments(parts.Skip(1).ToArray(), argumentType);
            var method = handler.GetType().GetMethod("HandleAsync")!;
            await (Task)method.Invoke(handler, new object[] { args!, socket });
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