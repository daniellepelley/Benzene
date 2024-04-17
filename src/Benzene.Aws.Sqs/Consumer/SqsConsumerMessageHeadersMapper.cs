using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageHeadersMapper : IMessageHeadersMapper<SqsConsumerMessageContext>
{
    public IDictionary<string, string> GetHeaders(SqsConsumerMessageContext context)
    {
        return context.Message.MessageAttributes
            .Where(x => x.Value.DataType == "String")
            .ToDictionary(x => x.Key, x => x.Value.StringValue);
    }
}
