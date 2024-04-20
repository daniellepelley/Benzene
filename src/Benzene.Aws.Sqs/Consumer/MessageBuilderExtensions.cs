using System.Linq;
using Amazon.SQS.Model;
using Benzene.Abstractions;

namespace Benzene.Aws.Sqs.Consumer;

public static class MessageBuilderExtensions
{
    public static Message AsSqsMessage(this IMessageBuilder source)
    {
        var headers = source.Headers.ToDictionary(x => x.Key, x => x.Value);
        headers.Add("topic", source.Topic);

        return new Message
        {
            MessageAttributes = headers.ToDictionary(x => x.Key, x => new MessageAttributeValue
            {
                StringValue = x.Value,
                DataType = "String"
            }),
            Body = System.Text.Json.JsonSerializer.Serialize(source.Message)
        };
    }
}
