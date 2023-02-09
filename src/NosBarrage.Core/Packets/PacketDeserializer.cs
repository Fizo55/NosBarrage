using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection;

namespace NosBarrage.Core.Packets;

public class PacketDeserializer
{
    private readonly ConcurrentDictionary<string, Type> _handlerTypes = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public PacketDeserializer(IServiceCollection services, ILogger logger)
    {
        LoadPacketHandlers(services);
        _serviceProvider = services.BuildServiceProvider();
        _logger = logger;
    }

    private void LoadPacketHandlers(IServiceCollection services)
    {
        var handlerTypes = typeof(PacketDeserializer).Assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPacketHandler<>)))
            .Where(t => t.GetCustomAttributes(typeof(PacketHandlerAttribute), false).Length > 0);

        foreach (var handlerType in handlerTypes)
        {
            var attribute = (PacketHandlerAttribute)handlerType.GetCustomAttribute(typeof(PacketHandlerAttribute), false)!;
            services.AddTransient(handlerType);
            _handlerTypes[attribute.PacketName] = GetArgumentType(handlerType);
        }
    }

    private Type GetArgumentType(Type handlerType)
    {
        var argumentTypes = handlerType.GetInterfaces()
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IPacketHandler<>))
            .Select(t => t.GetGenericArguments()[0]);

        return argumentTypes.FirstOrDefault()!;
    }

    public async Task DeserializeAsync(string packet, Socket socket)
    {
        var parts = packet.Split(' ');
        var command = parts[0];

        if (_handlerTypes.TryGetValue(command, out var argumentType))
        {
            var handlerType = typeof(IPacketHandler<>).MakeGenericType(argumentType);
            var handler = (IPacketHandler)_serviceProvider.GetRequiredService(handlerType);
            var args = DeserializeArguments(parts.Skip(1).ToArray(), argumentType);
            await handler.HandleAsync(args!, socket);
            return;
        }

        _logger.Error("error (deserialize) : packet handlers wasn't found");
    }

    private object? DeserializeArguments(string[] parts, Type argumentType)
    {
        var constructorParameters = argumentType.GetConstructors().FirstOrDefault()?.GetParameters();
        if (constructorParameters == null || constructorParameters.Length != parts.Length)
        {
            _logger.Error("error (deserialize_arguments) : invalid constructor parameters or parts count");
            return null;
        }

        var arguments = new object[constructorParameters.Length];
        for (int i = 0; i < constructorParameters.Length; i++)
        {
            try
            {
                arguments[i] = Convert.ChangeType(parts[i], constructorParameters[i].ParameterType);
            }
            catch (FormatException)
            {
                _logger.Error("error (deserialize_arguments) : invalid conversion for part {partIndex}", i);
                return null;
            }
        }

        return Activator.CreateInstance(argumentType, arguments);
    }
}