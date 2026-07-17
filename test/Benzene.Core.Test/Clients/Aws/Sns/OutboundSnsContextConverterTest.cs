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
}
