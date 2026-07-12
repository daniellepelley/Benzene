using System.IO;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Extracts and UTF-8 decodes the message body from a Kafka record's value stream.
/// </summary>
public class KafkaMessageBodyGetter : IMessageBodyGetter<KafkaContext>
{
    /// <summary>
    /// Gets the UTF-8 decoded body from the Kafka record's value.
    /// </summary>
    /// <param name="context">The Kafka context to extract the body from.</param>
    /// <returns>The decoded message body.</returns>
    public string GetBody(KafkaContext context)
    {
        return StreamToString(context.KafkaEventRecord.Value);
    }

    private static string StreamToString(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
