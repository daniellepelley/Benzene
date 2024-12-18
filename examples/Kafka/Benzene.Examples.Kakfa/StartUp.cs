using Benzene.Core.MessageHandling;
using Benzene.FluentValidation;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.HostedService;
using Benzene.Http.Cors;
using Benzene.Kafka.Core;
using Benzene.Schema.OpenApi;
using Benzene.SelfHost;
using Benzene.SelfHost.Http;
using Benzene.Xml;
using Confluent.Kafka;

namespace Benzene.Examples.Kakfa;

public class StartUp : BenzeneHostedServiceStartup
{
    public override IConfiguration GetConfiguration()
    {
        return DependenciesBuilder.GetConfiguration();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        DependenciesBuilder.Register(services, configuration);
    }

    public override void Configure(IBenzeneWorkerBuilder app, IConfiguration configuration)
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
                x.UseMessageHandlers())
            .UseKafka(benzeneKafkaConfig2, x =>
                x.UseMessageHandlers())
            .UseHttp(new BenzeneHttpConfig
            {
                Url = "http://localhost:5151/",
                ConcurrentRequests = 10
            }, http => http
                .UseProcessResponse()
                .UseXml()
                .UseSpec()
                .UseHealthCheck("get", "healthcheck", x => x.AddHealthCheck("test", x => true))
                .UseCors(new CorsSettings
                {
                    AllowedDomains = new[] { "editor-next.swagger.io" },
                    AllowedHeaders = Array.Empty<string>()
                })
                .UseMessageHandlers(x => x.UseFluentValidation()));
    }
}