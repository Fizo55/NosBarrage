using Microsoft.Extensions.Hosting;
using NosBarrage.Parser.Interface;
using Serilog;

namespace NosBarrage.Parser;

public class ParserServer : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IEnumerable<IParser> _parser;

    public ParserServer(ILogger logger, IEnumerable<IParser> parser)
    {
        _logger = logger;
        _parser = parser;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Debug("Parser started");
        await Task.WhenAll(_parser.Select(parser => parser.ParseAsync()));

    }
}