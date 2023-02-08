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
        Assembly asm = Assembly.GetAssembly(typeof(NoS0575PacketHandler))!;
        containerBuilder.RegisterInstance(Logger.GetLogger()).As<ILogger>();
        containerBuilder.RegisterType<PipelineService>().AsImplementedInterfaces().WithParameter("asm", asm);
        containerBuilder.RegisterGeneric(typeof(DatabaseService<>)).AsImplementedInterfaces().InstancePerLifetimeScope();
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
                services.AddOptions();

                var builder = new ConfigurationBuilder()
                    .AddYamlFile("appsettings.yml", optional: false, reloadOnChange: true);

                services.Configure<LoginConfiguration>(builder.Build());
                var loginConfig = services.BuildServiceProvider().GetRequiredService<IOptions<LoginConfiguration>>().Value;
                services.AddDbContext<NosBarrageContext>(options => options.UseNpgsql(loginConfig.Database));
                services.AddHostedService<LoginServer>();
            })
            .Build();
}
