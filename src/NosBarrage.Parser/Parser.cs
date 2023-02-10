using Microsoft.Extensions.Hosting;
using Serilog;

namespace NosBarrage.Parser;

public class Parser : BackgroundService
{
    private readonly ILogger _logger;
    
    public Parser(ILogger logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Debug("Start parsing");
    }
}
