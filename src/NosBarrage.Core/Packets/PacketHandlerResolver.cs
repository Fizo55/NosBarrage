using Serilog;
using System.Reflection;

namespace NosBarrage.Core.Packets;

public class PacketHandlerResolver
{
    private readonly Dictionary<string, Func<string, object>> _deserializers = new();
    private readonly Dictionary<string, IPacketHandler<object>> _handlers = new();
    private readonly ILogger _logger;

    public PacketHandlerResolver(IEnumerable<IPacketHandler<object>> handlers, ILogger logger)
    {
        _logger = logger;
        foreach (var handler in handlers)
        {
            var type = handler.GetType();
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute != null)
            {
                _handlers[attribute.PacketName] = handler;
                _deserializers[attribute.PacketName] = CreateDeserializer(attribute.PacketType);
            }
        }
    }

    private Func<string, object> CreateDeserializer(Type type)
    {
        var properties = type.GetProperties()
            .Where(p => p.GetCustomAttribute<PacketPropertyAttribute>() != null)
            .OrderBy(p => p.GetCustomAttribute<PacketPropertyAttribute>()?.Order)
            .ToList();

        return payload =>
        {
            var args = payload.Split(' ');
            var obj = Activator.CreateInstance(type);

            for (int i = 0; i < properties.Count && i < args.Length; i++)
            {
                properties[i].SetValue(obj, Convert.ChangeType(args[i], properties[i].PropertyType));
            }

            return obj;
        };
    }

    public async Task HandlePacketAsync(string packet, ClientSession session)
    {
        var parts = packet.Split(new[] { ' ' }, 2);
        var command = parts[0];
        var payload = parts.Length > 1 ? parts[1] : "";

        if (_handlers.TryGetValue(command, out var handler) && _deserializers.TryGetValue(command, out var deserializer))
        {
            var data = deserializer(payload);
            await handler.HandleAsync(data, session);
        }
        else
        {
            _logger.Error($"Error: packet handler for '{command}' not found.");
        }
    }
}
