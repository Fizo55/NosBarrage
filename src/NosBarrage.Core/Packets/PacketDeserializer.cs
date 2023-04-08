using Autofac;
using Serilog;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Reflection;

namespace NosBarrage.Core.Packets;

public class PacketDeserializer
{
    private readonly ConcurrentDictionary<string, (Type handlerType, Type argumentType, ConstructorInfo constructor, MethodInfo handleAsyncMethod)> _handlerMappings = new();
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
            {
                var constructor = argumentType.GetConstructors().FirstOrDefault();
                var handleAsyncMethod = handlerType.GetMethod("HandleAsync");
                _handlerMappings[attribute.PacketName] = (handlerType, argumentType, constructor, handleAsyncMethod);
            }
        }
    }

    public async Task DeserializeAsync(string packet, Socket socket)
    {
        var packetMemory = packet.AsMemory();
        var spaceIndex = packetMemory.Span.IndexOf(' ');

        var command = spaceIndex >= 0 ? packetMemory[..spaceIndex].ToString() : packet;
        var arguments = spaceIndex >= 0 ? packetMemory[(spaceIndex + 1)..] : Memory<char>.Empty;

        if (_handlerMappings.TryGetValue(command, out var handlerMapping))
        {
            var handler = _serviceProvider.GetService(handlerMapping.handlerType);
            if (handler != null)
            {
                var args = DeserializeArguments(arguments, handlerMapping.argumentType, handlerMapping.constructor);

                if (args is not null)
                {
                    await (Task)handlerMapping.handleAsyncMethod.Invoke(handler, new[] { args, socket });
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

    private object? DeserializeArguments(ReadOnlyMemory<char> argumentsMemory, Type argumentType, ConstructorInfo constructor)
    {
        if (constructor == null)
        {
            _logger.Error($"Error (deserialize_arguments): no constructor found for '{argumentType.Name}'.");
            return null;
        }

        var constructorParameters = constructor.GetParameters();
        var parts = argumentsMemory.ToString().Split(' ', StringSplitOptions.TrimEntries);

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