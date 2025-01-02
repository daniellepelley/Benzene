using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Lambda.Sqs;

public class SqsMessageHeadersMapper : IMessageHeadersMapper<SqsMessageContext>
{
    public IDictionary<string, string> GetHeaders(SqsMessageContext context)
    {
        return context.SqsMessage.MessageAttributes
            .Where(x => x.Value.DataType == "String")
            .ToDictionary(x => x.Key, x => x.Value.StringValue);
    }
}
