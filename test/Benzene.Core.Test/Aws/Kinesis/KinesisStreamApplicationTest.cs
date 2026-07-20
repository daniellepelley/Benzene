using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Aws.Lambda.Kinesis;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Aws.Kinesis;

public class KinesisStreamApplicationTest
{
    private static KinesisEventRecord NewRecord(string sequenceNumber)
    {
        return new KinesisEventRecord
        {
            EventSource = "aws:kinesis",
            EventId = "shardId-000000000000:" + sequenceNumber,
            Kinesis = new KinesisRecordData { SequenceNumber = sequenceNumber },
        };
    }

    private static KinesisEvent CreateKinesisEvent(params string[] sequenceNumbers)
    {
        var records = new List<KinesisEventRecord>();
        foreach (var sequenceNumber in sequenceNumbers)
        {
            records.Add(NewRecord(sequenceNumber));
        }
        return new KinesisEvent { Records = records };
    }

    private static KinesisStreamApplication BuildApplication(
        Func<StreamContext<KinesisEventRecord>, Task> process)
    {
        var services = ServiceResolverMother.CreateServiceCollection();
        var pipeline = new MiddlewarePipelineBuilder<StreamContext<KinesisEventRecord>>(
                new MicrosoftBenzeneServiceContainer(services))
            .UseStream(process)
            .Build();

        return new KinesisStreamApplication(pipeline);
    }

    private static IServiceResolverFactory ServiceResolverFactory()
    {
        return ServiceResolverMother.CreateServiceResolverFactory();
    }

    [Fact]
    public async Task HandleAsync_AllRecordsCheckpointed_ReturnsEmptyBatchItemFailures()
    {
        var application = BuildApplication(async context =>
        {
            await foreach (var record in context.Items)
            {
                await context.Checkpointer.CheckpointAsync(record);
            }
        });

        var response = await application.HandleAsync(CreateKinesisEvent("1", "2", "3"), ServiceResolverFactory());

        Assert.Empty(response.BatchItemFailures);
    }

    [Fact]
    public async Task HandleAsync_CheckpointingAForeignRecord_DoesNotRewindTheResumePoint()
    {
        // A handler that checkpoints record 2 (real), then a record that isn't in the batch by
        // reference (e.g. a projected/transformed copy), must NOT have its resume point rewound to the
        // start of the batch - IndexOf returns -1 for the foreign record, and the old code set the
        // watermark to -1 (reprocess everything). The watermark only advances now.
        var application = BuildApplication(async context =>
        {
            var index = 0;
            await foreach (var record in context.Items)
            {
                index++;
                if (index == 2)
                {
                    await context.Checkpointer.CheckpointAsync(record);                 // real record 2
                    await context.Checkpointer.CheckpointAsync(NewRecord("not-in-batch")); // foreign copy
                    throw new InvalidOperationException("stop after checkpointing");
                }
            }
        });

        var response = await application.HandleAsync(CreateKinesisEvent("1", "2", "3", "4"), ServiceResolverFactory());

        // Resume from record 3 (after the real checkpoint), not record 1 (a rewind to the start).
        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("3", failure.ItemIdentifier);
    }

    [Fact]
    public async Task HandleAsync_ThrowsAfterCheckpointingRecordTwoOfFive_ReturnsRecordThreesSequenceNumber()
    {
        var application = BuildApplication(async context =>
        {
            var processed = 0;
            await foreach (var record in context.Items)
            {
                processed++;
                if (processed == 3)
                {
                    throw new InvalidOperationException("boom");
                }
                await context.Checkpointer.CheckpointAsync(record);
            }
        });

        var response = await application.HandleAsync(CreateKinesisEvent("1", "2", "3", "4", "5"), ServiceResolverFactory());

        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("3", failure.ItemIdentifier);
    }

    [Fact]
    public async Task HandleAsync_ThrowsBeforeCheckpointingAnything_ReturnsFirstRecordsSequenceNumber()
    {
        var application = BuildApplication(_ => throw new InvalidOperationException("boom"));

        var response = await application.HandleAsync(CreateKinesisEvent("1", "2"), ServiceResolverFactory());

        var failure = Assert.Single(response.BatchItemFailures);
        Assert.Equal("1", failure.ItemIdentifier);
    }

    [Fact]
    public async Task HandleAsync_EmptyBatch_ReturnsEmptyBatchItemFailures()
    {
        var application = BuildApplication(_ => Task.CompletedTask);

        var response = await application.HandleAsync(new KinesisEvent { Records = null }, ServiceResolverFactory());

        Assert.Empty(response.BatchItemFailures);
    }
}
