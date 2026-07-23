using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.Hosting;
using Benzene.Azure.ServiceBus;
using Benzene.SelfHost;
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
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // AddServiceBusConsumer() registers the consumer's services; AddDiagnostics() wraps every
        // middleware in an Activity span (a no-op until an OpenTelemetry exporter is attached). The
        // generic host wires console logging. See docs/monitoring.md and docs/diagnosing-failures.md.
        // Register your application services here - a test can override any of them (see
        // BenzeneStarter.Tests). IGreeter is the demo handler's one dependency.
        services.AddSingleton<IGreeter, ConsoleGreeter>();

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddServiceBusConsumer()
            .AddDiagnostics());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        // NOTE: this is the SELF-HOSTED Service Bus consumer (Benzene owns the process). It is NOT the
        // Azure Functions Service Bus trigger - for that, use the `benzene.azure.servicebus` template.
        //
        // Consumes the "hello_world" queue using the SDK's ServiceBusProcessor. Set the connection string
        // via the ServiceBus__ConnectionString environment variable (or point it at the local emulator).
        // Each message routes to a handler by its "topic" application property. AckMode.Explicit settles
        // each message from the handler's outcome (complete on success, abandon on a thrown exception).
        var serviceBusConfig = new BenzeneServiceBusConfig
        {
            QueueName = "hello_world",
            MaxConcurrentCalls = 5,
            AckMode = ServiceBusConsumerAckMode.Explicit,
        };

        var connectionString = configuration["ServiceBus:ConnectionString"];
        var clientFactory = new ServiceBusClientFactory(new ServiceBusClient(connectionString));

        // UseBenzeneEnrichment + UseLogResult give day-one visibility: a structured log line per message
        // (Info on success, Error on a thrown exception) tagged topic/transport/handler. To also settle a
        // thrown exception yourself, add .UseExceptionHandler((ctx, ex) => ...) - see
        // docs/diagnosing-failures.md and docs/cookbooks/global-error-handling.md.
        app.UseWorker(worker => worker
            .UseServiceBus(serviceBusConfig, clientFactory, serviceBus => serviceBus
                .UseBenzeneEnrichment()
                .UseLogResult(_ => { })
                .UseMessageHandlers()));
    }
}
