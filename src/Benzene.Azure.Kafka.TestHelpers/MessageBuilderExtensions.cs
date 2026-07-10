using Benzene.Abstractions;
using Benzene.Core.MessageHandlers.Serialization;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Kafka.TestHelpers;

public static class MessageBuilderExtensions
{
    public static KafkaEventData<string> AsAzureKafkaEvent<T>(this IMessageBuilder<T> source)
    {
        return new KafkaEventData<string>
        {
            Topic = source.Topic,
            Value = new JsonSerializer().Serialize(source.Message)
        };
    }
}
