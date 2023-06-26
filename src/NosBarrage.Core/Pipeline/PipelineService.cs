using NosBarrage.Core.Packets;
using NosBarrage.Shared.Configuration;
using Serilog;
using System.Net;
using System.Net.Sockets;

namespace NosBarrage.Core.Pipeline;

public class PipelineService : IPipelineService
{
    private readonly ILogger _logger;
    private readonly PacketDeserializer _packetDeserializer;
    private CancellationTokenSource _cts = null!;

    public PipelineService(ILogger logger, PacketDeserializer packetDeserializer)
    {
        _logger = logger;
        _packetDeserializer = packetDeserializer;
    }

    public async Task StartAsync(LoginConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var listener = new TcpListener(IPAddress.Any, configuration.Port);
        listener.Start();

        _logger.Debug($"Server started on port {configuration.Port}");

        while (!_cts.Token.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync();
            var session = new ClientSession(client, _logger, _packetDeserializer);
            _ = session.StartSessionAsync(_cts.Token);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}
