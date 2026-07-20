using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Processes a Kafka event, honouring Kafka's per-partition ordering guarantee: records within a
/// single topic-partition run <em>sequentially in offset order and stop at the first failure</em>,
/// while different topic-partitions fan out concurrently. Each partition that fails reports the offset
/// to resume from in a <see cref="KafkaBatchResponse"/>, for triggers with
/// <c>ReportBatchItemFailures</c> configured.
/// </summary>
/// <remarks>
/// This replaces the earlier flatten-and-fan-out-every-record behaviour, which discarded partition
/// grouping (running all records concurrently, losing offset ordering) and never reported failures —
/// a returned failure result was silently dropped. See this package's <c>CLAUDE.md</c>.
/// </remarks>
public class KafkaApplication : IMiddlewareApplication<KafkaEvent, KafkaBatchResponse>
{
    private readonly IMiddlewarePipeline<KafkaContext> _pipeline;
    private readonly KafkaOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Kafka middleware pipeline to run each record through.</param>
    /// <param name="options">
    /// Configures how per-record failures affect the rest of the batch. Defaults to a new
    /// <see cref="KafkaOptions"/> instance (<see cref="KafkaBatchFailureMode.PartialBatchFailure"/>) if
    /// omitted.
    /// </param>
    public KafkaApplication(IMiddlewarePipeline<KafkaContext> pipeline, KafkaOptions options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<KafkaContext>(TransportNames.Kafka, pipeline);
        _options = options ?? new KafkaOptions();
    }

    /// <summary>
    /// Handles a Kafka batch event. Each topic-partition is processed on its own task; within it,
    /// records run one at a time in offset order and processing stops at the first failed record,
    /// preserving Kafka's ordering. Partitions fan out concurrently (bounded by
    /// <see cref="KafkaOptions.MaxDegreeOfParallelism"/>).
    /// </summary>
    /// <param name="event">The Kafka batch event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to create a scope per record.</param>
    /// <returns>
    /// A task that resolves to a <see cref="KafkaBatchResponse"/> naming each failed partition's
    /// resume offset, so the event source mapping redrives only those partitions. A record fails either
    /// if its handler reported an unsuccessful result, or if processing it threw. If
    /// <see cref="KafkaOptions.BatchFailureMode"/> is <see cref="KafkaBatchFailureMode.FailWholeBatch"/>
    /// and at least one partition failed, throws a <see cref="KafkaBatchProcessingException"/> instead,
    /// so the whole invocation — and therefore the whole batch — is retried.
    /// </returns>
    public async Task<KafkaBatchResponse> HandleAsync(KafkaEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var records = @event.Records ?? new Dictionary<string, IList<KafkaEvent.KafkaEventRecord>>();

        // One task per topic-partition. Within a partition we run records sequentially in offset order
        // and stop at the first failure (Kafka's per-partition ordering contract), returning that
        // partition's resume point; across partitions we fan out, bounded by MaxDegreeOfParallelism.
        var perPartition = await BoundedFanOut.WhenAllAsync(records, async partition =>
            await ProcessPartitionAsync(@event, partition.Key, partition.Value, serviceResolverFactory),
            _options.MaxDegreeOfParallelism);

        var failures = perPartition.Where(failure => failure != null).ToList();

        if (failures.Count > 0 && _options.BatchFailureMode == KafkaBatchFailureMode.FailWholeBatch)
        {
            throw new KafkaBatchProcessingException(failures.Select(f => f.ItemIdentifier.Partition).ToArray());
        }

        return new KafkaBatchResponse(failures);
    }

    private async Task<KafkaBatchResponse.BatchItemFailure> ProcessPartitionAsync(
        KafkaEvent @event, string partitionKey, IEnumerable<KafkaEvent.KafkaEventRecord> records,
        IServiceResolverFactory serviceResolverFactory)
    {
        foreach (var record in records.OrderBy(r => r.Offset))
        {
            var context = new KafkaContext(@event, record);

            try
            {
                using var scope = serviceResolverFactory.CreateScope();
                await _pipeline.HandleAsync(context, scope);
            }
            catch (Exception ex)
            {
                using var loggingScope = serviceResolverFactory.CreateScope();
                loggingScope.GetService<ILogger<KafkaApplication>>()
                    .LogError(ex, "Processing Kafka record {partition}@{offset} failed", partitionKey, record.Offset);

                return new KafkaBatchResponse.BatchItemFailure(partitionKey, record.Offset);
            }

            // Only an explicit failure result (IsSuccessful == false) stops the partition. An unset
            // outcome (null - e.g. an unroutable record whose topic matched no handler) is treated as
            // processed and skipped, so a record no handler wants can't wedge the partition into an
            // infinite resume loop - Kafka has no per-record DLQ the way SQS does; a reported failure
            // replays the partition from that offset. This matches the pre-existing fire-and-forget
            // behaviour for unrouted records, adding failure reporting only for genuine failures.
            if (context.MessageResult?.IsSuccessful == false)
            {
                return new KafkaBatchResponse.BatchItemFailure(partitionKey, record.Offset);
            }
        }

        return null;
    }
}
