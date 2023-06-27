using NosBarrage.Shared.Configuration;

namespace NosBarrage.Core.Pipeline;

public interface IPipelineService
{
    Task StartAsync(IConfiguration configuration, bool isWorld = false, CancellationToken cancellationToken = default);
}