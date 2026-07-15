using System;
using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Kafka.Core.Kafka;
using Benzene.Results;
using Confluent.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Kafka;

public class KafkaBenzeneMessageClientTest
{
    [Fact]
    public async Task SendMessageAsync_PersistedDeliveryResult_ReturnsAccepted()
    {
        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync("some-topic", It.IsAny<Message<string, string>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string> { Topic = "some-topic", Status = PersistenceStatus.Persisted });

        var client = new KafkaBenzeneMessageClient(producer.Object, NullLogger<KafkaBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_NotPersistedDeliveryResult_ReturnsUnexpectedError()
    {
        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync("some-topic", It.IsAny<Message<string, string>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string> { Topic = "some-topic", Status = PersistenceStatus.NotPersisted });

        var client = new KafkaBenzeneMessageClient(producer.Object, NullLogger<KafkaBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.UnexpectedError, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_ThrowingProducer_ReturnsServiceUnavailable()
    {
        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync("some-topic", It.IsAny<Message<string, string>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var client = new KafkaBenzeneMessageClient(producer.Object, NullLogger<KafkaBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_PrebuiltPipeline_PersistedDeliveryResult_ReturnsAccepted()
    {
        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync("some-topic", It.IsAny<Message<string, string>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string> { Topic = "some-topic", Status = PersistenceStatus.Persisted });

        var pipeline = new MiddlewarePipelineBuilder<KafkaSendMessageContext>(new NullBenzeneServiceContainer())
            .UseKafkaClient(producer.Object)
            .Build();

        var client = new KafkaBenzeneMessageClient(pipeline, NullLogger<KafkaBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
}
