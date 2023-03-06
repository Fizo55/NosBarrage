using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosBarrage.Core.Logger;
using NosBarrage.Core.Pipeline;
using NosBarrage.Database;
using NosBarrage.Database.Services;
using NosBarrage.PacketHandlers.Login;
using NosBarrage.Shared.Configuration;
using System.Reflection;
using ILogger = Serilog.ILogger;

namespace NosBarrage.Login;

class LoginServerBootstrap
{
    static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
    {
        services.AddOptions();

        var builder = new ConfigurationBuilder()
            .AddYamlFile("appsettings.yml", optional: false, reloadOnChange: true);

        services.Configure<LoginConfiguration>(builder.Build());
        var loginConfig = services.BuildServiceProvider().GetRequiredService<IOptions<LoginConfiguration>>().Value;
        services.AddDbContext<NosBarrageContext>(options => options.UseNpgsql(loginConfig.Database));
        services.AddHostedService<LoginServer>();

        services.AddSingleton(Logger.GetLogger());
        services.AddScoped(typeof(IDatabaseService<>), typeof(DatabaseService<>));

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        var pipelineService = new PipelineService(Assembly.GetAssembly(typeof(NoS0575PacketHandler))!, serviceProvider, serviceProvider.GetService<ILogger>()!);
        services.AddSingleton<IPipelineService>(pipelineService);
    }

    static IHostBuilder CreateWebHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices(ConfigureServices);

    static async Task Main(string[] args)
    {
        await CreateWebHostBuilder(args).RunConsoleAsync().ConfigureAwait(false);
    }
}
