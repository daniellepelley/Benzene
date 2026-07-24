using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Benzene.Test.Contract;

// Guards the 1.0 settlement contract (work/settlement-contract-1.0.md) against silent drift between
// the code and docs/capability-matrix.md. The contract is NOT uniform - and the guard encodes that:
//
//  * Queue-shaped transports are safe-by-default: a returned failure result is redelivered
//    (at-least-once), not silently settled. QueueShapedTransports_DefaultToSafe pins each one's *code*
//    default; CapabilityMatrix_MarksQueueTransportSafeByDefault pins its matrix row ("Safe by default").
//  * The two self-hosted *stream* workers (Kafka.Core, Azure.EventHub) default to AT-MOST-ONCE: a
//    stream has no per-message ack, so "don't lose a failed record" would mean halting the whole
//    worker - too drastic to be a default. SelfHostedStreamWorkers_DefaultToAtMostOnce pins their code
//    defaults; CapabilityMatrix_MarksStreamWorkerAtMostOnceByDefault pins their matrix row
//    ("At-most-once by default").
//
// Together they stop the capability matrix from ever again describing a transport's data-safety
// differently from how the code actually behaves - including the awkward stream-worker case where a
// naive reading of RaiseOnFailureStatus=true wrongly suggests safe-by-default.
public class SettlementContractDefaultsTest
{
    [Fact]
    public void QueueShapedTransports_DefaultToSafe()
    {
        // AWS Lambda event sources - a returned failure escalates (RaiseOnFailureStatus) ...
        Assert.True(new Benzene.Aws.Lambda.Sns.SnsOptions().RaiseOnFailureStatus);
        Assert.True(new Benzene.Aws.Lambda.S3.S3Options().RaiseOnFailureStatus);
        Assert.True(new Benzene.Aws.Lambda.EventBridge.EventBridgeOptions().RaiseOnFailureStatus);
        // ... or (Kafka) is reported for redelivery per-partition rather than the whole batch swallowed.
        Assert.Equal(Benzene.Aws.Lambda.Kafka.KafkaBatchFailureMode.PartialBatchFailure,
            new Benzene.Aws.Lambda.Kafka.KafkaOptions().BatchFailureMode);

        // AWS self-hosted SQS consumer - only successfully-handled messages are deleted.
        Assert.Equal(Benzene.Aws.Sqs.Consumer.SqsConsumerAckMode.PerMessage,
            new Benzene.Aws.Sqs.Consumer.SqsConsumerOptions().AckMode);

        // Azure Functions triggers.
        Assert.True(new Benzene.Azure.Function.ServiceBus.ServiceBusOptions().RaiseOnFailureStatus);
        Assert.True(new Benzene.Azure.Function.Kafka.KafkaOptions().RaiseOnFailureStatus);
        Assert.True(new Benzene.Azure.Function.QueueStorage.QueueStorageOptions().RaiseOnFailureStatus);
        Assert.True(new Benzene.Azure.Function.EventGrid.EventGridOptions().RaiseOnFailureStatus);
        Assert.True(new Benzene.Azure.Function.EventHub.Function.EventHubOptions().RaiseOnFailureStatus);

        // Azure self-hosted Service Bus worker (a queue - per-message abandon).
        Assert.Equal(Benzene.Azure.ServiceBus.ServiceBusConsumerAckMode.Explicit,
            new Benzene.Azure.ServiceBus.BenzeneServiceBusConfig().AckMode);

        // RabbitMQ self-hosted worker.
        Assert.Equal(Benzene.RabbitMq.RabbitMqAckMode.Explicit,
            new Benzene.RabbitMq.RabbitMqConfig { QueueName = "guard" }.AckMode);

        // Google Cloud Pub/Sub.
        Assert.True(new Benzene.GoogleCloud.Functions.PubSub.PubSubOptions().RaiseOnFailureStatus);
    }

