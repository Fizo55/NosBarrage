namespace NosBarrage.Shared.Configuration;

public interface IConfiguration
{
    string Address { get; }
    int Port { get; }
    string Database { get; }
}
