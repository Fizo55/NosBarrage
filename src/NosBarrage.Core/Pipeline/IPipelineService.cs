using NosBarrage.Shared.Configuration;

namespace NosBarrage.Core.Pipeline;

public interface IPipelineService
{
    Task StartAsync(LoginConfiguration configuration, CancellationToken cancellationToken = default);
}