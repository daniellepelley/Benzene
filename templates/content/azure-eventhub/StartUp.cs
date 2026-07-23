using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.EventHub.Function;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BenzeneStarter;

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
        // Register your application services here - a test can override any of them (see
        // BenzeneStarter.Tests). IGreeter is the demo handler's one dependency.
        services.AddSingleton<IGreeter, ConsoleGreeter>();

        // AddDiagnostics() wraps every middleware in an Activity span and marks failing stages Error - a
        // no-op until an OpenTelemetry exporter is attached. The Functions host wires logging (Application
        // Insights). See docs/monitoring.md and docs/diagnosing-failures.md.
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddDiagnostics());
    }

    // UseEventHub is a no-op on any host other than Azure Functions. UseBenzeneMessage means each event's
    // body is a Benzene message envelope ({ "topic": ..., "headers": ..., "body": ... }), routed to a
    // handler by that envelope's topic. (Alternatively, route by an event "topic" property with
    // eventHub.UseMessageHandlers() directly.) See docs/azure-functions.md.
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseEventHub(eventHub => eventHub
            .UseBenzeneMessage(direct => direct
                .UseBenzeneEnrichment()
                .UseLogResult(_ => { })
                .UseMessageHandlers()));
    }
}
