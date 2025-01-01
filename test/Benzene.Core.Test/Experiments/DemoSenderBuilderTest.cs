using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SQS.Model;
using Amazon.SQS;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Clients.Aws.Lambda;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.MessageSender;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Moq;
using Xunit;

namespace Benzene.Test.Experiments;

public class DemoSenderBuilderTest
{
    [Fact]
    public async Task Sqs_Send()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse
            {
                MD5OfMessageBody = "{}",
                HttpStatusCode = HttpStatusCode.Accepted
            });

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        var pipelineBuilder = new MiddlewarePipelineBuilder<AwsEventStreamContext>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseSqs(builder2 => builder2
                    .UseSqsClient(mockAmazonSqs.Object))));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload());

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
    
    [Fact]
    public async Task Sqs_Send2()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse
            {
                MD5OfMessageBody = "{}",
                HttpStatusCode = HttpStatusCode.Accepted
            });

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddScoped(mockAmazonSqs.Object);
        
        var pipelineBuilder = new MiddlewarePipelineBuilder<AwsEventStreamContext>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseSqs(builder2 => builder2
                    .UseSqsClient())));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload());

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task Sqs_Send3()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse
            {
                MD5OfMessageBody = "{}",
                HttpStatusCode = HttpStatusCode.Accepted
            });

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddScoped(mockAmazonSqs.Object);
        
        var pipelineBuilder = new MiddlewarePipelineBuilder<AwsEventStreamContext>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder.UseSqs()));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload());

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task Lambda_Send()
    {
        var mockAmazonSqs = new Mock<IAmazonLambda>();
        mockAmazonSqs.Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse
            {
                Payload = new MemoryStream("{}"u8.ToArray()),
                HttpStatusCode = HttpStatusCode.Accepted
            });

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        var pipelineBuilder = new MiddlewarePipelineBuilder<AwsEventStreamContext>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseAwsLambda(builder2 => builder2
                    .UseAwsLambdaClient(mockAmazonSqs.Object))));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload());

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
    
    [Fact]
    public async Task Lambda_Send2()
    {
        var mockAmazonSqs = new Mock<IAmazonLambda>();
        mockAmazonSqs.Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse
            {
                Payload = new MemoryStream("{}"u8.ToArray()),
                HttpStatusCode = HttpStatusCode.Accepted
            });

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddScoped(mockAmazonSqs.Object);
        
        var pipelineBuilder = new MiddlewarePipelineBuilder<AwsEventStreamContext>(serviceContainer);

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
        var mockAmazonSqs = new Mock<IAmazonLambda>();
        mockAmazonSqs.Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse
            {
                Payload = new MemoryStream("{}"u8.ToArray()),
                HttpStatusCode = HttpStatusCode.Accepted
            });

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddScoped(mockAmazonSqs.Object);
        
        var pipelineBuilder = new MiddlewarePipelineBuilder<AwsEventStreamContext>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseAwsLambda()));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload());

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
}



