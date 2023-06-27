using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosBarrage.Core.Pipeline;
using NosBarrage.Shared.Configuration;

namespace NosBarrage.World;

public class WorldServer : BackgroundService
{
    private readonly IPipelineService _pipelineService;
    private readonly WorldConfiguration _worldConfiguration;

    public WorldServer(IPipelineService pipelineService, IOptions<WorldConfiguration> worldConfiguration)
    {
        _pipelineService = pipelineService;
        _worldConfiguration = worldConfiguration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _pipelineService.StartAsync(_worldConfiguration, true);
    }
}
