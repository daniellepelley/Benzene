using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Aws.Client.Sqs;

public class SqsExtensionsTest
{
    [Fact]
    public async Task UseSqsClient_WithClientInstance_SendsMessage()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var pipeline = new MiddlewarePipelineBuilder<SqsSendMessageContext>(new NullBenzeneServiceContainer())
            .UseSqsClient(mockSqsClient.Object)
            .Build();

        var context = new SqsSendMessageContext(new SendMessageRequest { QueueUrl = "some-queue-url" });
        await pipeline.HandleAsync(context, new NullServiceResolver());

        Assert.Equal(HttpStatusCode.OK, context.Response.HttpStatusCode);
    }

    [Fact]
    public void UseSqs_ConvertsClientPipeline()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IAmazonSQS>());

        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<IBenzeneClientContext<string, Void>>(container);

        var result = builder.UseSqs<string>("some-queue-url");

        Assert.Same(builder, result);
    }

    [Fact]
    public void AddSqsMessageClient_RegistersScopedClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IAmazonSQS>());
        services.AddSingleton(Mock.Of<IBenzeneLogger>());
        services.AddSingleton<IServiceResolver>(Mock.Of<IServiceResolver>());

        var container = new MicrosoftBenzeneServiceContainer(services);

        var result = container.AddSqsMessageClient("some-queue-url", pipeline => pipeline.UseSqsClient(Mock.Of<IAmazonSQS>()));

        Assert.Same(container, result);
    }
}
