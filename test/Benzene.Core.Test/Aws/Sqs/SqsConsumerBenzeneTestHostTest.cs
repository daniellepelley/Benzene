using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Aws.Sqs;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Aws.Sqs.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.SelfHost;
using Benzene.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

// Dogfoods the standalone-SQS-consumer test harness (BuildSqsConsumerHost + HandleAsync) the exact way
// an adopter would: boot the REAL app from its StartUp, swap the outbound client for a fake via
// WithServices, push an SQS Message in the transport's native shape (AsSqsMessage), and assert on both
// the ingress side effect and what the service published (egress). Mirrors KafkaPipelineTest/the Kafka
// worker template test - no running queue, no AWS credentials.
public class SqsConsumerBenzeneTestHostTest
{
    // The egress port the handler publishes through - the thing a test swaps for a fake.
    public interface ISqsOrderPublisher
    {
        Task PublishAsync(string topic, SqsOrderMessage order);
    }

    public interface ISqsOrderStore
    {
        void Save(SqsOrderMessage order);
    }

    public class SqsOrderMessage
    {
        public string Name { get; set; }
    }

    // Deliberately not annotated with [Message]: registered explicitly below (see PubSubPipelineTest's
    // PubSubTestHandler for why - avoids polluting AppDomain-wide reflection scans in unrelated tests).
    public class SqsOrderHandler : IMessageHandler<SqsOrderMessage, Void>
    {
        private readonly ISqsOrderStore _store;
        private readonly ISqsOrderPublisher _publisher;

        public SqsOrderHandler(ISqsOrderStore store, ISqsOrderPublisher publisher)
        {
            _store = store;
            _publisher = publisher;
        }

        public async Task<IBenzeneResult<Void>> HandleAsync(SqsOrderMessage request)
        {
            _store.Save(request);
            await _publisher.PublishAsync(Topic + "d", request);
            return BenzeneResult.Ok(new Void());
        }
    }

    public const string Topic = "sqs-order-create";

    public class SqsConsumerTestStartUp : BenzeneStartUp
    {
        public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
            => services.UsingBenzene(x => x
                .AddBenzene()
                .AddMessageHandlers()
                .AddScoped<SqsOrderHandler>()
                .AddSingleton<IMessageHandlerDefinition>(_ => MessageHandlerDefinition.CreateInstance(
                    Topic, "", typeof(SqsOrderMessage), typeof(Void), typeof(SqsOrderHandler))));

        public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
            => app.UseWorker(worker => worker
                .UseSqs(new SqsConsumerConfig { QueueUrl = "not-polled-in-tests", MaxNumberOfMessages = 10 },
                    new Mock<ISqsClientFactory>().Object,
                    sqs => sqs.UseMessageHandlers()));
    }

    [Fact]
    public async Task HandleAsync_RoutesThroughUseMessageHandlers_AndPublishesEgress()
    {
        var store = new Mock<ISqsOrderStore>();
        var publisher = new Mock<ISqsOrderPublisher>();

        using var host = BenzeneTestHost.Create<SqsConsumerTestStartUp>()
            .WithServices(services => services
                .AddSingleton(store.Object)
                .AddSingleton(publisher.Object))
            .BuildSqsConsumerHost();

        var result = await host.HandleAsync(
            MessageBuilder.Create(Topic, new SqsOrderMessage { Name = "acme" }).AsSqsMessage());

        // Ingress: the routed message reached the real handler.
        store.Verify(x => x.Save(It.Is<SqsOrderMessage>(o => o.Name == "acme")), Times.Once);

        // Egress: the handler published on the derived topic, carrying the payload, through the fake.
        publisher.Verify(x => x.PublishAsync(Topic + "d", It.Is<SqsOrderMessage>(o => o.Name == "acme")), Times.Once);

        // Native response the worker would use to decide deletion: the message succeeded.
        Assert.Single(result.SuccessfulMessages);
        Assert.Empty(result.FailedMessages);
    }

    [Fact]
    public async Task HandleAsync_UnknownTopic_DoesNotReachHandler_AndReportsFailed()
    {
        var store = new Mock<ISqsOrderStore>();
        var publisher = new Mock<ISqsOrderPublisher>();

        using var host = BenzeneTestHost.Create<SqsConsumerTestStartUp>()
            .WithServices(services => services
                .AddSingleton(store.Object)
                .AddSingleton(publisher.Object))
            .BuildSqsConsumerHost();

        var result = await host.HandleAsync(
            MessageBuilder.Create("does-not-exist", new SqsOrderMessage { Name = "acme" }).AsSqsMessage());

        store.Verify(x => x.Save(It.IsAny<SqsOrderMessage>()), Times.Never);
        // An unrouted message is left for redelivery (reported as failed), never silently deleted.
        Assert.Empty(result.SuccessfulMessages);
        Assert.Single(result.FailedMessages);
    }
}
