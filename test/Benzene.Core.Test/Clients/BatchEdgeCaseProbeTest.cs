using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Clients.Azure.EventHub;
using Benzene.Clients.Azure.ServiceBus;
using Moq;
using Xunit;

namespace Benzene.Test.Clients;

/// <summary>
/// Empirical boundary/edge probes for the batch clients — deliberately hammering the corners
/// (empty input, exact chunk multiples, capacity-0 batches, per-group failure isolation) to surface
/// off-by-one / infinite-loop / mis-indexing bugs that static review can miss.
/// </summary>
public class BatchEdgeCaseProbeTest
{
    private class Payload { public int N { get; set; } }

    private static IReadOnlyCollection<IBenzeneClientRequest<Payload>> Reqs(int n) =>
        Enumerable.Range(0, n).Select(i => (IBenzeneClientRequest<Payload>)new BenzeneClientRequest<Payload>(
            "t", new Payload { N = i }, new Dictionary<string, string>())).ToList();

    [Fact]
    public async Task Sqs_EmptyCollection_MakesNoCallsAndSucceeds()
    {
        var mock = new Mock<IAmazonSQS>();
        var client = new SqsBatchMessageClient("https://q", mock.Object);

        var result = await client.SendBatchAsync(Reqs(0));

        Assert.True(result.AllSucceeded);
        mock.Verify(x => x.SendMessageBatchAsync(It.IsAny<SendMessageBatchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(20, 2)]
    [InlineData(21, 3)]
    [InlineData(1, 1)]
    public async Task Sqs_ChunkBoundaries(int count, int expectedCalls)
    {
        var calls = 0;
        var mock = new Mock<IAmazonSQS>();
        mock.Setup(x => x.SendMessageBatchAsync(It.IsAny<SendMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls++)
            .ReturnsAsync(new SendMessageBatchResponse { HttpStatusCode = HttpStatusCode.OK });
        var client = new SqsBatchMessageClient("https://q", mock.Object);

        await client.SendBatchAsync(Reqs(count));

        Assert.Equal(expectedCalls, calls);
    }

    private static ServiceBusMessageBatch CapacityBatch(int capacity)
    {
        var store = new List<ServiceBusMessage>();
        return ServiceBusModelFactory.ServiceBusMessageBatch(256 * 1024, store, new CreateMessageBatchOptions(),
            _ => store.Count < capacity);
    }

    [Fact]
    public async Task ServiceBus_CapacityZeroBatch_EveryMessageReportedTooLarge_NoSend_NoHang()
    {
        var mockSender = new Mock<ServiceBusSender>();
        mockSender.Setup(s => s.CreateMessageBatchAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => CapacityBatch(0));
        var client = new ServiceBusBatchMessageClient(mockSender.Object);

        var task = client.SendBatchAsync(Reqs(3));
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(task, completed); // did not hang

        var result = await task;
        Assert.Equal(3, result.Failures.Count);
        Assert.All(result.Failures, f => Assert.Equal("MessageTooLarge", f.ErrorCode));
        Assert.Equal(new[] { 0, 1, 2 }, result.Failures.Select(f => f.Index).ToArray());
        mockSender.Verify(s => s.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ServiceBus_CapacityOne_SendsEachMessageInItsOwnBatch()
    {
        var sentCounts = new List<int>();
        var mockSender = new Mock<ServiceBusSender>();
        mockSender.Setup(s => s.CreateMessageBatchAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => CapacityBatch(1));
        mockSender.Setup(s => s.SendMessagesAsync(It.IsAny<ServiceBusMessageBatch>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessageBatch, CancellationToken>((b, _) => sentCounts.Add(b.Count))
            .Returns(Task.CompletedTask);
        var client = new ServiceBusBatchMessageClient(mockSender.Object);

        var result = await client.SendBatchAsync(Reqs(3));

        Assert.True(result.AllSucceeded);
        Assert.Equal(new[] { 1, 1, 1 }, sentCounts);
    }

    private static EventDataBatch CapacityEventBatch(int capacity, CreateBatchOptions options)
    {
        var store = new List<EventData>();
        return EventHubsModelFactory.EventDataBatch(256 * 1024, store, options, _ => store.Count < capacity);
    }

    [Fact]
    public async Task EventHub_OnePartitionGroupFails_OnlyThatGroupsIndicesReported()
    {
        // Track which batches were created for the "even" partition-key group, so SendAsync can fail
        // only that group and prove per-group failure isolation.
        var evenBatches = new HashSet<EventDataBatch>();
        var mockProducer = new Mock<EventHubProducerClient>();
        mockProducer.Setup(p => p.CreateBatchAsync(It.IsAny<CreateBatchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateBatchOptions o, CancellationToken _) =>
            {
                var batch = CapacityEventBatch(100, o);
                if (o.PartitionKey == "even") evenBatches.Add(batch);
                return batch;
            });
        mockProducer.Setup(p => p.SendAsync(It.IsAny<EventDataBatch>(), It.IsAny<CancellationToken>()))
            .Returns((EventDataBatch b, CancellationToken _) =>
                evenBatches.Contains(b) ? Task.FromException(new EventHubsException(false, "h", "boom")) : Task.CompletedTask);

        var client = new EventHubBatchMessageClient(mockProducer.Object, partitionKeyHeader: "pk");

        // indices 0,2 -> "even"; 1,3 -> "odd"
        var reqs = Enumerable.Range(0, 4).Select(i => (IBenzeneClientRequest<Payload>)new BenzeneClientRequest<Payload>(
            "t", new Payload { N = i }, new Dictionary<string, string> { { "pk", i % 2 == 0 ? "even" : "odd" } })).ToList();

        var result = await client.SendBatchAsync(reqs);

        var failedIndices = result.Failures.Select(f => f.Index).OrderBy(i => i).ToArray();
        Assert.Equal(new[] { 0, 2 }, failedIndices); // only the even group failed; odd survived
    }
}
