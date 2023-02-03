namespace NosBarrage.Shared.Configuration;

public record LoginConfiguration
{
    public string Adress { get; init; }

    public int Port { get; init; }
}