using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Clients;
using Benzene.Clients.Aws.EventBridge;
using Benzene.Clients.Aws.Sns;
using Benzene.Clients.Aws.Sqs;
using Moq;
using Xunit;

namespace Benzene.Test.Clients.Aws;

/// <summary>
/// Covers the AWS batch clients (SQS <c>SendMessageBatch</c>, SNS <c>PublishBatch</c>, EventBridge
/// <c>PutEvents</c>): chunking to the 10-entry provider limit and mapping per-entry failures back to
/// the caller's request indices.
/// </summary>
public class BatchMessageClientTest
{
    private class ExamplePayload
    {
        public string Name { get; set; }
    }

    private static IReadOnlyCollection<IBenzeneClientRequest<ExamplePayload>> Requests(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => (IBenzeneClientRequest<ExamplePayload>)new BenzeneClientRequest<ExamplePayload>(
                "order.created", new ExamplePayload { Name = $"item-{i}" }, new Dictionary<string, string>()))
            .ToList();
    }

    // ---- SQS ----

    [Fact]
    public async Task Sqs_ChunksToTen_IssuingOneBatchCallPerChunk()
    {
        var calls = new List<SendMessageBatchRequest>();
        var mockSqs = new Mock<IAmazonSQS>();
        mockSqs.Setup(x => x.SendMessageBatchAsync(It.IsAny<SendMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendMessageBatchRequest, CancellationToken>((r, _) => calls.Add(r))
            .ReturnsAsync(new SendMessageBatchResponse { HttpStatusCode = HttpStatusCode.OK });

        var client = new SqsBatchMessageClient("https://queue", mockSqs.Object);

        var result = await client.SendBatchAsync(Requests(11));

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, calls.Count);
        Assert.Equal(10, calls[0].Entries.Count);
        Assert.Single(calls[1].Entries);
    }

    [Fact]
    public async Task Sqs_MapsFailuresBackToCallerIndices()
    {
        var mockSqs = new Mock<IAmazonSQS>();
        mockSqs.Setup(x => x.SendMessageBatchAsync(It.IsAny<SendMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Failed = new List<Amazon.SQS.Model.BatchResultErrorEntry>
                {
                    new Amazon.SQS.Model.BatchResultErrorEntry { Id = "2", Code = "InternalError", Message = "boom" }
                }
            });

        var client = new SqsBatchMessageClient("https://queue", mockSqs.Object);

        var result = await client.SendBatchAsync(Requests(3));

        var failure = Assert.Single(result.Failures);
        Assert.Equal(2, failure.Index);
        Assert.Equal("InternalError", failure.ErrorCode);
        Assert.Equal("boom", failure.ErrorMessage);
    }

    // ---- SNS ----

    [Fact]
    public async Task Sns_ChunksToTen_IssuingOneBatchCallPerChunk()
    {
        var calls = new List<PublishBatchRequest>();
        var mockSns = new Mock<IAmazonSimpleNotificationService>();
        mockSns.Setup(x => x.PublishBatchAsync(It.IsAny<PublishBatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PublishBatchRequest, CancellationToken>((r, _) => calls.Add(r))
            .ReturnsAsync(new PublishBatchResponse { HttpStatusCode = HttpStatusCode.OK });

        var client = new SnsBatchMessageClient("arn:aws:sns:::topic", mockSns.Object);

        var result = await client.SendBatchAsync(Requests(23));

        Assert.True(result.AllSucceeded);
        Assert.Equal(3, calls.Count);
        Assert.Equal(10, calls[0].PublishBatchRequestEntries.Count);
        Assert.Equal(10, calls[1].PublishBatchRequestEntries.Count);
        Assert.Equal(3, calls[2].PublishBatchRequestEntries.Count);
    }

    [Fact]
    public async Task Sns_MapsFailuresAcrossChunksToGlobalIndices()
    {
        var mockSns = new Mock<IAmazonSimpleNotificationService>();
        mockSns.Setup(x => x.PublishBatchAsync(It.IsAny<PublishBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PublishBatchRequest req, CancellationToken _) => new PublishBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                // Fail whichever entry in this chunk carries the global index 12.
                Failed = req.PublishBatchRequestEntries
                    .Where(e => e.Id == "12")
                    .Select(e => new Amazon.SimpleNotificationService.Model.BatchResultErrorEntry { Id = e.Id, Code = "Throttled", Message = "slow" })
                    .ToList()
            });

        var client = new SnsBatchMessageClient("arn:aws:sns:::topic", mockSns.Object);

        var result = await client.SendBatchAsync(Requests(15));

        var failure = Assert.Single(result.Failures);
        Assert.Equal(12, failure.Index);
        Assert.Equal("Throttled", failure.ErrorCode);
    }

    // ---- EventBridge ----

    [Fact]
    public async Task EventBridge_ChunksToTen_IssuingOneBatchCallPerChunk()
    {
        var calls = new List<PutEventsRequest>();
        var mockEb = new Mock<IAmazonEventBridge>();
        mockEb.Setup(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutEventsRequest, CancellationToken>((r, _) => calls.Add(r))
            .ReturnsAsync(new PutEventsResponse { HttpStatusCode = HttpStatusCode.OK, FailedEntryCount = 0 });

        var client = new EventBridgeBatchMessageClient("com.example.orders", mockEb.Object);

        var result = await client.SendBatchAsync(Requests(12));

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, calls.Count);
        Assert.Equal(10, calls[0].Entries.Count);
        Assert.Equal(2, calls[1].Entries.Count);
    }

    [Fact]
    public async Task EventBridge_MapsPositionalFailuresToCallerIndices()
    {
        var mockEb = new Mock<IAmazonEventBridge>();
        mockEb.Setup(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PutEventsRequest req, CancellationToken _) =>
            {
                // PutEvents responds positionally: build one result entry per request entry, failing the
                // entry at position 1 within the chunk.
                var entries = req.Entries
                    .Select((_, i) => i == 1
                        ? new PutEventsResultEntry { ErrorCode = "RateExceeded", ErrorMessage = "slow" }
                        : new PutEventsResultEntry { EventId = "ok" })
                    .ToList();
                return new PutEventsResponse
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    FailedEntryCount = 1,
                    Entries = entries
                };
            });

        var client = new EventBridgeBatchMessageClient("com.example.orders", mockEb.Object);

        var result = await client.SendBatchAsync(Requests(3));

        var failure = Assert.Single(result.Failures);
        Assert.Equal(1, failure.Index);
        Assert.Equal("RateExceeded", failure.ErrorCode);
    }
}
