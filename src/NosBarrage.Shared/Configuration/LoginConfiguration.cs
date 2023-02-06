namespace NosBarrage.Shared.Configuration;

public record LoginConfiguration
{
    public string Address { get; init; }

    public int Port { get; init; }

    public string Database { get; init; }
}