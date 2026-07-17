using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Kafka;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class KafkaBatchAndNoOpTest
{
    private static (Mock<IServiceResolver> Resolver, Mock<IServiceResolverFactory> ResolverFactory) CreateResolver()
    {
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<ISetCurrentTransport>()).Returns(Mock.Of<ISetCurrentTransport>());
        mockResolver.Setup(x => x.GetService<ILogger<KafkaApplication>>()).Returns(Mock.Of<ILogger<KafkaApplication>>());
        var mockResolverFactory = new Mock<IServiceResolverFactory>();
        mockResolverFactory.Setup(x => x.CreateScope()).Returns(mockResolver.Object);
        return (mockResolver, mockResolverFactory);
    }

    [Fact]
    public async Task HandleAsync_CatchExceptionsTrue_OneRecordThrows_RestOfBatchStillProcessed()
    {
        var mockPipeline = new Mock<IMiddlewarePipeline<KafkaContext>>();
        mockPipeline
            .Setup(x => x.HandleAsync(It.Is<KafkaContext>(c => c.KafkaEvent.Topic == "topic-fail"), It.IsAny<IServiceResolver>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        mockPipeline
            .Setup(x => x.HandleAsync(It.Is<KafkaContext>(c => c.KafkaEvent.Topic != "topic-fail"), It.IsAny<IServiceResolver>()))
            .Returns(Task.CompletedTask);

        var (_, resolverFactory) = CreateResolver();
        var application = new KafkaBatchApplication(mockPipeline.Object, new KafkaOptions { CatchExceptions = true });

        var batch = new[]
        {
            new KafkaRecord { Topic = "topic-1" },
            new KafkaRecord { Topic = "topic-fail" },
            new KafkaRecord { Topic = "topic-3" }
        };

        // Reaching the end without throwing proves the failing record's exception didn't cascade.
        await application.HandleAsync(batch, resolverFactory.Object);

        mockPipeline.Verify(x => x.HandleAsync(It.Is<KafkaContext>(c => c.KafkaEvent.Topic == "topic-1"), It.IsAny<IServiceResolver>()), Times.Once);
        mockPipeline.Verify(x => x.HandleAsync(It.Is<KafkaContext>(c => c.KafkaEvent.Topic == "topic-3"), It.IsAny<IServiceResolver>()), Times.Once);
    }

    [Fact]
    public void KafkaMessageProcessingException_MessageContainsTheTopic()
    {
        var exception = new KafkaMessageProcessingException("orders-topic");

        Assert.Contains("orders-topic", exception.Message);
    }

    [Fact]
    public void UseKafka_OnNonAzureApplicationBuilder_IsANoOp()
    {
        var mockApp = new Mock<IBenzeneApplicationBuilder>();

        var result = mockApp.Object.UseKafka(_ => { });

        Assert.Same(mockApp.Object, result);
        mockApp.VerifyNoOtherCalls();
    }
}
