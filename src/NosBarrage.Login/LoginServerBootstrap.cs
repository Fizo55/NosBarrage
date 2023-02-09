using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NosBarrage.Core.Logger;
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
        containerBuilder.RegisterInstance(Logger.GetLogger()).As<ILogger>();
        containerBuilder.RegisterGeneric(typeof(DatabaseService<>)).AsImplementedInterfaces().InstancePerLifetimeScope();
    }

    public static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
    {
        services.AddOptions();

        var builder = new ConfigurationBuilder()
            .AddYamlFile("appsettings.yml", optional: false, reloadOnChange: true);

        services.Configure<LoginConfiguration>(builder.Build());
        var loginConfig = services.BuildServiceProvider().GetRequiredService<IOptions<LoginConfiguration>>().Value;
        services.AddDbContext<NosBarrageContext>(options => options.UseNpgsql(loginConfig.Database));
        services.AddHostedService<LoginServer>();

        var containerBuilder = new ContainerBuilder();
        InitializeContainer(containerBuilder);
        containerBuilder.Populate(services);
        var container = containerBuilder.Build();
        var serviceProvider = new AutofacServiceProvider(container);
        var pipelineService = ActivatorUtilities.CreateInstance<PipelineService>(serviceProvider,
            Assembly.GetAssembly(typeof(NoS0575PacketHandler))!,
            serviceProvider,
            serviceProvider.GetService<ILogger>());
        services.AddSingleton<IPipelineService>(pipelineService);
    }

    static async Task Main(string[] _)
    {
        await CreateWebHostBuilder().RunAsync().ConfigureAwait(false);
    }

    static IHost CreateWebHostBuilder() =>
        new HostBuilder()
            .UseConsoleLifetime()
            .ConfigureServices(ConfigureServices)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .Build();
}
