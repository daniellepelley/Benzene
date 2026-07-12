using Amazon.SQS;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client.Sqs;

public class SqsBenzeneMessageClientExtensionsTest
{
    [Fact]
    public void CreateSqsBenzeneMessageClient_Named_RegistersClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IAmazonSQS>());
        services.AddSingleton(Mock.Of<IBenzeneLogger>());

        var container = new MicrosoftBenzeneServiceContainer(services);
        var clientsBuilder = new ClientsBuilder(container);

        var result = clientsBuilder.CreateSqsBenzeneMessageClient("named-client", "some-queue-url", new NullServiceResolver(), _ => { });

        Assert.Same(clientsBuilder, result);
    }

    [Fact]
    public void CreateSqsBenzeneMessageClient_Unnamed_RegistersClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IAmazonSQS>());
        services.AddSingleton(Mock.Of<IBenzeneLogger>());

        var container = new MicrosoftBenzeneServiceContainer(services);
        var clientsBuilder = new ClientsBuilder(container);

        clientsBuilder.CreateSqsBenzeneMessageClient("some-queue-url", new NullServiceResolver(), _ => { });
    }
}
