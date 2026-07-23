using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Benzene.Test.Contract;

// Guards the 1.0 "safe-by-default" settlement contract (work/settlement-contract-1.0.md) against
// silent drift between the code and docs/capability-matrix.md:
//
//  * EverySettlementTransport_DefaultsToSafe pins each transport's *code* default - a returned
//    failure result is redelivered (at-least-once), not silently settled. Flip any default back to
//    the unsafe value and this fails.
//  * CapabilityMatrix_MarksTransportSafeByDefault pins the *doc*: the matrix row for each of those
//    transports must say "Safe by default". Let the matrix drift (or add a new unsafe transport
//    without documenting it) and this fails.
//
// Together they stop the capability matrix from ever again describing a transport's data-safety
// differently from how the code actually behaves - the exact drift that had the AWS/Azure rows
// claiming "unsafe, no opt-out" long after the settlement flip had made them safe.
public class SettlementContractDefaultsTest
{
    [Fact]
    public void EverySettlementTransport_DefaultsToSafe()
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

        // Azure self-hosted workers.
        Assert.True(new Benzene.Azure.EventHub.BenzeneEventHubConfig().RaiseOnFailureStatus);
        Assert.Equal(Benzene.Azure.ServiceBus.ServiceBusConsumerAckMode.Explicit,
            new Benzene.Azure.ServiceBus.BenzeneServiceBusConfig().AckMode);

        // RabbitMQ self-hosted worker.
        Assert.Equal(Benzene.RabbitMq.RabbitMqAckMode.Explicit,
            new Benzene.RabbitMq.RabbitMqConfig { QueueName = "guard" }.AckMode);

        // Google Cloud Pub/Sub.
        Assert.True(new Benzene.GoogleCloud.Functions.PubSub.PubSubOptions().RaiseOnFailureStatus);
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
    [InlineData("Benzene.Azure.EventHub")]
    [InlineData("Benzene.Azure.ServiceBus")]
    [InlineData("Benzene.RabbitMq")]
    [InlineData("Benzene.GoogleCloud.Functions.PubSub")]
    public void CapabilityMatrix_MarksTransportSafeByDefault(string packageId)
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
        Assert.True(rows[0].Contains("Safe by default"),
            $"The capability-matrix.md row for {token} must say \"Safe by default\" - it has drifted " +
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
