using Benzene.Kafka.Core;
using Confluent.Kafka;
using Xunit;

namespace Benzene.Test.Kafka;

public class BenzeneKafkaConfigTest
{
    [Fact]
    public void CatchHandlerExceptions_DefaultsToTrue()
    {
        var config = new BenzeneKafkaConfig { ConsumerConfig = new ConsumerConfig(), Topics = new[] { "some-topic" } };

        Assert.True(config.CatchHandlerExceptions);
    }
}
