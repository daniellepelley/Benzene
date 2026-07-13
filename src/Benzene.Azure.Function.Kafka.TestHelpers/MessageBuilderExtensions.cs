using System.Text;
using Benzene.Abstractions;
using Benzene.Core.MessageHandlers.Serialization;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Azure.Function.Kafka.TestHelpers;

public static class MessageBuilderExtensions
{
    public static KafkaRecord AsAzureKafkaEvent<T>(this IMessageBuilder<T> source)
    {
        return new KafkaRecord
        {
            Topic = source.Topic,
            Value = Encoding.UTF8.GetBytes(new JsonSerializer().Serialize(source.Message))
        };
    }
}
