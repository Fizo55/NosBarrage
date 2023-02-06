using NosBarrage.Shared.Enums;

namespace NosBarrage.Shared.Entities;

public record AccountEntity(int accountId, string username, string password, Authority authority);