using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Benzene.Azure.EventHub;
using Benzene.Core.MessageHandlers;
using Benzene.HostedService;
using Benzene.Integration.Test.Fixtures;
using Benzene.Integration.Test.Helpers;
using Benzene.SelfHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Benzene.Integration.Test.EventHub;

// Reuses the same Event Hubs emulator container as EventHubConsumerPipelineTest (see
// DockerEmulatorCollection), but its own entity (eh2, added to eventhub-emulator-config.json) so
// this worker doesn't cross-read that test's events on eh1. Unlike EventHubConsumerPipelineTest
// (which exercises Benzene.Azure.Function.EventHub's trigger-driven application against a
// hand-received event), this test exercises Benzene.Azure.EventHub's BenzeneEventHubWorker end to
// end: a real EventProcessorClient - checkpointing against the same emulator setup's azurite
// container (blob port 10000, exposed in eventhub-docker-compose.yaml for exactly this test) -
// consuming the hub and dispatching through the message-handler pipeline.
[Collection(DockerEmulatorCollection.Name)]
public class BenzeneEventHubWorkerLiveTest
{
    private const string ConnectionString =
        "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
    private const string EventHubName = "eh2";
    private const string AzuriteConnectionString = "UseDevelopmentStorage=true";

    [Fact]
    public async Task StartAsync_ConsumesRealEventHubEvent_DispatchesThroughPipeline()
    {
        var mockExampleService = new Mock<IExampleService>();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        mockExampleService
            .Setup(x => x.Register(It.IsAny<string>()))
            .Callback(() => received.TrySetResult());

        // Sending first (with its own readiness retry) means the emulator is known-reachable
        // before the worker's processor starts. The checkpoint container has to exist up front
        // too - EventProcessorClient doesn't create it.
        await SendAsync();
        var containerClient = await CreateCheckpointContainerAsync();

        var processorClient = new EventProcessorClient(
            containerClient, EventHubConsumerClient.DefaultConsumerGroupName, ConnectionString, EventHubName);

        var inlineSelfHostedStartUp = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object))
            // Earliest: the event is produced before the processor claims its partition, and with
            // no prior checkpoint the EventProcessorClient would otherwise start from the end of
            // the partition and never see it (the same reason KafkaConsumerConfig uses
            // AutoOffsetReset.Earliest and the pipeline test reads startReadingAtEarliestEvent).
            .Configure(app => app.UseEventHub(
                new BenzeneEventHubConfig { DefaultStartingPosition = EventPosition.Earliest },
                new EventProcessorClientFactory(processorClient),
                eventHub => eventHub.UseMessageHandlers()));

        var host = new HostBuilder()
            .ConfigureServices(services =>
                services.AddHostedService(_ => inlineSelfHostedStartUp.BuildHostedService()))
            .Build();

        await host.StartAsync();
        try
        {
            // Longer than the other emulator tests' 60s: EventProcessorClient has to claim
            // partition ownership via the blob store before it delivers anything.
            var completedTask = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(120)));
            Assert.Same(received.Task, completedTask);
        }
        finally
        {
            await host.StopAsync();
        }

        mockExampleService.Verify(x => x.Register(Defaults.Name), Times.AtLeastOnce);
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
            var producerClient = new EventHubProducerClient(ConnectionString, EventHubName);
            try
            {
                var eventData = new EventData(new BinaryData(Defaults.Message));
                eventData.Properties["topic"] = Defaults.Topic;

                await producerClient.SendAsync(new[] { eventData });
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            finally
            {
                await producerClient.DisposeAsync();
            }
        }

        throw new TimeoutException("Timed out waiting for the Event Hubs emulator to become ready.", lastException);
    }

    private static async Task<BlobContainerClient> CreateCheckpointContainerAsync()
    {
        var containerClient = new BlobContainerClient(AzuriteConnectionString, "eventhub-worker-checkpoints");

        var deadline = DateTime.UtcNow.AddSeconds(180);
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await containerClient.CreateIfNotExistsAsync();
                return containerClient;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new TimeoutException("Timed out waiting for azurite's blob endpoint to become ready.", lastException);
    }
}
