using NosBarrage.Shared.Configuration;

namespace NosBarrage.Core.Pipeline;

public interface IPipelineService
{
    Task StartServer(LoginConfiguration loginConfiguration);
}