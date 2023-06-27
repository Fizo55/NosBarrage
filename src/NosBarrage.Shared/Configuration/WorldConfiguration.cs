namespace NosBarrage.Shared.Configuration;

public record WorldConfiguration : IConfiguration
{
    public string Address { get; init; }

    public int Port { get; init; }

    public string Database { get; init; }
}