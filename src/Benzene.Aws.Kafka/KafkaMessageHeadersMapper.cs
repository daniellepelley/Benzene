﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Kafka;

public class KafkaMessageHeadersMapper : IMessageHeadersMapper<KafkaContext>
{
    public IDictionary<string, string> GetHeaders(KafkaContext context)
    {
        var headers = context.KafkaEventRecord.Headers.FirstOrDefault();

        if (headers == null)
        {
            return new Dictionary<string, string>();
        }

        return headers.ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.Value));
    }
}
