using Benzene.Abstractions.MessageHandlers;
using Benzene.AspNet.Core;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.Mesh.ShippingService.HealthChecks;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddSingleton<IMessageHandlerDefinition>(_ =>
            MessageHandlerDefinition.CreateInstance("spec", "", typeof(SpecRequest), typeof(RawStringMessage),
                typeof(SpecMessageHandler)));
        services.AddScoped<SpecMessageHandler>();
        services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("get", "/spec", "spec"));
        services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("get", "/healthcheck", "healthcheck"));

        services.UsingBenzene();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // The meshed wire-envelope endpoint (docs/specification/mesh.md): serves this service's
        // topics service-to-service, with the reserved mesh descriptor topic and the trace feed.
        // Branched before UseBenzene so the HTTP pipeline never sees it; StartAnnouncing
        // registers + heartbeats with the collector, log-and-continue.
        app.Map("/benzene/invoke", branch => branch.Run(Benzene.Examples.Mesh.ShippingService.MeshHost.Instance.HandleAsync));
        Benzene.Examples.Mesh.ShippingService.MeshHost.Instance.StartAnnouncing();

        app.UseRouting();

        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseSpec()
                .UseHealthCheck("healthcheck", new ShippingCarrierApiHealthCheck(), new ShippingQueueHealthCheck())
                .UseMessageHandlers()
            )
        );

        app.UseEndpoints(endpoints => { });
    }
}
