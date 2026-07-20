using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Processes a DynamoDB Streams batch by running each record through the middleware pipeline
/// <b>sequentially, in shard order, stopping at the first failure</b> (plan decision DS5) —
/// deliberately unlike <c>SqsApplication</c>'s concurrent fan-out. Stream records are ordered
/// change-data-capture: processing them concurrently, or continuing past a failure, breaks the
/// per-key ordering the stream guarantees. The first failed record's sequence number is reported
/// as the batch item failure, so Lambda checkpoints there and redelivers from that record.
/// </summary>
public class DynamoDbApplication : IMiddlewareApplication<DynamoDbEvent, DynamoDbBatchResponse>
{
    private readonly IMiddlewarePipeline<DynamoDbRecordContext> _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoDbApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built DynamoDB middleware pipeline to run each record through.</param>
    public DynamoDbApplication(IMiddlewarePipeline<DynamoDbRecordContext> pipeline)
    {
        _pipeline = pipeline;
    }

    /// <summary>
    /// Handles a stream batch, running each record through the pipeline in its own service scope,
    /// in order, until a record fails or the batch completes.
    /// </summary>
    /// <param name="event">The DynamoDB Streams batch event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to create a scope per record.</param>
    /// <returns>
    /// A task that resolves to a batch response. On success the failure list is empty; on the first
    /// failed record (unsuccessful handler result or exception) it contains that record's sequence
    /// number and the remaining records are left unprocessed for redelivery.
    /// </returns>
    public async Task<DynamoDbBatchResponse> HandleAsync(DynamoDbEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        foreach (var record in @event.Records)
        {
            var context = DynamoDbRecordContext.CreateInstance(@event, record);
            try
            {
                using (var scope = serviceResolverFactory.CreateScope())
                {
                    var setCurrentTransport = scope.GetService<ISetCurrentTransport>();
                    setCurrentTransport.SetTransport("dynamodb");
                    await _pipeline.HandleAsync(context, scope);
                }
            }
            catch (Exception ex)
            {
                using (var loggingScope = serviceResolverFactory.CreateScope())
                {
                    loggingScope.GetService<ILogger<DynamoDbApplication>>()
                        .LogError(ex, "Processing DynamoDB stream record {sequenceNumber} failed", record.Dynamodb?.SequenceNumber);
                }

                context.IsSuccessful = false;
            }

            // Treat a null result as failure (not just an explicit false), matching the SQS reference:
            // a record whose pipeline short-circuited without setting a result must NOT be checkpointed
            // past in an ordered CDC stream - that would silently skip it. Stop here for redelivery.
            if (context.IsSuccessful != true)
            {
                return new DynamoDbBatchResponse
                {
                    BatchItemFailures = new List<DynamoDbBatchResponse.BatchItemFailure>
                    {
                        new DynamoDbBatchResponse.BatchItemFailure
                        {
                            ItemIdentifier = record.Dynamodb?.SequenceNumber ?? record.EventId
                        }
                    }
                };
            }
        }

        return new DynamoDbBatchResponse();
    }
}
