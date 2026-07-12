using Benzene.Aws.Sqs;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class UseSqsTest
{
    [Fact]
    public void UseSqs_ReturnsSameStartup_RegistersWorker()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        IBenzeneWorkerStartup app = new BenzeneWorkerStartup2(container);

        var mockSqsClientFactory = new Mock<ISqsClientFactory>();
        var config = new SqsConsumerConfig { QueueUrl = "some-url", MaxNumberOfMessages = 10 };

        var result = app.UseSqs(config, mockSqsClientFactory.Object, pipeline => pipeline.UseMessageHandlers());

        Assert.Same(app, result);

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        var worker = app.Create(serviceResolverFactory);

        Assert.NotNull(worker);
    }
}
