using Autofac;
using Serilog;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection;

namespace NosBarrage.Core.Packets;

public class PacketDeserializer
{
    private readonly ConcurrentDictionary<string, (Type handlerType, Type argumentType)> _handlerMappings = new();
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
            .Where(t => t.GetCustomAttribute<PacketHandlerAttribute>() is not null);

        foreach (var handlerType in handlerTypes)
        {
            var attribute = handlerType.GetCustomAttribute<PacketHandlerAttribute>();
            var argumentType = handlerType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPacketHandler<>))?
                .GetGenericArguments()[0];

            if (argumentType != null)
                _handlerMappings[attribute.PacketName] = (handlerType, argumentType);
        }
    }

    public async Task DeserializeAsync(string packet, Socket socket)
    {
        var parts = packet.Split(' ', StringSplitOptions.TrimEntries);
        var command = parts[0];

        if (_handlerMappings.TryGetValue(command, out var handlerMapping))
        {
            var handler = _serviceProvider.GetService(handlerMapping.handlerType);
            if (handler != null)
            {
                var args = DeserializeArguments(parts[1..], handlerMapping.argumentType);

                if (args is not null)
                {
                    var handleAsyncMethod = handlerMapping.handlerType.GetMethod("HandleAsync");
                    await (Task)handleAsyncMethod.Invoke(handler, new[] { args, socket });
                }
                else
                {
                    _logger.Error($"Error (deserialize): failed to deserialize arguments for packet '{command}'.");
                }
            }
            else
            {
                _logger.Error($"Error (deserialize): no handler found for packet '{command}'");
            }
        }
        else
        {
            _logger.Error($"Error (deserialize): packet handler for '{command}' not found.");
        }
    }

    private object? DeserializeArguments(string[] parts, Type argumentType)
    {
        var constructor = argumentType.GetConstructors().FirstOrDefault();
        if (constructor == null)
        {
            _logger.Error($"Error (deserialize_arguments): no constructor found for '{argumentType.Name}'.");
            return null;
        }

        var constructorParameters = constructor.GetParameters();
        if (constructorParameters.Length != parts.Length)
        {
            _logger.Error($"Error (deserialize_arguments): incorrect number of arguments for '{argumentType.Name}'.");
            return null;
        }

        var arguments = constructorParameters
            .Select((param, index) => Convert.ChangeType(parts[index], param.ParameterType))
            .ToArray();

        return constructor.Invoke(arguments);
    }
}