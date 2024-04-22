using System.Collections.Generic;
using System.IO;
using System.Text;
using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions;

namespace Benzene.Aws.Kafka.TestHelpers;

public static class MessageBuilderExtensions
{
    public static KafkaEvent AsAwsKafkaEvent(this IMessageBuilder source)
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
                        new() { Topic = source.Topic, Value = ObjectToStream(source.Message) }
                    }
                }
            }
        };
    }

    private static MemoryStream ObjectToStream(object obj)
    {
        var byteArray = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(obj));
        return new MemoryStream(byteArray);
    }
}
