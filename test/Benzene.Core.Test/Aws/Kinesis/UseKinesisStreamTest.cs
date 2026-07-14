using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Kinesis;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws.Kinesis;

public class UseKinesisStreamTest
{
    private static KinesisEvent CreateKinesisEvent(string eventSource = "aws:kinesis")
    {
        return new KinesisEvent
        {
            Records = new List<KinesisEventRecord>
            {
                NewRecord(eventSource, "pk1", "1", "one"),
                NewRecord(eventSource, "pk1", "2", "two"),
                NewRecord(eventSource, "pk2", "3", "three"),
            }
        };
    }

    private static KinesisEventRecord NewRecord(string eventSource, string partitionKey, string sequenceNumber, string payload)
    {
        return new KinesisEventRecord
        {
            EventSource = eventSource,
            EventId = "shardId-000000000000:" + sequenceNumber,
            Kinesis = new KinesisRecordData
            {
                PartitionKey = partitionKey,
                SequenceNumber = sequenceNumber,
                Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            }
        };
    }

    [Fact]
    public async Task Send_FromStream_RoutesWholeBatchAsOneStream_InOrder()
    {
        var runs = 0;
        var collected = new List<string>();
        var services = ServiceResolverMother.CreateServiceCollection();
        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(services));

        app.UseKinesisStream(kinesis => kinesis
            .UseStream<KinesisEventRecord>(async (records, _) =>
            {
                runs++;
                await foreach (var record in records)
                {
                    collected.Add(record.Kinesis.GetDataAsString());
                }
            })
        );

        await app.Build().HandleAsync(
            AwsEventStreamContextBuilder.Build(CreateKinesisEvent()),
            new MicrosoftServiceResolverAdapter(services.BuildServiceProvider()));

        // Fan-in: the pipeline runs ONCE for the whole batch, and records arrive in order.
        Assert.Equal(1, runs);
        Assert.Equal(new[] { "one", "two", "three" }, collected);
    }

    [Fact]
    public async Task Send_FromStream_NonKinesisEvent_DoesNotRoute()
    {
        var runs = 0;
        var services = ServiceResolverMother.CreateServiceCollection();
        var app = new MiddlewarePipelineBuilder<AwsEventStreamContext>(new MicrosoftBenzeneServiceContainer(services));

        app.UseKinesisStream(kinesis => kinesis
            .UseStream<KinesisEventRecord>((records, _) =>
            {
                runs++;
                return Task.CompletedTask;
            })
        );

        await app.Build().HandleAsync(
            AwsEventStreamContextBuilder.Build(CreateKinesisEvent(eventSource: "aws:sqs")),
            new MicrosoftServiceResolverAdapter(services.BuildServiceProvider()));

        Assert.Equal(0, runs);
    }
}
