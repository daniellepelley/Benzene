using Benzene.Core.MiddlewareBuilder;
using Benzene.FluentValidation;
using Benzene.HostedService;
using Benzene.Kafka.Core;
using Benzene.SelfHost.Http;
using Benzene.Xml;
using Confluent.Kafka;

namespace Benzene.Examples.Kakfa;

public class Worker : BenzeneHostedServiceStartup
{
    public override IConfiguration GetConfiguration()
    {
        return DependenciesBuilder.GetConfiguration();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        DependenciesBuilder.Register(services, configuration);
    }

    public override void Configure(IHostedServiceAppBuilder app, IConfiguration configuration)
    {
        var benzeneKafkaConfig1 = new BenzeneKafkaConfig
        {
            ConsumerConfig = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.Plaintext,
                GroupId = Guid.NewGuid().ToString(),
                AutoOffsetReset = AutoOffsetReset.Earliest
            },
            Topics = new[] { "order_create", "order_delete" }
        };

        var benzeneKafkaConfig2 = new BenzeneKafkaConfig
        {
            ConsumerConfig = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.Plaintext,
                GroupId = Guid.NewGuid().ToString(),
                AutoOffsetReset = AutoOffsetReset.Earliest
            },
            Topics = new[] { "order_create", "order_delete" }
        };
        app
            .UseKafka(benzeneKafkaConfig1, x =>
                x.UseMessageRouter())
            .UseKafka(benzeneKafkaConfig2, x =>
                x.UseMessageRouter())
            .UseHttp(new BenzeneHttpConfig
            {
                Url = "http://localhost:5151/",
                ConcurrentRequests = 10
            }, http => http
                .UseXml()
                .UseProcessResponse()
                .UseMessageRouter(x => x.UseFluentValidation()));
    }
}