using NosBarrage.Shared.Configuration;
using Serilog;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace NosBarrage.Core.Pipeline;

public class PipelineService : IPipelineService
{
    private readonly Func<Socket, PipeReader, PipeWriter, CancellationToken, ValueTask> _clientConnected;
    private CancellationTokenSource _cts;
    private ILogger _logger;

    public PipelineService(ILogger logger,
        Func<Socket, PipeReader, PipeWriter, CancellationToken, ValueTask> clientConnected)
    {
        _logger = logger;
        _clientConnected = clientConnected;
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
            _ = HandleClientAsync(clientSocket, _cts.Token);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task HandleClientAsync(Socket clientSocket, CancellationToken cancellationToken)
    {
        using var networkStream = new NetworkStream(clientSocket, ownsSocket: true);
        var reader = PipeReader.Create(networkStream, new StreamPipeReaderOptions(leaveOpen: true));
        var writer = PipeWriter.Create(networkStream, new StreamPipeWriterOptions(leaveOpen: true));

        try
        {
            await _clientConnected(clientSocket, reader, writer, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        finally
        {
            await reader.CompleteAsync();
            await writer.CompleteAsync();
        }
    }
}
