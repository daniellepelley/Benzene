using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions;

namespace Benzene.Aws.Sns;

public static class MessageBuilderExtensions
{
    public static SNSEvent AsSns(this IMessageBuilder source)
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
                        Message = System.Text.Json.JsonSerializer.Serialize(source.Message)
                    }
                }
            }
        };
    }
}
