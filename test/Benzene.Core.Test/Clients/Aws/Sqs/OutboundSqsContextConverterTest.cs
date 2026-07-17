using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients.Aws.Sqs;

public class OutboundSqsContextConverterTest
{
    [Fact]
    public async Task SendAsync_RoutedThroughUseSqs_SendsViaSqsAndReturnsAcceptedVoidResult()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var services = new ServiceCollection();
        services.AddSingleton(mockAmazonSqs.Object);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseSqs(Defaults.SqsQueueUrl)));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        var result = await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" });

        mockAmazonSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(message =>
                message.QueueUrl == Defaults.SqsQueueUrl &&
                message.MessageAttributes["topic"].StringValue == Defaults.Topic &&
                JsonConvert.DeserializeObject<ExampleRequestPayload>(message.MessageBody).Name == "foo"
            ), It.IsAny<CancellationToken>()));

        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task SendAsync_WithHeaders_ForwardsHeadersAsMessageAttributes()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var services = new ServiceCollection();
        services.AddSingleton(mockAmazonSqs.Object);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddOutboundRouting(routing => routing
            .Route(Defaults.Topic, pipeline => pipeline.UseSqs(Defaults.SqsQueueUrl)));

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var sender = resolver.GetService<IBenzeneMessageSender>();

        await sender.SendAsync<ExampleRequestPayload, Void>(
            Defaults.Topic, new ExampleRequestPayload { Id = 42, Name = "foo" },
            new Dictionary<string, string> { { "tenantId", "tenant-1" } });

        mockAmazonSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(message =>
                message.MessageAttributes["tenantId"].StringValue == "tenant-1"
            ), It.IsAny<CancellationToken>()));
    }
}
