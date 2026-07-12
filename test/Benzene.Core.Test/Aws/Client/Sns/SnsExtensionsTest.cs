using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Aws.Sns;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Aws.Client.Sns;

public class SnsExtensionsTest
{
    [Fact]
    public async Task UseSnsClient_WithClientInstance_PublishesMessage()
    {
        var mockSnsClient = new Mock<IAmazonSimpleNotificationService>();
        mockSnsClient
            .Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { HttpStatusCode = HttpStatusCode.OK });

        var pipeline = new MiddlewarePipelineBuilder<SnsSendMessageContext>(new NullBenzeneServiceContainer())
            .UseSnsClient(mockSnsClient.Object)
            .Build();

        var context = new SnsSendMessageContext(new PublishRequest { TopicArn = "some-topic-arn" });
        await pipeline.HandleAsync(context, new NullServiceResolver());

        Assert.Equal(HttpStatusCode.OK, context.Response.HttpStatusCode);
    }

    [Fact]
    public void UseSnsClient_ResolvedFromContainer_ReturnsSameBuilder()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IAmazonSimpleNotificationService>());

        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<SnsSendMessageContext>(container);

        var result = builder.UseSnsClient();

        Assert.Same(builder, result);
    }

    [Fact]
    public void UseSns_WithAction_ConvertsClientPipeline()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IAmazonSimpleNotificationService>());

        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<IBenzeneClientContext<string, Void>>(container);

        var result = builder.UseSns<string>("some-topic-arn", inner => inner.UseSnsClient(Mock.Of<IAmazonSimpleNotificationService>()));

        Assert.Same(builder, result);
    }

    [Fact]
    public void UseSns_ResolvedFromContainer_ConvertsClientPipeline()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IAmazonSimpleNotificationService>());

        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<IBenzeneClientContext<string, Void>>(container);

        var result = builder.UseSns<string>("some-topic-arn");

        Assert.Same(builder, result);
    }
}
