using Microsoft.Extensions.Hosting;
using NosBarrage.Core.Pipeline;

namespace NosBarrage.Login;

public class LoginServer : BackgroundService
{
    private readonly IPipelineService _pipelineService;

    public LoginServer(IPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _pipelineService.StartServer();
    }
}