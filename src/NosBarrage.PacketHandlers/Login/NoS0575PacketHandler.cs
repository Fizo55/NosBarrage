using NosBarrage.Core;
using NosBarrage.Core.Packets;
using NosBarrage.Database.Services;
using NosBarrage.Shared.Args.Login;
using NosBarrage.Shared.Entities;
using Serilog;

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

    public async Task HandleAsync(NoS0575PacketArgs args, ClientSession session)
    {
        // todo : check if already connected but idc for now
        bool exist = await _accountEntity.AnyAsync(s => s.Password == args.Password!.ToLower() && s.Username == args.Name);
        if (!exist)
        {
            await session.SendPacketAsync("failc 5");
            return;
        }

        await session.SendPacketAsync("NsTeST 0 admin 2 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 0 123 127.0.0.1:1337:0:1.1.NosBarrage -1:-1:-1:10000.10000.1");
        _logger.Debug($"[Player: {args.Name}] Logged in");
    }
}
