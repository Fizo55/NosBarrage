using NosBarrage.Core.Packets;
using NosBarrage.Shared.Args.Login;
using Serilog;
using System.Net.Sockets;

namespace NosBarrage.PacketHandlers.Login;

[PacketHandler("NoS0575")]
public class NoS0575PacketHandler : IPacketHandler<NoS0575PacketArgs>
{
    private readonly ILogger _logger;

    public NoS0575PacketHandler(ILogger logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(NoS0575PacketArgs args, Socket socket)
    {
        _logger.Debug(args.Name);
        return Task.CompletedTask;
    }
}
