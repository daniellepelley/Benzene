using Confluent.Kafka;

namespace Benzene.Kafka.Core;

public class BenzeneKafkaConfig
{
    public ConsumerConfig ConsumerConfig { get; set; }
    public string[] Topics { get; set; }
    public int ConcurrentRequests { get; set; } = 5;
}