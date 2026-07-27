using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Kafka.Core;
using Benzene.Kafka.Core.TestHelpers;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.SelfHost;
using Benzene.Testing;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Kafka;

// Proves the additive SendKafkaAsync verb-alias on KafkaBenzeneTestHost dispatches through the real
// pipeline exactly like HandleAsync - so a worker test reads with the same Send* verb the Lambda/GCP
// hosts use. No running broker (only the Kafka application is resolved and driven).
public class KafkaBenzeneTestHostSendKafkaTest
{
    public interface IKafkaWorkerStore
    {
        void Save(string name);
    }

    public class KafkaWorkerMessage
    {
        public string Name { get; set; }
    }

    // Registered explicitly below (no [Message]) to avoid AppDomain-wide reflection-scan pollution.
    public class KafkaWorkerHandler : IMessageHandler<KafkaWorkerMessage, Void>
    {
        private readonly IKafkaWorkerStore _store;

        public KafkaWorkerHandler(IKafkaWorkerStore store)
        {
            _store = store;
        }

        public Task<IBenzeneResult<Void>> HandleAsync(KafkaWorkerMessage request)
        {
            _store.Save(request.Name);
            return Task.FromResult(BenzeneResult.Ok(new Void()));
        }
    }

    public const string Topic = "kafka-worker-topic";

    public class KafkaWorkerTestStartUp : BenzeneStartUp
    {
        public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
            => services.UsingBenzene(x => x
                .AddBenzene()
                .AddMessageHandlers()
                .AddScoped<KafkaWorkerHandler>()
                .AddSingleton<IMessageHandlerDefinition>(_ => MessageHandlerDefinition.CreateInstance(
                    Topic, "", typeof(KafkaWorkerMessage), typeof(Void), typeof(KafkaWorkerHandler))));

        public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
            => app.UseWorker(worker => worker
                .UseKafka<Ignore, string>(
                    new BenzeneKafkaConfig
                    {
                        // Never connected to in a test - the host resolves and drives the Kafka
                        // application directly, no consume loop runs.
                        ConsumerConfig = new ConsumerConfig { BootstrapServers = "not-connected-in-tests", GroupId = "test" },
                        Topics = new[] { Topic }
                    },
                    kafka => kafka.UseMessageHandlers(),
                    healthCheck: false));
    }

    [Fact]
    public async Task SendKafkaAsync_RoutesThroughUseMessageHandlers()
    {
        var store = new Mock<IKafkaWorkerStore>();

        using var host = BenzeneTestHost.Create<KafkaWorkerTestStartUp>()
            .WithServices(services => services.AddSingleton(store.Object))
            .BuildKafkaHost<KafkaWorkerTestStartUp, Ignore, string>();

        await host.SendKafkaAsync(
            MessageBuilder.Create(Topic, new KafkaWorkerMessage { Name = "acme" }).AsKafkaBenzeneMessage());

        store.Verify(x => x.Save("acme"), Times.Once);
    }
}
