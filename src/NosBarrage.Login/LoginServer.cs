using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosBarrage.Core.Pipeline;
using NosBarrage.Shared.Configuration;

namespace NosBarrage.Login;

public class LoginServer : BackgroundService
{
    private readonly IPipelineService _pipelineService;
    private readonly LoginConfiguration _loginConfiguration;

    public LoginServer(IPipelineService pipelineService, IOptions<LoginConfiguration> loginConfiguration)
    {
        _pipelineService = pipelineService;
        _loginConfiguration = loginConfiguration.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _pipelineService.StartServer(_loginConfiguration);
    }
}