using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Sns;

public class SnsMessageHeadersMapper : IMessageHeadersMapper<SnsRecordContext>
{
    public IDictionary<string, string> GetHeaders(SnsRecordContext context)
    {
        return context.SnsRecord.Sns.MessageAttributes
            .ToDictionary(x => x.Key, x => x.Value.Value);
    }
}
