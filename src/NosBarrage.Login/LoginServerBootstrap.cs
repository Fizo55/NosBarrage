using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosBarrage.Core.Logger;
using NosBarrage.Core.Packets;
using NosBarrage.Core.Pipeline;
using NosBarrage.Database;
using NosBarrage.Database.Services;
using NosBarrage.PacketHandlers.Login;
using NosBarrage.Shared.Configuration;
using Serilog;
using System.Reflection;

namespace NosBarrage.Login;

class LoginServerBootstrap
{

    private static void InitializeContainer(ContainerBuilder containerBuilder)
    {
        Assembly asm = Assembly.GetAssembly(typeof(NoS0575PacketHandler))!;

        var builder = new ConfigurationBuilder()
            .AddYamlFile("appsettings.yml", optional: false, reloadOnChange: true);

        var config = builder.Build();

        containerBuilder.RegisterInstance(config)
            .As<IOptions<LoginConfiguration>>()
            .SingleInstance();

        containerBuilder.Register(c => c.Resolve<IConfiguration>().Get<LoginConfiguration>())
            .As<LoginConfiguration>()
            .SingleInstance();

        containerBuilder.RegisterType<NosBarrageContext>()
            .WithParameter("options", new DbContextOptionsBuilder<NosBarrageContext>()
                .UseNpgsql(config.Get<LoginConfiguration>().Database).Options)
            .InstancePerLifetimeScope();

        containerBuilder.RegisterInstance(Logger.GetLogger()).As<ILogger>();

        containerBuilder.RegisterType<PacketDeserializer>()
            .AsImplementedInterfaces()
            .SingleInstance();

        containerBuilder.RegisterType<PipelineService>()
            .AsImplementedInterfaces();

        containerBuilder.RegisterGeneric(typeof(DatabaseService<>))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }

    static async Task Main(string[] _)
    {
        await CreateWebHostBuilder().RunAsync().ConfigureAwait(false);
    }

    static IHost CreateWebHostBuilder() =>
        new HostBuilder()
            .UseConsoleLifetime()
            .ConfigureContainer<ContainerBuilder>(InitializeContainer)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<LoginServer>();
            })
            .Build();
}
