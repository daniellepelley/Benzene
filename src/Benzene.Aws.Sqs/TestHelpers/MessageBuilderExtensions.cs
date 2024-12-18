using System;
using System.Linq;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Serialization;

namespace Benzene.Aws.Sqs.TestHelpers;

public static class MessageBuilderExtensions
{
    public static SQSEvent AsSqs<T>(this IMessageBuilder<T> source, int numberOfMessages = 1)
    {
        return AsSqs(source, new JsonSerializer(), numberOfMessages);
    }
    
    public static SQSEvent AsSqs<T>(this IMessageBuilder<T> source, ISerializer serializer, int numberOfMessages = 1)
    {
        var headers = source.Headers.ToDictionary(x => x.Key, x => x.Value);
        headers.Add("topic", source.Topic);

        return new SQSEvent
        {
            Records = Enumerable.Range(0, numberOfMessages).Select(_ =>
                new SQSEvent.SQSMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    EventSource = "aws:sqs",
                    MessageAttributes = headers.ToDictionary(x => x.Key, x => new SQSEvent.MessageAttribute
                    {
                        StringValue = x.Value,
                        DataType = "String"
                    }),
                    Body = serializer.Serialize(source.Message)
                }
            ).ToList()
        };
    }
}
