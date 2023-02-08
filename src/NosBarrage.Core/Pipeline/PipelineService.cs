using NosBarrage.Core.Cryptography;
using NosBarrage.Core.Packets;
using NosBarrage.Shared.Configuration;
using Serilog;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NosBarrage.Core.Pipeline;

public class PipelineService : IPipelineService
{
    private PacketDeserializer _deserializer;
    private readonly LoginConfiguration _loginConfiguration;
    private ILogger _logger;

    public PipelineService(PacketDeserializer deserializer, LoginConfiguration loginConfiguration, ILogger logger)
    {
        _deserializer = deserializer;
        _loginConfiguration = loginConfiguration;
        _logger = logger;
    }

    public async Task StartServer()
    {
        var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        IPAddress address = IPAddress.Parse(_loginConfiguration.Address);
        listenSocket.Bind(new IPEndPoint(address, _loginConfiguration.Port));

        _logger.Debug($"Listening on port {_loginConfiguration.Port}");

        listenSocket.Listen(120);

        while (true)
        {
            var socket = await listenSocket.AcceptAsync();
            _ = ProcessLinesAsync(socket);
        }
    }

    private async Task ProcessLinesAsync(Socket socket)
    {
        _logger.Debug($"Client connected");

        var pipe = new Pipe();
        Task writing = FillPipeAsync(socket, pipe.Writer);
        Task reading = ReadPipeAsync(socket, pipe.Reader);

        await Task.WhenAll(reading, writing);

        _logger.Debug($"Client disconnected");
    }

    private async Task FillPipeAsync(Socket socket, PipeWriter writer)
    {
        const int minimumBufferSize = 512;

        while (true)
        {
            try
            {
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);

                int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
                if (bytesRead == 0)
                {
                    break;
                }

                writer.Advance(bytesRead);
            }
            catch
            {
                break;
            }

            FlushResult result = await writer.FlushAsync();

            if (result.IsCompleted)
            {
                break;
            }
        }

        writer.Complete();
    }

    private async Task ReadPipeAsync(Socket socket, PipeReader reader)
    {
        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            if (buffer.Length == 0)
            {
                if (result.IsCompleted)
                    break;

                continue;
            }

            ProcessLine(socket, buffer);
            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
                break;
        }

        reader.Complete();
    }

    private void ProcessLine(Socket socket, in ReadOnlySequence<byte> buffer)
    {
        byte[] bArray = buffer.ToArray();
        var loginDecrypt = LoginCryptography.LoginDecrypt(bArray);
        _deserializer.Deserialize(Encoding.UTF8.GetString(loginDecrypt), socket);
    }
}
