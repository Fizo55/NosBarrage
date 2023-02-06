using NosBarrage.Shared.Enums;

namespace NosBarrage.Shared.Entities;

public record AccountEntity(int AccountId, string Username, string Password, Authority Authority);