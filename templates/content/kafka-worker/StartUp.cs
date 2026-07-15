using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
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
        services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddKafka<Ignore, string>());
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

        app.UseWorker(worker =>
            worker.UseKafka<Ignore, string>(kafkaConfig, kafka => kafka.UseMessageHandlers()));
    }
}
