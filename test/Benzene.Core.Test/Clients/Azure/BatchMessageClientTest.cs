using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Azure.EventGrid;
using Benzene.Clients.Azure.EventHub;
using Benzene.Clients.Azure.ServiceBus;
using Benzene.Test.Examples;
using Moq;
using Xunit;

namespace Benzene.Test.Clients.Azure;

/// <summary>
/// Covers the Azure batch clients (Service Bus <c>ServiceBusMessageBatch</c>, Event Hub
/// <c>EventDataBatch</c>, Event Grid <c>SendEventsAsync</c>): packing into size-bounded batches, and —
/// since an Azure batch send is atomic — reporting a failed batch send against every request index in
/// that batch.
/// </summary>
public class BatchMessageClientTest
{
    private static IReadOnlyCollection<IBenzeneClientRequest<ExampleRequestPayload>> Requests(int count, Func<int, Dictionary<string, string>> headers = null)
    {
        return Enumerable.Range(0, count)
            .Select(i => (IBenzeneClientRequest<ExampleRequestPayload>)new BenzeneClientRequest<ExampleRequestPayload>(
                Defaults.Topic, new ExampleRequestPayload { Id = i, Name = $"item-{i}" },
                headers?.Invoke(i) ?? new Dictionary<string, string>()))
            .ToList();
    }

    // ---- Service Bus ----

    private static ServiceBusMessageBatch CapacityBatch(int capacity)
    {
        var store = new List<ServiceBusMessage>();
        return ServiceBusModelFactory.ServiceBusMessageBatch(256 * 1024, store, new CreateMessageBatchOptions(),
            _ => store.Count < capacity);
    }

