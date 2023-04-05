using NosBarrage.Core.Logger;
using NosBarrage.Core.Packets;
using Serilog;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace NosBarrage.Login;

public class ClientHandler
{
    private static readonly ILogger _logger = Logger.GetLogger();

    public async ValueTask HandleClientConnectedAsync(Socket socket, PipeReader reader, PipeWriter writer, CancellationToken cancellationToken)
    {
        _logger.Debug("Client connected");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                if (result.IsCompleted)
                {
                    await HandleClientDisconnectedAsync(socket);
                    break;
                }

                // TOOD : Process data
                reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }
        catch (OperationCanceledException)
        {
            // we don't care
        }
        finally
        {
            reader.Complete();
        }
    }

    public ValueTask HandleClientDisconnectedAsync(Socket socket)
    {
        _logger.Debug("Client disconnected");
        return ValueTask.CompletedTask;
    }
}