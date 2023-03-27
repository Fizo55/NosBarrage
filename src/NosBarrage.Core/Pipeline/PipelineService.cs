using NosBarrage.Core.Cryptography;
using NosBarrage.Core.Packets;
using NosBarrage.Shared.Configuration;
using Serilog;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace NosBarrage.Core.Pipeline;

public class PipelineService : IPipelineService
{
    private PacketDeserializer _deserializer;
    private ILogger _logger;
    private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    public PipelineService(Assembly asm, IServiceProvider serviceProvider, ILogger logger)
    {
        _deserializer = new PacketDeserializer(asm, logger, serviceProvider);
        _logger = logger;
    }

    public async Task StartServer(LoginConfiguration loginConfiguration)
    {
        using var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        IPAddress address = IPAddress.Parse(loginConfiguration.Address);
        listenSocket.Bind(new IPEndPoint(address, loginConfiguration.Port));

        _logger.Debug($"Listening on port {loginConfiguration.Port}");

        listenSocket.Listen(120);

        while (true)
        {
            var acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += async (_, e) => await ProcessLinesAsync(e.AcceptSocket);

            if (!listenSocket.AcceptAsync(acceptEventArgs))
                await ProcessLinesAsync(acceptEventArgs.AcceptSocket);
        }
    }

    private async Task ProcessLinesAsync(Socket socket)
    {
        _logger.Debug($"Client connected");

        var pipe = new Pipe();
        Task writing = FillPipeAsync(socket, pipe.Writer);
        Task reading = ReadPipeAsync(socket, pipe.Reader);

        await Task.WhenAll(reading, writing);

        socket.Shutdown(SocketShutdown.Both);
        socket.Close();

        _logger.Debug($"Client disconnected");
    }

    private async Task FillPipeAsync(Socket socket, PipeWriter writer)
    {
        const int minimumBufferSize = 512;

        var receiveEventArgs = new SocketAsyncEventArgs();
        var buffer = _arrayPool.Rent(minimumBufferSize);
        receiveEventArgs.SetBuffer(buffer, 0, minimumBufferSize);

        try
        {
            while (true)
            {
                TaskCompletionSource<int> tcs = new();
                receiveEventArgs.Completed += OnReceiveCompleted;
                receiveEventArgs.UserToken = tcs;

                if (!socket.ReceiveAsync(receiveEventArgs))
                    OnReceiveCompleted(socket, receiveEventArgs);

                int bytesRead = await tcs.Task;

                if (bytesRead == 0)
                    break;

                writer.Advance(bytesRead);
                var result = await writer.FlushAsync();

                if (result.IsCompleted)
                    break;
            }
        }
        finally
        {
            _arrayPool.Return(buffer);
        }

        writer.Complete();

        void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= OnReceiveCompleted;
            ((TaskCompletionSource<int>)e.UserToken).TrySetResult(e.BytesTransferred);
        }
    }

    private async Task ReadPipeAsync(Socket socket, PipeReader reader)
    {
        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            if (result.IsCompleted && buffer.Length == 0)
                break;

            if (buffer.Length > 0)
            {
                await ProcessLine(socket, buffer);
                reader.AdvanceTo(buffer.End);
            }
        }

        reader.Complete();
    }

    private async Task ProcessLine(Socket socket, ReadOnlySequence<byte> buffer)
    {
        byte[] bArray = _arrayPool.Rent((int)buffer.Length);
        buffer.CopyTo(bArray);

        try
        {
            var loginDecrypt = LoginCryptography.LoginDecrypt(bArray);
            await _deserializer.Deserialize(Encoding.UTF8.GetString(loginDecrypt, 0, (int)buffer.Length), socket);
        }
        finally
        {
            _arrayPool.Return(bArray);
        }
    }
}
