using System.Net;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.Lambda;
using Benzene.Core.Messages.MessageSender;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Moq;
using Xunit;

namespace Benzene.Aws.Tests;

[Collection("Sequential")]
public class LambdaSenderBuilderTest 
{
    [Fact]
    public async Task Lambda_Send()
    {
        var mockAmazonSns = new Mock<IAmazonLambda>();
        mockAmazonSns.Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse
            {
                Payload = new MemoryStream("{}"u8.ToArray()),
                HttpStatusCode = HttpStatusCode.Accepted
            });

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        var pipelineBuilder = new MiddlewarePipelineBuilder<string>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseAwsLambda(builder2 => builder2
                    .UseAwsLambdaClient(mockAmazonSns.Object))));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload());

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task Lambda_Send2()
    {
        var mockAmazonSns = new Mock<IAmazonLambda>();
        mockAmazonSns.Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse
            {
                Payload = new MemoryStream("{}"u8.ToArray()),
                HttpStatusCode = HttpStatusCode.Accepted
            });

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddScoped(mockAmazonSns.Object);

        var pipelineBuilder = new MiddlewarePipelineBuilder<string>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseAwsLambda(builder2 => builder2.UseAwsLambdaClient())));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload());
        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task Lambda_Send3()
    {
        var mockAmazonSns = new Mock<IAmazonLambda>();
        mockAmazonSns.Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse
            {
                Payload = new MemoryStream("{}"u8.ToArray()),
                HttpStatusCode = HttpStatusCode.Accepted
            });

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddScoped(mockAmazonSns.Object);

        var pipelineBuilder = new MiddlewarePipelineBuilder<string>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseAwsLambda()));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload());

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
}


