using Serilog;
using NosBarrage.Parser.Interface;
using NosBarrage.Database.Services;
using NosBarrage.Shared.Entities;
using NosBarrage.Shared.Enums;

namespace NosBarrage.Parser.Parser;

public class AccountParser : IParser
{
    private readonly ILogger _logger;
    private readonly IDatabaseService<AccountEntity> _accountEntity;

    public AccountParser(ILogger logger, IDatabaseService<AccountEntity> accountEntity)
    {
        _logger = logger;
        _accountEntity = accountEntity;
    }

    public async Task ParseAsync()
    {
        AccountEntity account = new(AccountId: 0, Username: "Fizo", Password: "ee26b0dd4af7e749aa1a8ee3c10ae9923f618980772e473f8819a5d4940e0db27ac185f8a0e1d5f84f88bc887fd67b143732c304cc5fa9ad8e6f57f50028a8ff", Authority: Authority.Administrator);
        await _accountEntity.AddAsync(account);
        AccountEntity account2 = new(AccountId: 0, Username: "Test", Password: "ee26b0dd4af7e749aa1a8ee3c10ae9923f618980772e473f8819a5d4940e0db27ac185f8a0e1d5f84f88bc887fd67b143732c304cc5fa9ad8e6f57f50028a8ff", Authority: Authority.User);
        await _accountEntity.AddAsync(account2);
        _logger.Debug("Accounts parsed");
    }
}
