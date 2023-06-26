using NosBarrage.Core.Cryptography;
using NosBarrage.Core.Packets;
using Serilog;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;

namespace NosBarrage.Core;

public class ClientSession
{
    private readonly Socket _clientSocket;
    private readonly ILogger _logger;
    private readonly PacketDeserializer _packetDeserializer;
    private NetworkStream _networkStream;
    private PipeWriter _writer;

    public ClientSession(Socket clientSocket, ILogger logger, PacketDeserializer packetDeserializer)
    {
        _clientSocket = clientSocket;
        _logger = logger;
        _packetDeserializer = packetDeserializer;
        _networkStream = new NetworkStream(_clientSocket, ownsSocket: true);
        _writer = PipeWriter.Create(_networkStream, new StreamPipeWriterOptions(leaveOpen: true));
    }

    public async ValueTask HandleClientConnectedAsync(Socket socket, PipeReader reader, CancellationToken cancellationToken)
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

                var loginDecrypt = LoginCryptography.LoginDecrypt(buffer.ToArray());
                var packet = Encoding.UTF8.GetString(loginDecrypt);
                await _packetDeserializer.DeserializeAsync(packet, this);
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
        socket.Close();
        return ValueTask.CompletedTask;
    }

    public async Task HandleClientAsync(CancellationToken token)
    {
        using var networkStream = new NetworkStream(_clientSocket, ownsSocket: true);
        var reader = PipeReader.Create(networkStream, new StreamPipeReaderOptions(leaveOpen: true));
        var writer = PipeWriter.Create(networkStream, new StreamPipeWriterOptions(leaveOpen: true));

        try
        {
            await HandleClientConnectedAsync(_clientSocket, reader, token);
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

    public async ValueTask SendPacket(string packet)
    {
        byte[] packetBytes = Encoding.UTF8.GetBytes(packet);
        byte[] encodedBytes = LoginCryptography.LoginEncrypt(packetBytes);

        var buffer = _writer.GetMemory(encodedBytes.Length);
        encodedBytes.CopyTo(buffer.Span);

        _writer.Advance(encodedBytes.Length);

        var flushResult = await _writer.FlushAsync();

        if (!flushResult.IsCompleted || flushResult.IsCanceled)
        {
            _logger.Error("Failed to send data, connection was closed.");
        }
    }
}
