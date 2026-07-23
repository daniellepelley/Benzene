using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers;
using Benzene.FluentValidation;
using Benzene.Kafka.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Confluent.Kafka;

namespace Benzene.Examples.Kafka;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return DependenciesBuilder.GetConfiguration();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        DependenciesBuilder.Register(services, configuration);
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        var benzeneKafkaConfig = new BenzeneKafkaConfig
        {
            ConsumerConfig = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.Plaintext,
                GroupId = Guid.NewGuid().ToString(),
                AutoOffsetReset = AutoOffsetReset.Earliest
            },
            Topics = new[] { "order_create" }
        };

        app.UseWorker(worker => worker
            .UseKafka<Ignore, string>(benzeneKafkaConfig, x =>
                x.UseMessageHandlers(handlers => handlers.UseFluentValidation())));
    }
}
