using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Benzene.Abstractions.Hosting;
using Benzene.Azure.EventHub;
using Benzene.Azure.ServiceBus;
using Benzene.Core.MessageHandlers;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;

namespace Benzene.Example.Azure.Worker;

/// <summary>
/// Consumes Azure Service Bus <em>and</em> Azure Event Hubs in one long-running process, dispatching
/// both through the same message-handler pipeline (the shared <c>Benzene.Examples.App</c> handlers,
/// routed by the message's <c>"topic"</c> - e.g. <c>order_create</c>). Neither consumer is an Azure
/// Function; Benzene owns the process. See <c>README.md</c> for how to run it against the local
/// Azure emulators.
/// </summary>
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return DependenciesBuilder.GetConfiguration();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        DependenciesBuilder.Register(services, configuration);
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseWorker(worker => worker
            .UseServiceBus(
                BuildServiceBusConfig(configuration),
                new ServiceBusClientFactory(new ServiceBusClient(configuration["ServiceBus:ConnectionString"])),
                serviceBus => serviceBus.UseMessageHandlers())
            .UseEventHub(
                BuildEventHubConfig(),
                new EventProcessorClientFactory(BuildEventProcessorClient(configuration)),
                eventHub => eventHub.UseMessageHandlers()));
    }

    private static BenzeneServiceBusConfig BuildServiceBusConfig(IConfiguration configuration)
    {
        return new BenzeneServiceBusConfig
        {
            QueueName = configuration["ServiceBus:QueueName"],
            MaxConcurrentCalls = 5,
            // Explicit: settle each message from the handler's outcome (abandon on a thrown
            // exception or an unsuccessful result, complete otherwise) rather than the processor's
            // fire-and-forget auto-complete.
            AckMode = ServiceBusConsumerAckMode.Explicit,
        };
    }

    private static BenzeneEventHubConfig BuildEventHubConfig()
    {
        return new BenzeneEventHubConfig
        {
            CheckpointInterval = 1,
            // Fresh consumer group with no checkpoint: read the retained backlog from the start,
            // so a message sent before the worker started is still seen (the EventProcessorClient
            // default is to read only new events). Kafka analog: AutoOffsetReset.Earliest.
            DefaultStartingPosition = EventPosition.Earliest,
        };
    }

    private static EventProcessorClient BuildEventProcessorClient(IConfiguration configuration)
    {
        // EventProcessorClient checkpoints partition offsets into a blob container that must already
        // exist - it doesn't create one. Create-if-missing here keeps `dotnet run` a single step.
        var checkpointStore = new BlobContainerClient(
            configuration["Storage:ConnectionString"],
            configuration["EventHub:CheckpointContainer"]);
        checkpointStore.CreateIfNotExists();

        var consumerGroup = configuration["EventHub:ConsumerGroup"];
        if (string.IsNullOrEmpty(consumerGroup))
        {
            consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
        }

        return new EventProcessorClient(
            checkpointStore,
            consumerGroup,
            configuration["EventHub:ConnectionString"],
            configuration["EventHub:Name"]);
    }
}
