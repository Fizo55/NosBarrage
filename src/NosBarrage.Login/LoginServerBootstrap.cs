﻿using Microsoft.EntityFrameworkCore;
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

        var assembly = Assembly.GetAssembly(typeof(NoS0575PacketHandler));
        var packetHandlerType = typeof(IPacketHandler<>);

        var type = typeof(NoS0575PacketHandler);
        var handlerTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(type.IsAssignableFrom).Where(s => !s.IsInterface);
        foreach (var handlerType in handlerTypes)
            services.AddScoped(handlerType);

        services.AddSingleton(provider =>
        {
            var handlers = provider.GetServices<IPacketHandler<object>>().ToList();
            var logger = provider.GetRequiredService<ILogger>();

            return new PacketHandlerResolver(handlers, logger);
        });

        services.AddSingleton<IPipelineService, PipelineService>();

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
