using System;
using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Microsoft.Dependencies;
using Benzene.RabbitMq;
using Benzene.SelfHost;
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
        // AddRabbitMq() registers the consumer's services; AddDiagnostics() wraps every middleware in an
        // Activity span (a no-op until an OpenTelemetry exporter is attached). The generic host wires
        // console logging. See docs/monitoring.md and docs/diagnosing-failures.md.
        // Register your application services here - a test can override any of them (see
        // BenzeneStarter.Tests). IGreeter is the demo handler's one dependency.
        services.AddSingleton<IGreeter, ConsoleGreeter>();

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddRabbitMq()
            .AddDiagnostics());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        // Consumes the "hello_world" queue on a local broker (`docker run -p 5672:5672 -p 15672:15672
        // rabbitmq:3-management`, or point the URI at your own). The worker assumes the queue already
        // exists. Each message routes to a handler by its "topic" header (default key "topic"), falling
        // back to the AMQP routing key. Publish a test message with header topic=hello:world and body
        // {"name":"world"}.
        var rabbitMqConfig = new RabbitMqConfig
        {
            QueueName = "hello_world",
            ConcurrentRequests = 5,
        };

        var connectionUri = configuration["RabbitMq:Uri"] ?? "amqp://guest:guest@localhost:5672/";

        // UseBenzeneEnrichment + UseLogResult give day-one visibility: a structured log line per message
        // (Info on success, Error on a thrown exception) tagged topic/transport/handler. To also settle a
        // thrown exception yourself, add .UseExceptionHandler((ctx, ex) => ...) - see
        // docs/diagnosing-failures.md and docs/cookbooks/global-error-handling.md.
        app.UseWorker(worker => worker
            .UseRabbitMq(rabbitMqConfig, new RabbitMqConnectionFactory(new Uri(connectionUri)), rabbit => rabbit
                .UseBenzeneEnrichment()
                .UseLogResult(_ => { })
                .UseMessageHandlers()));
    }
}
