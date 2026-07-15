using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.App.Services;
using Benzene.HealthChecks;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Constants = Benzene.HealthChecks.Constants;

namespace Benzene.Example.Cloudflare;

// Cloudflare Containers proxies HTTP into this process on port 8080 (see ../Dockerfile) and health
// checks it at GET /livez - see docs/getting-started-cloudflare.md and docs/kubernetes-health-checks.md.
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOrderDbClient, InMemoryOrderDbClient>();
        services.AddScoped<IOrderService, OrderService>();

        services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(CreateOrderMessage).Assembly)
            .AddSingleton<IHttpEndpointDefinition>(_ =>
                new HttpEndpointDefinition("GET", "/livez", Constants.DefaultLivenessTopic)));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(http => http
            .UseLivenessCheck(x => x.AddHealthCheck<SimpleHealthCheck>())
            .UseMessageHandlers());
    }
}
