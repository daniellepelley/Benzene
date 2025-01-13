using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Aws.Sns;

public class SnsMessageHeadersGetter : IMessageHeadersGetter<SnsRecordContext>
{
    public IDictionary<string, string> GetHeaders(SnsRecordContext context)
    {
        return context.SnsRecord.Sns.MessageAttributes
            .ToDictionary(x => x.Key, x => x.Value.Value);
    }
}
