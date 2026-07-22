using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Benzene.HealthChecks.Core;
using Benzene.Kafka.Core;
using Confluent.Kafka;
using Moq;
using Xunit;

namespace Benzene.Test.Kafka;

public class KafkaHealthCheckTest
{
    private const string Brokers = "broker1:9092,broker2:9092";

    private static IKafkaAdminClientFactory FactoryReturning(Metadata metadata)
    {
        var admin = new Mock<IAdminClient>();
        admin.Setup(x => x.GetMetadata(It.IsAny<TimeSpan>())).Returns(metadata);
        var factory = new Mock<IKafkaAdminClientFactory>();
        factory.Setup(x => x.AdminClient).Returns(admin.Object);
        return factory.Object;
    }

    private static IKafkaAdminClientFactory FactoryThrowing(Exception ex)
    {
        var admin = new Mock<IAdminClient>();
        admin.Setup(x => x.GetMetadata(It.IsAny<TimeSpan>())).Throws(ex);
        var factory = new Mock<IKafkaAdminClientFactory>();
        factory.Setup(x => x.AdminClient).Returns(admin.Object);
        return factory.Object;
    }

    private static Metadata MetadataWithTopics(params string[] topics)
    {
        var brokers = new List<BrokerMetadata> { new(0, "broker1", 9092) };
        var topicMetadata = topics
            .Select(t => new TopicMetadata(t, new List<PartitionMetadata>(), new Error(ErrorCode.NoError)))
            .ToList();
        return new Metadata(brokers, topicMetadata, 0, "broker1");
    }

    [Fact]
    public async Task ExecuteAsync_BrokerReachable_TopicsPresent_ReturnsHealthy_NonDestructively()
    {
        var factory = FactoryReturning(MetadataWithTopics("orders", "invoices"));
        var check = new KafkaHealthCheck(factory, Brokers, new[] { "orders", "invoices" });

        var result = await check.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("Kafka", check.Type);
        Assert.Contains(result.Dependencies, d => d.Kind == "Broker" && d.Name == Brokers);
        Assert.Contains(result.Dependencies, d => d.Kind == "Topic" && d.Name == "orders");
        Assert.Contains(result.Dependencies, d => d.Kind == "Topic" && d.Name == "invoices");
    }

    [Fact]
    public async Task ExecuteAsync_SubscribedTopicMissing_ReturnsUnhealthy_NamingIt()
    {
        var factory = FactoryReturning(MetadataWithTopics("orders")); // "invoices" not present on the cluster
        var check = new KafkaHealthCheck(factory, Brokers, new[] { "orders", "invoices" });

        var result = await check.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Contains("invoices", result.Data["MissingTopics"].ToString());
    }

    [Fact]
    public async Task ExecuteAsync_BrokerUnreachable_ReturnsFailed_WithTheErrorCode()
    {
        var factory = FactoryThrowing(new KafkaException(new Error(ErrorCode.Local_Transport, "broker down")));
        var check = new KafkaHealthCheck(factory, Brokers, new[] { "orders" });

        var result = await check.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("Local_Transport", result.Data["ErrorCode"]);
        Assert.Equal("KafkaException", result.Data["Error"]);
    }

    [Fact]
    public async Task ExecuteAsync_NotAuthorized_IsPersistentFailure()
    {
        var factory = FactoryThrowing(new KafkaException(new Error(ErrorCode.ClusterAuthorizationFailed, "no acl")));
        var check = new KafkaHealthCheck(factory, Brokers, new[] { "orders" });

        var result = await check.ExecuteAsync();

        // A Kafka authorization failure is a Warning, not a failure (§3.9).
        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.True(result.IsPersistent);
        Assert.Equal("ClusterAuthorizationFailed", result.Data["ErrorCode"]);
        Assert.Equal(403, result.Data["StatusCode"]);
    }
}
