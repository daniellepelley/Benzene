using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.Sns.TestHelpers;

public static class MessageBuilderExtensions
{
    public static SNSEvent AsSns<T>(this IMessageBuilder<T> source)
    {
        return AsSns(source, new JsonSerializer());
    }

    public static SNSEvent AsSns<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        var headers = source.Headers.ToDictionary(x => x.Key, x => x.Value);
        headers.Add("topic", source.Topic);
        return new SNSEvent
        {
            Records = new List<SNSEvent.SNSRecord>
            {
                new SNSEvent.SNSRecord
                {
                    EventSource = "aws:sns",
                    Sns = new SNSEvent.SNSMessage
                    {
                        MessageAttributes = headers.ToDictionary(x => x.Key, x => new SNSEvent.MessageAttribute
                                {
                                    Value = x.Value,
                                    Type = "String"
                                }),
                        Message = serializer.Serialize(source.Message)
                    }
                }
            }
        };
    }
}
