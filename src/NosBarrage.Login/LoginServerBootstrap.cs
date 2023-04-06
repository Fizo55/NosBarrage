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
using NosBarrage.Shared.Configuration;
using System.IO.Pipelines;
using System.Net.Sockets;
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
        services.AddSingleton<ClientHandler>();
        services.AddSingleton<Func<Socket, PipeReader, PipeWriter, CancellationToken, ValueTask>>(sp =>
        {
            var handler = sp.GetRequiredService<ClientHandler>();
            return handler.HandleClientConnectedAsync;
        });
        services.AddSingleton<Func<Socket, ValueTask>>(sp =>
        {
            var handler = sp.GetRequiredService<ClientHandler>();
            return handler.HandleClientDisconnectedAsync;
        });
        services.AddSingleton<IPipelineService, PipelineService>(sp =>
        {
            var clientConnected = sp.GetRequiredService<Func<Socket, PipeReader, PipeWriter, CancellationToken, ValueTask>>();
            var clientDisconnected = sp.GetRequiredService<Func<Socket, ValueTask>>();
            return new PipelineService(services.BuildServiceProvider().GetRequiredService<ILogger>(), clientConnected, clientDisconnected);
        });

        IServiceProvider serviceProvider = services.BuildServiceProvider();
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
