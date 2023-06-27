using NosBarrage.Core.Cryptography;
using NosBarrage.Core.Packets;
using Serilog;
using System.Net.Sockets;
using System.Text;

namespace NosBarrage.Core
{
    public class ClientSession : IDisposable
    {
        private readonly TcpClient _client;
        private readonly ILogger _logger;
        private readonly PacketDeserializer _packetDeserializer;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private bool _isWorld;

        public ClientSession(TcpClient client, ILogger logger, PacketDeserializer packetDeserializer, bool isWorld = false)
        {
            _client = client;
            _logger = logger;
            _packetDeserializer = packetDeserializer;
            _stream = client.GetStream();
            _isWorld = isWorld;
        }

        public async Task StartSessionAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                _logger.Debug("Client connected");
                var receiveTask = ReceivePacketsAsync(_cts.Token);
                await Task.WhenAny(receiveTask);
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occurred: {ex.Message}");
            }
            finally
            {
                _logger.Debug("Client disconnected");
                Dispose();
            }
        }

        private async Task ReceivePacketsAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];

            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (bytesRead == 0)
                    break;
                byte[] decryptedData;
                if (!_isWorld)
                    decryptedData = LoginCryptography.LoginDecrypt(buffer.AsSpan(0, bytesRead).ToArray());
                else
                    decryptedData = WorldCryptography.WorldDecrypt(buffer.AsSpan(0, bytesRead).ToArray());
                var packet = Encoding.UTF8.GetString(decryptedData);
                Console.WriteLine(packet);

                await _packetDeserializer.DeserializeAsync(packet, this);
            }
        }

        public async Task SendPacketAsync(string packet)
        {
            var packetBytes = Encoding.UTF8.GetBytes(packet);
            var encryptedPacketBytes = LoginCryptography.LoginEncrypt(packetBytes);

            await _stream.WriteAsync(encryptedPacketBytes, 0, encryptedPacketBytes.Length);
            await _stream.FlushAsync();
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _client?.Dispose();
            _cts?.Dispose();
        }
    }
}
