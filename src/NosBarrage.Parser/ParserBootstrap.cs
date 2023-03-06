using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NosBarrage.Core.Logger;
using NosBarrage.Database;
using NosBarrage.Database.Services;
using NosBarrage.Parser;
using NosBarrage.Parser.Interface;
using NosBarrage.Shared.Configuration;

namespace NosBarrage.Login
{
    class LoginServerBootstrap
    {
        static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            services.AddOptions();

            var builder = new ConfigurationBuilder()
                .AddYamlFile("appsettings.yml", optional: false, reloadOnChange: true);

            services.Configure<ParserConfiguration>(builder.Build());
            var loginConfig = services.BuildServiceProvider().GetRequiredService<IOptions<ParserConfiguration>>().Value;
            services.AddDbContext<NosBarrageContext>(options => options.UseNpgsql(loginConfig.Database));
            services.AddHostedService<ParserServer>();

            services.AddSingleton(Logger.GetLogger());
            services.AddScoped(typeof(IDatabaseService<>), typeof(DatabaseService<>));

            var type = typeof(IParser);
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(type.IsAssignableFrom).Where(s => !s.IsInterface);

            foreach (Type parse in types)
            {
                services.AddSingleton(typeof(IParser), parse);
            }

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
}
