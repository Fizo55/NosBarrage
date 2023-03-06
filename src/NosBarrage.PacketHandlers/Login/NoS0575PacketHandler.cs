using NosBarrage.Core.Packets;
using NosBarrage.Database.Services;
using NosBarrage.Shared.Args.Login;
using NosBarrage.Shared.Entities;
using Serilog;
using System.Net.Sockets;

namespace NosBarrage.PacketHandlers.Login;

[PacketHandler("NoS0575")]
public class NoS0575PacketHandler : IPacketHandler<NoS0575PacketArgs>
{
    private readonly ILogger _logger;
    private readonly IDatabaseService<AccountEntity> _accountEntity;

    public NoS0575PacketHandler(ILogger logger, IDatabaseService<AccountEntity> accountEntity)
    {
        _logger = logger;
        _accountEntity = accountEntity;
    }

    public async Task HandleAsync(NoS0575PacketArgs args, Socket socket)
    {
        bool exist = await _accountEntity.AnyAsync(s => s.Password == args.Password!.ToLower() && s.Username == args.Name);
        if (!exist)
        {
            // FIXME: Send message to client
            return;
        }

        // FIXME: Send channel packet to client
        _logger.Debug($"[Player: {args.Name}] Logged in");
    }
}
