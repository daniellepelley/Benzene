using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.ServiceBus;
using Benzene.Core.MessageHandlers;
using Benzene.Integration.Test.Fixtures;
using Benzene.Integration.Test.Helpers;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Integration.Test.ServiceBus;

[Collection(DockerEmulatorCollection.Name)]
public class ServiceBusConsumerPipelineTest
{
    private const string ConnectionString =
        "Endpoint=sb://localhost:5673;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
    private const string QueueName = "benzene-queue";

    [Fact]
    public async Task Send()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseServiceBus(serviceBus => serviceBus
                    .UseMessageHandlers()))
            .Build();

        // The emulator (and its SQL Server backend) takes a few seconds to become ready after the
        // containers start; retry the initial send rather than relying on a fixed sleep.
        await SendAsync();

        var receivedMessage = await ReceiveAsync();

        await app.HandleServiceBusMessages(receivedMessage);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    private static async Task SendAsync()
    {
        // 180s, not 60s: on a cold CI runner this fixture's container may still be settling by
        // the time this test's own retry window starts, since collection-fixture construction for
        // every emulator in DockerEmulatorCollection happens up front, before any test runs.
        var deadline = DateTime.UtcNow.AddSeconds(180);
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            await using var client = new ServiceBusClient(ConnectionString);
            var sender = client.CreateSender(QueueName);
            try
            {
                var message = new ServiceBusMessage(Defaults.Message);
                message.ApplicationProperties["topic"] = Defaults.Topic;

                await sender.SendMessageAsync(message);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new TimeoutException("Timed out waiting for the Service Bus emulator to become ready.", lastException);
    }

    private static async Task<ServiceBusReceivedMessage> ReceiveAsync()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var receiver = client.CreateReceiver(QueueName);

        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(60));
        if (message == null)
        {
            throw new TimeoutException("Timed out waiting to receive the message back from the Service Bus emulator.");
        }

        await receiver.CompleteMessageAsync(message);
        return message;
    }
}
