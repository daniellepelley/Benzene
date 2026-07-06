using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Aws.Lambda.Kafka;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.Lambda.Sqs.TestHelpers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Kafka.Core.Kafka;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Sns;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Aws;

public class PipelineTest
{
       
    [Fact]
    public async Task SqsInSnsOut()
    {
        var mockAmazonSimpleNotificationService = new Mock<IAmazonSimpleNotificationService>();
        mockAmazonSimpleNotificationService
            .Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse());
        var services = new MicrosoftBenzeneServiceContainer(new ServiceCollection());
        services.AddBenzeneMiddleware()
            .AddBenzene()
            .AddBenzeneMessage()
            .AddKafka()
            .AddSendKafka()
            .AddSns()
            .AddSqs();
        
        var appBuilder = new MiddlewarePipelineBuilder<SqsMessageContext>(services);
        
        var app = appBuilder
            .SendToSns(mockAmazonSimpleNotificationService.Object)
            .Build();

        var entryPoint =
            new EntryPointMiddlewareApplication<SQSEvent, SQSBatchResponse>(new SqsApplication(app),
                services.CreateServiceResolverFactory());

        var exampleRequestPayload = new ExampleRequestPayload
        {
            Name = "foo"
        };

        var sqsEvent = MessageBuilder.Create(Defaults.Topic, exampleRequestPayload).AsSqs();
        var batchItems = await entryPoint.SendAsync(sqsEvent);
        
        mockAmazonSimpleNotificationService.Verify(x => 
            x.PublishAsync(It.Is<PublishRequest>(x => x
                .Message.Contains("foo") &&
                x.MessageAttributes["topic"].StringValue == Defaults.Topic
            ), It.IsAny<CancellationToken>()));
        Assert.Empty(batchItems.BatchItemFailures);
    }

    // [Fact]
    // public async Task SqsInKafkaOut()
    // {
    //     var producer = new Mock<IProducer<string, string>>();
    //     producer
    //         .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
    //         .ReturnsAsync(new DeliveryReport<string, string>());
    //     var services = new MicrosoftBenzeneServiceContainer(new ServiceCollection());
    //     services.AddBenzeneMiddleware()
    //         .AddBenzene()
    //         .AddBenzeneMessage()
    //         .AddSqs();
    //     
    //     var appBuilder = new MiddlewarePipelineBuilder<SqsMessageContext>(services);
    //     
    //     var app = appBuilder
    //         .WhenIsTopic(Defaults.Topic.ToUpperInvariant(), 
    //             x => x
    //                 .SendToKafka(producer.Object))
    //         .Build();
    //
    //     var entryPoint =
    //         new EntryPointMiddlewareApplication<SQSEvent, SQSBatchResponse>(new SqsApplication(app),
    //             services.CreateServiceResolverFactory());
    //
    //     var exampleRequestPayload = new ExampleRequestPayload
    //     {
    //         Name = "foo"
    //     };
    //
    //     var sqsEvent = MessageBuilder.Create(Defaults.Topic, exampleRequestPayload).AsSqs();
    //     var batchItems = await entryPoint.SendAsync(sqsEvent);
    //     
    //     producer.Verify(x => 
    //         x.ProduceAsync(Defaults.Topic, It.Is<Message<string, string>>(x => x
    //             .Value.Contains("foo") 
    //         ), It.IsAny<CancellationToken>()));
    //     Assert.Empty(batchItems.BatchItemFailures);
    // }
}
