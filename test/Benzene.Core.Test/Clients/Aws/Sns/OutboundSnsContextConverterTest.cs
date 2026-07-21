using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Clients;
using Benzene.Clients.Aws.Sns;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Aws.Sns;

public class OutboundSnsContextConverterTest
{
    private const string TopicArn = "arn:aws:sns:us-east-1:000000000000:example-topic";

    [Fact]
    public async Task SendAsync_RoutedThroughUseSns_PublishesViaSnsAndReturnsAcceptedVoidResult()
    {
        var mockAmazonSns = new Mock<IAmazonSimpleNotificationService>();
        mockAmazonSns.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        var services = new ServiceCollection();
        services.AddSingleton(mockAmazonSns.Object);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseSns(TopicArn)));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        var result = await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" });

        mockAmazonSns.Verify(x => x.PublishAsync(
            It.Is<PublishRequest>(message =>
                message.TopicArn == TopicArn &&
                JsonConvert.DeserializeObject<ExampleRequestPayload>(message.Message).Name == "foo"
            ), It.IsAny<CancellationToken>()));

        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task SendAsync_RoutedThroughUseSns_WritesTheBenzeneTopicAsMessageAttribute()
    {
        // Regression: the SNS publisher used to omit the topic attribute the SNS Lambda consumer
        // routes on, so a Benzene->Benzene SNS round-trip resolved to a null topic. It must now be set.
        var mockAmazonSns = new Mock<IAmazonSimpleNotificationService>();
        mockAmazonSns.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        var services = new ServiceCollection();
        services.AddSingleton(mockAmazonSns.Object);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseSns(TopicArn)));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" });

        mockAmazonSns.Verify(x => x.PublishAsync(
            It.Is<PublishRequest>(message =>
                message.MessageAttributes["topic"].StringValue == Defaults.Topic
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SendAsync_WithCustomTopicAttributeKey_WritesTopicToThatAttribute()
    {
        var mockAmazonSns = new Mock<IAmazonSimpleNotificationService>();
        mockAmazonSns.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        var services = new ServiceCollection();
        services.AddSingleton(mockAmazonSns.Object);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseSns(TopicArn, topicAttributeKey: "x-my-topic")));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" });

        mockAmazonSns.Verify(x => x.PublishAsync(
            It.Is<PublishRequest>(message =>
                message.MessageAttributes.ContainsKey("x-my-topic") &&
                message.MessageAttributes["x-my-topic"].StringValue == Defaults.Topic &&
                !message.MessageAttributes.ContainsKey("topic")
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SendAsync_WithHeaders_ForwardsHeadersAsMessageAttributes()
    {
        var mockAmazonSns = new Mock<IAmazonSimpleNotificationService>();
        mockAmazonSns.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        var services = new ServiceCollection();
        services.AddSingleton(mockAmazonSns.Object);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseSns(TopicArn)));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "tenantId", "tenant-1" } });

        mockAmazonSns.Verify(x => x.PublishAsync(
            It.Is<PublishRequest>(message =>
                message.MessageAttributes["tenantId"].StringValue == "tenant-1"
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SendAsync_WithPublishOptions_AppliesFifoGroupAndDedupFromHeaders()
    {
        // Regression: the OutboundContext SNS path had drifted from SnsContextConverter<T> and could
        // not set MessageGroupId/MessageDeduplicationId, so it silently couldn't publish to a .fifo topic.
        var mockAmazonSns = new Mock<IAmazonSimpleNotificationService>();
        mockAmazonSns.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        var publishOptions = new SnsPublishOptions
        {
            MessageGroupIdHeader = "group",
            MessageDeduplicationIdHeader = "dedup"
        };

        var services = new ServiceCollection();
        services.AddSingleton(mockAmazonSns.Object);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseSns(TopicArn, publishOptions: publishOptions)));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "group", "orders" }, { "dedup", "abc-123" } });

        mockAmazonSns.Verify(x => x.PublishAsync(
            It.Is<PublishRequest>(message =>
                message.MessageGroupId == "orders" &&
                message.MessageDeduplicationId == "abc-123"
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SendAsync_WithInferNumericAttributeTypes_TypesNumericHeaderAsNumber()
    {
        // Regression: the OutboundContext SNS path hardcoded DataType="String" for every attribute, so
        // numeric subscription filter policies could never match on a header it forwarded.
        var mockAmazonSns = new Mock<IAmazonSimpleNotificationService>();
        mockAmazonSns.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        var publishOptions = new SnsPublishOptions { InferNumericAttributeTypes = true };

        var services = new ServiceCollection();
        services.AddSingleton(mockAmazonSns.Object);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseSns(TopicArn, publishOptions: publishOptions)));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "priority", "5" }, { "tenantId", "tenant-1" } });

        mockAmazonSns.Verify(x => x.PublishAsync(
            It.Is<PublishRequest>(message =>
                message.MessageAttributes["priority"].DataType == "Number" &&
                message.MessageAttributes["tenantId"].DataType == "String"
            ), It.IsAny<CancellationToken>()));
    }
}
