using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.EventGrid;
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
        // AddDiagnostics() wraps every middleware in an Activity span and marks failing stages Error - a
        // no-op until an OpenTelemetry exporter is attached. The Functions host wires logging (Application
        // Insights). See docs/monitoring.md and docs/diagnosing-failures.md.
        // Register your application services here - a test can override any of them (see
        // BenzeneStarter.Tests). IGreeter is the demo handler's one dependency.
        services.AddSingleton<IGreeter, ConsoleGreeter>();

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddDiagnostics());
    }

    // UseEventGrid is a no-op on any host other than Azure Functions. Each event routes to a handler by
    // its event TYPE (matched against [Message(...)]); handles both Event Grid schema and CloudEvents 1.0.
    // See docs/azure-functions.md.
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseEventGrid(eventGrid => eventGrid
            .UseBenzeneEnrichment()
            .UseLogResult(_ => { })
            .UseMessageHandlers());
    }
}
