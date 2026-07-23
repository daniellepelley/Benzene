using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.QueueStorage;
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

    // UseQueueStorage is a no-op on any host other than Azure Functions. A Queue Storage message carries
    // no properties, so the topic comes from a Benzene message envelope in the body (UseBenzeneMessage:
    // { "topic": ..., "headers": ..., "body": ... }). For a queue that always maps to one topic, use
    // .UsePresetTopic("hello:world").UseMessageHandlers() with a raw body instead. See docs/azure-functions.md.
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseQueueStorage(queue => queue
            .UseBenzeneMessage(direct => direct
                .UseBenzeneEnrichment()
                .UseLogResult(_ => { })
                .UseMessageHandlers()));
    }
}
