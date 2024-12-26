using System.Collections.Generic;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Aws.Sns;

namespace Benzene.Test.Aws.Sns.Examples;

public class CustomSnsEnricher : IRequestEnricher<SnsRecordContext>
{
    public IDictionary<string, object> Enrich<TRequest>(TRequest request, SnsRecordContext context)
    {
        return new Dictionary<string, object>
        {
            { "mapped", context.SnsRecord.Sns.MessageId }
        };
    }
}