    [Fact]
    public void SelfHostedStreamWorkers_DefaultToAtMostOnce()
    {
        // A stream has no per-message ack/abandon, so safe-by-default would mean halting the worker
        // (never advancing the offset/checkpoint past a poison record) - too drastic to be a default.
        // Both therefore default to skip-and-continue (at-most-once); at-least-once is opt-in.

        // Event Hub worker: RaiseOnFailureStatus=true escalates a failure result into an exception,
        // but CatchHandlerExceptions=true (this default) then catches it and the partition advances -
        // so the failed event is skipped once a later one checkpoints. At-least-once needs
        // CatchHandlerExceptions=false. Guarding CatchHandlerExceptions here is what pins the
        // *documented* at-most-once default (RaiseOnFailureStatus alone doesn't achieve safety).
        var eventHub = new Benzene.Azure.EventHub.BenzeneEventHubConfig();
        Assert.True(eventHub.CatchHandlerExceptions);
        Assert.True(eventHub.RaiseOnFailureStatus); // true, but defeated by CatchHandlerExceptions above

        // Kafka worker: offsets auto-commit regardless of outcome unless CommitOnlyOnSuccess is set,
        // so a failed record is skipped on restart. At-least-once needs CommitOnlyOnSuccess=true.
        var kafka = new Benzene.Kafka.Core.BenzeneKafkaConfig
        {
            ConsumerConfig = new Confluent.Kafka.ConsumerConfig(),
            Topics = new[] { "guard" }
        };
        Assert.False(kafka.CommitOnlyOnSuccess);
    }

    [Theory]
    [InlineData("Benzene.Aws.Lambda.Sns")]
    [InlineData("Benzene.Aws.Lambda.S3")]
    [InlineData("Benzene.Aws.Lambda.EventBridge")]
    [InlineData("Benzene.Aws.Lambda.Kafka")]
    [InlineData("Benzene.Aws.Sqs")]
    [InlineData("Benzene.Azure.Function.ServiceBus")]
    [InlineData("Benzene.Azure.Function.Kafka")]
    [InlineData("Benzene.Azure.Function.QueueStorage")]
    [InlineData("Benzene.Azure.Function.EventGrid")]
    [InlineData("Benzene.Azure.Function.EventHub")]
    [InlineData("Benzene.Azure.ServiceBus")]
    [InlineData("Benzene.RabbitMq")]
    [InlineData("Benzene.GoogleCloud.Functions.PubSub")]
    public void CapabilityMatrix_MarksQueueTransportSafeByDefault(string packageId) =>
        AssertMatrixRow(packageId, "Safe by default");

    // The two self-hosted stream workers are the deliberate at-most-once exception (see the class
    // comment and the streaming callout in docs/capability-matrix.md).
    [Theory]
    [InlineData("Benzene.Azure.EventHub")]
    [InlineData("Benzene.Kafka.Core")]
    public void CapabilityMatrix_MarksStreamWorkerAtMostOnceByDefault(string packageId) =>
        AssertMatrixRow(packageId, "At-most-once by default");

    private static void AssertMatrixRow(string packageId, string expectedMarker)
    {
        var matrixPath = FindRepoFile(Path.Combine("docs", "capability-matrix.md"));
        var token = $"`{packageId}`";

        // A table row is a line starting with '|'; match on the backtick-wrapped package id so a
        // shorter id can't match a longer one's row (e.g. Benzene.Azure.EventHub vs .Function.EventHub).
        var rows = File.ReadLines(matrixPath)
            .Where(line => line.TrimStart().StartsWith("|") && line.Contains(token))
            .ToList();

        Assert.True(rows.Count == 1,
            $"Expected exactly one capability-matrix.md table row mentioning {token}, found {rows.Count}. " +
            "Add/deduplicate its row (see work/settlement-contract-1.0.md).");
        Assert.True(rows[0].Contains(expectedMarker),
            $"The capability-matrix.md row for {token} must say \"{expectedMarker}\" - it has drifted " +
            $"from the code default the settlement contract guarantees. Row:\n{rows[0]}");
    }

    private static string FindRepoFile(string relativePath)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"Could not locate {relativePath} walking up from {AppContext.BaseDirectory}");
    }
}
