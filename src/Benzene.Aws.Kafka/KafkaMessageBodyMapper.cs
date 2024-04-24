using System.IO;
using System.Text;
using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Kafka;

public class KafkaMessageBodyMapper : IMessageBodyMapper<KafkaContext>
{
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
