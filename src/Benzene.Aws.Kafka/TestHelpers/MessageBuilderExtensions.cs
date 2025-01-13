using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.Kafka.TestHelpers;

public static class MessageBuilderExtensions
{
    public static KafkaEvent AsAwsKafkaEvent<T>(this IMessageBuilder<T> source)
    {
        return AsAwsKafkaEvent(source, new JsonSerializer());
    }

    public static KafkaEvent AsAwsKafkaEvent<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        return new KafkaEvent
        {
            EventSource = "aws:kafka",
            Records = new Dictionary<string, IList<KafkaEvent.KafkaEventRecord>>
            {
                {
                    "some-id",
                    new List<KafkaEvent.KafkaEventRecord>
                    {
                        new()
                        {
                            Topic = source.Topic,
                            Value = ObjectToStream(source.Message, serializer),
                            Headers = new [] { source.Headers.ToDictionary(x => x.Key, x => Encoding.UTF8.GetBytes(x.Value)) as IDictionary<string, byte[]> }.ToList()
                        }
                    }
                }
            }
        };
    }

    private static MemoryStream ObjectToStream<T>(T obj, ISerializer serializer)
    {
        var byteArray = Encoding.UTF8.GetBytes(serializer.Serialize(obj));
        return new MemoryStream(byteArray);
    }
}
