using Confluent.Kafka;

namespace Benzene.Kafka.Core.Kafka;

public class KafkaSendMessageContext
{
    public KafkaSendMessageContext(string topic, Message<string, string> message)
    {
        Message = message;
        Topic = topic;
    }
    public string Topic { get; }
    public Message<string, string> Message { get; }
    public DeliveryResult<string, string> Response { get; set; }
}