using NosBarrage.Core.Packets;
using NosBarrage.Shared.Configuration;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace NosBarrage.Core.Pipeline;

public class PipelineService : IPipelineService
{
    private readonly ILogger _logger;
    private readonly PacketHandlerResolver _packetResolver;
    private CancellationTokenSource _cts = null!;

    public PipelineService(ILogger logger, PacketHandlerResolver packetResolver)
    {
        _logger = logger;
        _packetResolver = packetResolver;
    }

    public async Task StartAsync(IConfiguration configuration, bool isWorld = false, CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var listener = new TcpListener(IPAddress.Any, configuration.Port);
        listener.Start();

        _logger.Debug($"Server started on port {configuration.Port}");

        while (!_cts.Token.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync();
            var session = new ClientSession(client, _logger, _packetResolver, isWorld);
            _ = session.StartSessionAsync(_cts.Token);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}
