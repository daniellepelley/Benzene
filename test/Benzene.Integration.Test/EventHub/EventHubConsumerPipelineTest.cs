using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.EventHub.Function;
using Benzene.Azure.Function.EventHub.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Integration.Test.Fixtures;
using Benzene.Integration.Test.Helpers;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Integration.Test.EventHub;

[Collection(EventHubEmulatorCollection.Name)]
public class EventHubConsumerPipelineTest
{
    private const string ConnectionString =
        "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
    private const string EventHubName = "eh1";

    [Fact]
    public async Task Send()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseEventHub(eventHub => eventHub
                    .UseBenzeneMessage(direct => direct
                        .UseMessageHandlers())))
            .Build();

        var sentEvent = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsEventHubBenzeneMessage();

        // The emulator takes a few seconds to become ready after the containers start;
        // retry the initial send rather than relying on a fixed sleep.
        await using var producerClient = await CreateProducerClientAsync();
        await producerClient.SendAsync(new[] { sentEvent });

        var receivedEvent = await ReceiveEventAsync();

        await app.HandleEventHub(receivedEvent);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    private static async Task<EventHubProducerClient> CreateProducerClientAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            var client = new EventHubProducerClient(ConnectionString, EventHubName);
            try
            {
                await client.GetEventHubPropertiesAsync();
                return client;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await client.DisposeAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new TimeoutException("Timed out waiting for the Event Hubs emulator to become ready.", lastException);
    }

    private static async Task<EventData> ReceiveEventAsync()
    {
        await using var consumerClient = new EventHubConsumerClient(
            EventHubConsumerClient.DefaultConsumerGroupName, ConnectionString, EventHubName);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await foreach (var partitionEvent in consumerClient.ReadEventsAsync(startReadingAtEarliestEvent: true, cancellationToken: cts.Token))
        {
            if (partitionEvent.Data != null)
            {
                return partitionEvent.Data;
            }
        }

        throw new TimeoutException("Timed out waiting to receive the event back from the Event Hubs emulator.");
    }
}
