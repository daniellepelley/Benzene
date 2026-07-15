using Benzene.Abstractions.MessageHandlers;
using Benzene.AspNet.Core;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Examples.Mesh.OrdersService.HealthChecks;
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
        app.UseRouting();

        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseSpec()
                .UseHealthCheck("healthcheck", new OrdersDatabaseHealthCheck(), new OrdersCacheHealthCheck(), new OrdersQueueHealthCheck())
                .UseMessageHandlers()
            )
        );

        app.UseEndpoints(endpoints => { });
    }
}
