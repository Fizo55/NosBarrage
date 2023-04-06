using NosBarrage.Core.Logger;
using NosBarrage.Core.Packets;
using Serilog;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace NosBarrage.Login;

public class ClientHandler
{
    private static readonly ILogger _logger = Logger.GetLogger();

    private readonly PacketDeserializer _packetDeserializer;

    public ClientHandler(PacketDeserializer packetDeserializer)
    {
        _packetDeserializer = packetDeserializer;
    }

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

                var packet = buffer.ToString(); // TODO : replace it
                await _packetDeserializer.Deserialize(packet, socket);
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