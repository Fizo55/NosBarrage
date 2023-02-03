using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NosBarrage.Core.Pipeline;
using NosBarrage.PacketHandlers.Login;
using NosBarrage.Shared.Configuration;
using System.Reflection;

namespace NosBarrage.Login;

class LoginServerBootstrap
{

    private static void InitializeContainer(ContainerBuilder containerBuilder)
    {
        Assembly asm = Assembly.GetAssembly(typeof(NoS0575PacketHandler))!;
        containerBuilder.RegisterType<PipelineService>().AsImplementedInterfaces().WithParameter("asm", asm);
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
                services.AddHostedService<LoginServer>();
            })
            .Build();
}
