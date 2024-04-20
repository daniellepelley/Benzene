using Azure.Messaging.EventHubs;
using Benzene.Abstractions;
using Benzene.Core.DirectMessage;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Kafka;

public static class MessageBuilderExtensions
{
    public static EventData AsEventHubDirectMessage(this IMessageBuilder source)
    {
        return new EventData
        {
            EventBody = new BinaryData(source.AsDirectMessage())
        };
    }

    public static KafkaEventData<string> AsAzureKafkaEvent(this IMessageBuilder source)
    {
        return new KafkaEventData<string>
        {
            Topic = source.Topic,
            Value = new Benzene.Core.Serialization.JsonSerializer().Serialize(source.Message)
        };
    }

}