    [Fact]
    public async Task ServiceBus_PacksIntoCapacityBatches_SendingEachOnce()
    {
        var sentCounts = new List<int>();
        var mockSender = new Mock<ServiceBusSender>();
        mockSender.Setup(s => s.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CapacityBatch(2));
        mockSender.Setup(s => s.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessageBatch, CancellationToken>((b, _) => sentCounts.Add(b.Count))
            .Returns(Task.CompletedTask);

        var client = new ServiceBusBatchMessageClient(mockSender.Object);

        var result = await client.SendBatchAsync(Requests(5));

        Assert.True(result.AllSucceeded);
        // Capacity 2 over 5 messages: batches of 2, 2, 1.
        Assert.Equal(new[] { 2, 2, 1 }, sentCounts);
    }

    [Fact]
    public async Task ServiceBus_WhenABatchSendThrows_ReportsEveryMessageInThatBatch()
    {
        var mockSender = new Mock<ServiceBusSender>();
        mockSender.Setup(s => s.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CapacityBatch(2));
        mockSender.Setup(s => s.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("boom", ServiceBusFailureReason.ServiceBusy));

        var client = new ServiceBusBatchMessageClient(mockSender.Object);

        var result = await client.SendBatchAsync(Requests(3));

        // Two batches (2 + 1), both throw: all three indices reported failed.
        Assert.Equal(3, result.Failures.Count);
        Assert.Equal(new[] { 0, 1, 2 }, result.Failures.Select(f => f.Index).OrderBy(i => i).ToArray());
        Assert.All(result.Failures, f => Assert.Contains("boom", f.ErrorMessage));
    }

    // ---- Event Hub ----

    private static EventDataBatch CapacityEventBatch(int capacity, CreateBatchOptions options)
    {
        var store = new List<EventData>();
        return EventHubsModelFactory.EventDataBatch(256 * 1024, store, options, _ => store.Count < capacity);
    }

    [Fact]
    public async Task EventHub_GroupsByPartitionKey_OneBatchPerKey()
    {
        var partitionKeys = new List<string>();
        var mockProducer = new Mock<EventHubProducerClient>();
        mockProducer.Setup(p => p.CreateBatchAsync(It.IsAny<CreateBatchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBatchOptions o, CancellationToken _) =>
            {
                partitionKeys.Add(o.PartitionKey);
                return CapacityEventBatch(100, o);
            });
        mockProducer.Setup(p => p.SendAsync(It.IsAny<EventDataBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = new EventHubBatchMessageClient(mockProducer.Object, partitionKeyHeader: "pk");

        // Two distinct partition keys across four requests.
        var result = await client.SendBatchAsync(Requests(4, i => new Dictionary<string, string>
        {
            { "pk", i % 2 == 0 ? "even" : "odd" }
        }));

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, partitionKeys.Count);
        Assert.Contains("even", partitionKeys);
        Assert.Contains("odd", partitionKeys);
    }

    [Fact]
    public async Task EventHub_WhenSendThrows_ReportsEveryEventInThatBatch()
    {
        var mockProducer = new Mock<EventHubProducerClient>();
        mockProducer.Setup(p => p.CreateBatchAsync(It.IsAny<CreateBatchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBatchOptions o, CancellationToken _) => CapacityEventBatch(100, o));
        mockProducer.Setup(p => p.SendAsync(It.IsAny<EventDataBatch>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EventHubsException(false, "hub", "boom"));

        var client = new EventHubBatchMessageClient(mockProducer.Object);

        var result = await client.SendBatchAsync(Requests(3));

        Assert.Equal(3, result.Failures.Count);
        Assert.All(result.Failures, f => Assert.Contains("boom", f.ErrorMessage));
    }

    // ---- Event Grid ----

    [Fact]
    public async Task EventGrid_ChunksToBatchSize_SendingEachOnce()
    {
        var chunkSizes = new List<int>();
        var mockPublisher = new Mock<EventGridPublisherClient>();
        mockPublisher.Setup(p => p.SendEventsAsync(It.IsAny<IEnumerable<CloudEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CloudEvent>, CancellationToken>((events, _) => chunkSizes.Add(events.Count()))
            .ReturnsAsync((global::Azure.Response)null);

        var client = new EventGridBatchMessageClient("com.example.orders", mockPublisher.Object, batchSize: 2);

        var result = await client.SendBatchAsync(Requests(5));

        Assert.True(result.AllSucceeded);
        Assert.Equal(new[] { 2, 2, 1 }, chunkSizes);
    }

    [Fact]
    public async Task EventGrid_WhenAChunkSendThrows_ReportsEveryEventInThatChunk()
    {
        var mockPublisher = new Mock<EventGridPublisherClient>();
        mockPublisher.Setup(p => p.SendEventsAsync(It.IsAny<IEnumerable<CloudEvent>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var client = new EventGridBatchMessageClient("com.example.orders", mockPublisher.Object, batchSize: 10);

        var result = await client.SendBatchAsync(Requests(3));

        Assert.Equal(3, result.Failures.Count);
        Assert.Equal(new[] { 0, 1, 2 }, result.Failures.Select(f => f.Index).OrderBy(i => i).ToArray());
    }

    // ---- Per-entry conversion failures (isolated, not whole-batch aborting) ----

    // System.Text.Json serializes public get-only properties, so an instance with Explode set throws
    // when its payload is serialized inside converter.CreateRequestAsync - simulating one entry that
    // can't be built while the rest are fine.
    private class BatchConversionTestPayload
    {
        public int Id { get; set; }
        public bool Explode { get; set; }
        public string Value => Explode ? throw new InvalidOperationException("cannot serialize this entry") : "ok";
    }

    private static IReadOnlyCollection<IBenzeneClientRequest<BatchConversionTestPayload>> MixedRequests(int count, int explodeAt)
    {
        return Enumerable.Range(0, count)
            .Select(i => (IBenzeneClientRequest<BatchConversionTestPayload>)new BenzeneClientRequest<BatchConversionTestPayload>(
                Defaults.Topic, new BatchConversionTestPayload { Id = i, Explode = i == explodeAt },
                new Dictionary<string, string>()))
            .ToList();
    }

    [Fact]
    public async Task ServiceBus_WhenOneEntryFailsToSerialize_ReportsOnlyThatEntry()
    {
        var mockSender = new Mock<ServiceBusSender>();
        mockSender.Setup(s => s.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CapacityBatch(100));
        mockSender.Setup(s => s.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = new ServiceBusBatchMessageClient(mockSender.Object);

        var result = await client.SendBatchAsync(MixedRequests(3, explodeAt: 1));

        // The one un-serializable entry is its own failure; the other two still send successfully.
        Assert.Equal(1, result.Failures.Single().Index);
    }

    [Fact]
    public async Task EventHub_WhenOneEntryFailsToSerialize_ReportsOnlyThatEntry()
    {
        var mockProducer = new Mock<EventHubProducerClient>();
        mockProducer.Setup(p => p.CreateBatchAsync(It.IsAny<CreateBatchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBatchOptions o, CancellationToken _) => CapacityEventBatch(100, o));
        mockProducer.Setup(p => p.SendAsync(It.IsAny<EventDataBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = new EventHubBatchMessageClient(mockProducer.Object);

        var result = await client.SendBatchAsync(MixedRequests(3, explodeAt: 1));

        Assert.Equal(1, result.Failures.Single().Index);
    }

    [Fact]
    public async Task EventGrid_WhenOneEntryFailsToSerialize_ReportsOnlyThatEntry()
    {
        var mockPublisher = new Mock<EventGridPublisherClient>();
        mockPublisher.Setup(p => p.SendEventsAsync(It.IsAny<IEnumerable<CloudEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((global::Azure.Response)null);

        var client = new EventGridBatchMessageClient("com.example.orders", mockPublisher.Object, batchSize: 10);

        var result = await client.SendBatchAsync(MixedRequests(3, explodeAt: 1));

        Assert.Equal(1, result.Failures.Single().Index);
    }
}
