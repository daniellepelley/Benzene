using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Benzene.HostedService;

public static class HostBuilderExtensions
{
    /// <summary>Runs a platform-neutral BenzeneStartUp as a hosted worker on the .NET generic host.</summary>
    public static IHostBuilder UseBenzene<TStartUp>(this IHostBuilder hostBuilder)
        where TStartUp : BenzeneStartUp, new()
    {
        var startUp = new TStartUp();
        var configuration = startUp.GetConfiguration();
        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddLogging();
            var container = new MicrosoftBenzeneServiceContainer(services);
            var builder = new WorkerApplicationBuilder(container);

            startUp.ConfigureServices(services, configuration);
            startUp.Configure(builder, configuration);

            services.AddSingleton<IHostedService>(provider =>
                new BenzeneHostedServiceAdapter(builder.CreateWorker(new MicrosoftServiceResolverFactory(provider))));
        });
    }
}
