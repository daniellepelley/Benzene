using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Kafka.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Confluent.Kafka;
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
        // AddDiagnostics() wraps every middleware in an Activity span and marks failing stages Error
        // - a no-op until an OpenTelemetry exporter is attached. The generic host (Program.cs) wires
        // console logging for you. See docs/monitoring.md and docs/diagnosing-failures.md.
        // Register your application services here - a test can override any of them (see
        // BenzeneStarter.Tests). IGreeter is the demo handler's one dependency.
        services.AddSingleton<IGreeter, ConsoleGreeter>();

        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddKafka<Ignore, string>()
            .AddDiagnostics());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        // BootstrapServers below matches examples/Kafka/docker-compose.yaml's single-broker
        // Confluent Kafka cluster (`docker compose up -d` from that folder) - point this at your
        // own broker for anything beyond local development.
        var kafkaConfig = new BenzeneKafkaConfig
        {
            ConsumerConfig = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                SecurityProtocol = SecurityProtocol.Plaintext,
                GroupId = "benzene-starter-worker",
                AutoOffsetReset = AutoOffsetReset.Earliest
            },
            Topics = new[] { "hello_world" },
            ConcurrentRequests = 5
        };

        // UseBenzeneEnrichment + UseLogResult give day-one visibility: a structured log line per
        // message (Info on success, Error on a thrown exception) tagged topic/transport/handler. To
        // also settle a thrown exception yourself, add .UseExceptionHandler((ctx, ex) => ...) - see
        // docs/diagnosing-failures.md and docs/cookbooks/global-error-handling.md.
        app.UseWorker(worker =>
            worker.UseKafka<Ignore, string>(kafkaConfig, kafka => kafka
                .UseBenzeneEnrichment()
                .UseLogResult(_ => { })
                .UseMessageHandlers()));
    }
}
