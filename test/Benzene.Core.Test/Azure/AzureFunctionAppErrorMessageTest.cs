using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.ServiceBus;
using Benzene.Core.Exceptions;
using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Azure;

public class AzureFunctionAppErrorMessageTest
{
    [Fact]
    public async Task Dispatch_ToAnUnregisteredRequestShape_NamesTheShapeAndTheRegisteredEntryPoints()
    {
        // Only Service Bus is wired, so dispatching a Queue Storage message (string) has no match.
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(app => app
                .UseServiceBus(serviceBus => serviceBus.UseMessageHandlers()))
            .Build();

        var exception = await Assert.ThrowsAsync<BenzeneException>(() =>
            app.HandleAsync(new QueueStorageMessageStub()));

        // The requested shape is named...
        Assert.Contains(nameof(QueueStorageMessageStub), exception.Message);
        // ...the registered entry point (Service Bus batch) is named...
        Assert.Contains(nameof(ServiceBusReceivedMessage), exception.Message);
        // ...and the message points at the fix.
        Assert.Contains("Configure", exception.Message);
        Assert.DoesNotContain("Cannot handle this kind of request", exception.Message);
    }

    [Fact]
    public async Task Dispatch_WithNoEntryPointsRegistered_SaysNone()
    {
        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services.ConfigureServiceCollection())
            .Configure(_ => { })
            .Build();

        var exception = await Assert.ThrowsAsync<BenzeneException>(() =>
            app.HandleAsync(new QueueStorageMessageStub()));

        Assert.Contains("none", exception.Message);
    }

    private class QueueStorageMessageStub;
}
