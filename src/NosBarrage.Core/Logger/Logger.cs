using Serilog;

namespace NosBarrage.Core.Logger;

public static class Logger
{
    private static readonly ILogger _logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    public static ILogger GetLogger() => _logger;
}