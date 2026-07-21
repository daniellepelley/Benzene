using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Kafka.Core.Kafka;
using Benzene.Kafka.Core.KafkaMessage;
using Benzene.Results;
using Confluent.Kafka;
using Moq;
using Xunit;

namespace Benzene.Test.Kafka;

// Unlike BenzeneKafkaWorker (needs a live IConsumer<TKey,TValue>/broker), the small mapper/getter
// types below operate on already-constructed Confluent.Kafka objects (ConsumeResult, Message,
// Headers) with no broker involved, so they're straightforward to unit test directly - a gap that
// previously existed alongside the (unavoidable) lack of coverage for the worker itself.
public class KafkaCoreMappersTest
{
    private static ConsumeResult<string, string> CreateConsumeResult(string topic, string value, Headers headers = null)
    {
        return new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = value, Headers = headers ?? new Headers() },
            TopicPartitionOffset = new TopicPartitionOffset(topic, new Partition(0), new Offset(1))
        };
    }

    [Fact]
    public void KafkaMessageBodyGetter_ReturnsTheMessageValue()
    {
        var context = new KafkaRecordContext<string, string>(CreateConsumeResult("my-topic", "hello"));

        Assert.Equal("hello", new KafkaMessageBodyGetter<string, string>().GetBody(context));
    }

    [Fact]
    public void KafkaMessageBodyGetter_ReturnsNull_WhenValueIsNull()
    {
        var context = new KafkaRecordContext<string, string>(CreateConsumeResult("my-topic", null));

        Assert.Null(new KafkaMessageBodyGetter<string, string>().GetBody(context));
    }

    [Fact]
    public void KafkaMessageTopicGetter_ReturnsTheConsumeResultTopic()
    {
        var context = new KafkaRecordContext<string, string>(CreateConsumeResult("my-topic", "hello"));

        Assert.Equal("my-topic", new KafkaMessageTopicGetter<string, string>().GetTopic(context).Id);
    }

    [Fact]
    public void KafkaMessageHeadersGetter_DecodesUtf8HeaderValues()
    {
        var headers = new Headers { { "traceparent", Encoding.UTF8.GetBytes("abc-123") } };
        var context = new KafkaRecordContext<string, string>(CreateConsumeResult("my-topic", "hello", headers));

        var result = new KafkaMessageHeadersGetter<string, string>().GetHeaders(context);

        Assert.Equal("abc-123", result["traceparent"]);
    }

    [Fact]
    public void KafkaMessageHeadersGetter_DuplicateHeaderKeys_TakesLastValue_DoesNotThrow()
    {
        // Kafka headers are an ordered list that legitimately permits repeated keys; ToDictionary threw
        // ArgumentException on the second occurrence, making a valid record unprocessable. Last-wins,
        // matching the RabbitMq/gRPC header getters.
        var headers = new Headers();
        headers.Add("trace", Encoding.UTF8.GetBytes("a"));
        headers.Add("trace", Encoding.UTF8.GetBytes("b"));
        var context = new KafkaRecordContext<string, string>(CreateConsumeResult("my-topic", "hello", headers));

        var result = new KafkaMessageHeadersGetter<string, string>().GetHeaders(context);

        Assert.Equal("b", result["trace"]);
    }

    [Fact]
    public async Task KafkaMessageHandlerResultSetter_ReflectsSuccessOnTheMessageResult()
    {
        var context = new KafkaRecordContext<string, string>(CreateConsumeResult("my-topic", "hello"));
        var handlerResult = new MessageHandlerResult(null, MessageHandlerDefinition.Empty(), BenzeneResult.Ok());

        await new KafkaMessageHandlerResultSetter<string, string>().SetResultAsync(context, handlerResult);

        Assert.True(context.MessageResult.IsSuccessful);
    }

    [Fact]
    public async Task KafkaMessageHandlerResultSetter_ReflectsFailureOnTheMessageResult()
    {
        var context = new KafkaRecordContext<string, string>(CreateConsumeResult("my-topic", "hello"));
        var handlerResult = new MessageHandlerResult(null, MessageHandlerDefinition.Empty(), BenzeneResult.ServiceUnavailable());

        await new KafkaMessageHandlerResultSetter<string, string>().SetResultAsync(context, handlerResult);

        Assert.False(context.MessageResult.IsSuccessful);
    }

    [Fact]
    public void KafkaSendMessageBodyGetter_ReturnsTheMessageValue()
    {
        var context = new KafkaSendMessageContext("my-topic", new Message<string, string> { Value = "hello" });

        Assert.Equal("hello", new KafkaSendMessageBodyGetter().GetBody(context));
    }

    [Fact]
    public void KafkaSendMessageTopicGetter_ReturnsTheContextTopic()
    {
        var context = new KafkaSendMessageContext("my-topic", new Message<string, string> { Value = "hello" });

        Assert.Equal("my-topic", new KafkaSendMessageTopicGetter().GetTopic(context).Id);
    }

    [Fact]
    public void KafkaSendMessageHeadersGetter_DecodesUtf8HeaderValues()
    {
        var headers = new Headers { { "traceparent", Encoding.UTF8.GetBytes("abc-123") } };
        var context = new KafkaSendMessageContext("my-topic", new Message<string, string> { Value = "hello", Headers = headers });

        var result = new KafkaSendMessageHeadersGetter().GetHeaders(context);

        Assert.Equal("abc-123", result["traceparent"]);
    }

    [Fact]
    public void KafkaSendMessageHeadersGetter_DuplicateHeaderKeys_TakesLastValue_DoesNotThrow()
    {
        var headers = new Headers();
        headers.Add("trace", Encoding.UTF8.GetBytes("a"));
        headers.Add("trace", Encoding.UTF8.GetBytes("b"));
        var context = new KafkaSendMessageContext("my-topic", new Message<string, string> { Value = "hello", Headers = headers });

        var result = new KafkaSendMessageHeadersGetter().GetHeaders(context);

        Assert.Equal("b", result["trace"]);
    }

    [Fact]
    public async Task KafkaClientMiddleware_SetsTheContextResponse_FromTheProducer()
    {
        var context = new KafkaSendMessageContext("my-topic", new Message<string, string> { Value = "hello" });
        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = "my-topic",
            Status = PersistenceStatus.Persisted
        };

        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync("my-topic", context.Message, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(deliveryResult);

        var nextCalled = false;
        await new KafkaClientMiddleware(producer.Object).HandleAsync(context, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.Same(deliveryResult, context.Response);
        // KafkaClientMiddleware is the terminal middleware for this pipeline - it should not call `next`.
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task KafkaMessageContextConverter_MapsTopicAndBody_FromTheRegisteredGetters()
    {
        var topicGetter = new Mock<IMessageTopicGetter<string>>();
        topicGetter.Setup(x => x.GetTopic("context")).Returns(new Topic("my-topic"));
        var bodyGetter = new Mock<IMessageBodyGetter<string>>();
        bodyGetter.Setup(x => x.GetBody("context")).Returns("hello");
        var headersGetter = new Mock<IMessageHeadersGetter<string>>();
        var resultSetter = new Mock<IMessageHandlerResultSetter<string>>();
        var responseAdapter = new Mock<IBenzeneResponseAdapter<string>>();

        var converter = new KafkaMessageContextConverter<string>(topicGetter.Object, bodyGetter.Object, headersGetter.Object, resultSetter.Object, responseAdapter.Object);

        var sendContext = await converter.CreateRequestAsync("context");

        Assert.Equal("my-topic", sendContext.Topic);
        Assert.Equal("hello", sendContext.Message.Value);
    }

    [Fact]
    public async Task KafkaMessageContextConverter_Throws_WhenTopicGetterReturnsNull()
    {
        var topicGetter = new Mock<IMessageTopicGetter<string>>();
        topicGetter.Setup(x => x.GetTopic("context")).Returns((ITopic)null);
        var bodyGetter = new Mock<IMessageBodyGetter<string>>();
        var headersGetter = new Mock<IMessageHeadersGetter<string>>();
        var resultSetter = new Mock<IMessageHandlerResultSetter<string>>();
        var responseAdapter = new Mock<IBenzeneResponseAdapter<string>>();

        var converter = new KafkaMessageContextConverter<string>(topicGetter.Object, bodyGetter.Object, headersGetter.Object, resultSetter.Object, responseAdapter.Object);

        await Assert.ThrowsAsync<System.InvalidOperationException>(() => converter.CreateRequestAsync("context"));
    }
}
