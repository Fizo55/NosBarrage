using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosBarrage.Core.Logger;
using NosBarrage.Core.Packets;
using NosBarrage.Core.Pipeline;
using NosBarrage.Database;
using NosBarrage.Database.Services;
using NosBarrage.PacketHandlers.Login;
using NosBarrage.Shared.Configuration;
using System.Reflection;
using ILogger = Serilog.ILogger;

namespace NosBarrage.World;

class WorldServerBootstrap
{
    static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
    {
        services.AddOptions();

        var builder = new ConfigurationBuilder()
            .AddYamlFile("appsettings.yml", optional: false, reloadOnChange: true);

        services.Configure<WorldConfiguration>(builder.Build());
        var worldConfig = services.BuildServiceProvider().GetRequiredService<IOptions<WorldConfiguration>>().Value;
        services.AddDbContext<NosBarrageContext>(options => options.UseNpgsql(worldConfig.Database));
        services.AddHostedService<WorldServer>();

        services.AddSingleton(Logger.GetLogger());
        services.AddScoped(typeof(IDatabaseService<>), typeof(DatabaseService<>));

        // we shouldn't be using login packet there but I haven't coded the world now
        var assembly = Assembly.GetAssembly(typeof(NoS0575PacketHandler));
        var packetHandlerType = typeof(IPacketHandler<>);

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == packetHandlerType));

        foreach (var handlerType in handlerTypes)
        {
            foreach (var interfaceType in handlerType.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == packetHandlerType)
                {
                    services.AddSingleton(typeof(IPacketHandler<object>), handlerType);
                }
            }
        }

        services.AddSingleton(provider =>
        {
            var handlers = provider.GetServices<IPacketHandler<object>>().ToList();
            var logger = provider.GetRequiredService<ILogger>();

            return new PacketHandlerResolver(handlers, logger);
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