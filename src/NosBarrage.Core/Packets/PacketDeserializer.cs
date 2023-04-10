using Autofac;
using Microsoft.Extensions.Primitives;
using Serilog;
using System.Buffers;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Reflection;

namespace NosBarrage.Core.Packets;

public class PacketDeserializer
{
    private readonly Dictionary<string, (Type handlerType, Type argumentType, Func<object[], object> constructor, Func<object, object, Socket, Task> handleAsyncMethod)> _handlerMappings = new();
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
        var handlerTypes = assembly.GetTypes();

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces();
            var attribute = handlerType.GetCustomAttribute<PacketHandlerAttribute>();

            if (attribute != null && interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPacketHandler<>)))
            {
                var argumentType = handlerType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPacketHandler<>))?
                    .GetGenericArguments()[0];

                if (argumentType != null)
                {
                    var constructor = argumentType.GetConstructors().FirstOrDefault();
                    var objectArrayParameter = Expression.Parameter(typeof(object[]));
                    var constructorParameters = constructor.GetParameters().Select((p, i) => Expression.Convert(Expression.ArrayIndex(objectArrayParameter, Expression.Constant(i)), p.ParameterType)).ToArray();
                    var constructorLambda = Expression.Lambda<Func<object[], object>>(Expression.New(constructor, constructorParameters), objectArrayParameter).Compile();

                    var handleAsyncMethod = handlerType.GetMethod("HandleAsync");
                    var handlerParameter = Expression.Parameter(typeof(object));
                    var argsParameter = Expression.Parameter(typeof(object));
                    var socketParameter = Expression.Parameter(typeof(Socket));
                    var handleAsyncLambda = Expression.Lambda<Func<object, object, Socket, Task>>(Expression.Call(Expression.Convert(handlerParameter, handlerType), handleAsyncMethod, Expression.Convert(argsParameter, argumentType), socketParameter), handlerParameter, argsParameter, socketParameter).Compile();

                    _handlerMappings[attribute.PacketName] = (handlerType, argumentType, constructorLambda, handleAsyncLambda);
                }
            }
        }
    }

    public async Task DeserializeAsync(string packet, Socket socket)
    {
        var commandEnd = packet.IndexOf(' ');
        var command = commandEnd >= 0 ? packet[..commandEnd] : packet;

        if (_handlerMappings.TryGetValue(command, out var handlerMapping))
        {
            var handler = _serviceProvider.GetService(handlerMapping.handlerType);
            if (handler != null)
            {
                var args = DeserializeArguments(packet.AsSpan(commandEnd + 1), handlerMapping.argumentType, handlerMapping.constructor);

                if (args is not null)
                {
                    await handlerMapping.handleAsyncMethod(handler, args, socket);
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

    private object? DeserializeArguments(ReadOnlySpan<char> arguments, Type argumentType, Func<object[], object> constructor)
    {
        var properties = argumentType.GetProperties();
        var parts = new List<StringSegment>();

        int startIndex = 0;
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == ' ')
            {
                parts.Add(new StringSegment(arguments[startIndex..i].ToString()));
                startIndex = i + 1;
            }
        }
        if (startIndex < arguments.Length)
        {
            parts.Add(new StringSegment(arguments[startIndex..].ToString()));
        }

        var instance = constructor(Array.Empty<object>());

        for (int i = 0; i < properties.Length; i++)
            properties[i]?.SetValue(instance, Convert.ChangeType(parts[i].ToString(), properties[i].PropertyType));

        return instance;
    }
}
