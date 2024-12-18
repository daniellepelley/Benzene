using Benzene.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Kafka.TestHelpers;

public static class MessageBuilderExtensions
{
    public static KafkaEventData<string> AsAzureKafkaEvent<T>(this IMessageBuilder<T> source)
    {
        return new KafkaEventData<string>
        {
            Topic = source.Topic,
            Value = new Benzene.Core.Serialization.JsonSerializer().Serialize(source.Message)
        };
    }
}
