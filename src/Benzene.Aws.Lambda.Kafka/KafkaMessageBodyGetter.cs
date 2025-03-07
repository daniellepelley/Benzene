﻿using System.IO;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.Kafka;

public class KafkaMessageBodyGetter : IMessageBodyGetter<KafkaContext>
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
