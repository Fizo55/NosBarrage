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

    public ClientSession(Socket clientSocket, ILogger logger)
    {
        _clientSocket = clientSocket;
        _logger = logger;
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
                //await _packetDeserializer.DeserializeAsync(packet, socket);
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

    public async Task SendPacket(string packet)
    {
        byte[] packetBytes = Encoding.UTF8.GetBytes(packet);
        byte[] encodedBytes = LoginCryptography.LoginEncrypt(packetBytes);

        await _clientSocket.SendAsync(encodedBytes, SocketFlags.None);
    }
}
