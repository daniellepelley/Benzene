using System.Collections.Generic;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Helper;
using Benzene.Core.Mappers;

namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageMapper : IMessageMapper<BenzeneMessageContext>
{
    public IDictionary<string, string> GetHeaders(BenzeneMessageContext context)
    {
        return context.BenzeneMessageRequest.Headers ?? new Dictionary<string, string>();
    }

    public ITopic GetTopic(BenzeneMessageContext context)
    {
        return new Topic(
            context.BenzeneMessageRequest.Topic,
            context.BenzeneMessageRequest.Headers.GetValue("version"));
    }

    public string GetMessage(BenzeneMessageContext context)
    {
        return context.BenzeneMessageRequest.Body;
    }
}
