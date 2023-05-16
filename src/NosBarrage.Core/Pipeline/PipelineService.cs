using NosBarrage.Core.Cryptography;
using NosBarrage.Shared.Configuration;
using Serilog;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NosBarrage.Core.Pipeline;

public class PipelineService : IPipelineService
{
    private readonly ILogger _logger;
    private CancellationTokenSource _cts;

    public PipelineService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(LoginConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        using var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, configuration.Port));
        listener.Listen(128);

        _logger.Debug($"Server started on port {configuration.Port}");

        while (!_cts.Token.IsCancellationRequested)
        {
            var clientSocket = await listener.AcceptAsync();
            var session = new ClientSession(clientSocket, _logger);
            _ = session.HandleClientAsync(_cts.Token);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}
