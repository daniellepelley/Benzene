using System.Collections.Generic;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Core.MessageHandlers;
using Utils = Benzene.Core.Helper.Utils;

namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageMapper : IMessageMapper<BenzeneMessageContext>
{
    public IDictionary<string, string> GetHeaders(BenzeneMessageContext context)
    {
        return context.BenzeneMessageRequest.Headers ?? new Dictionary<string, string>();
    }

    public ITopic GetTopic(BenzeneMessageContext context)
    {
        if (context?.BenzeneMessageRequest?.Topic == null)
        {
            return new Topic(Constants.Missing);
        }

        return new Topic(
            context.BenzeneMessageRequest.Topic,
            Utils.GetValue(context.BenzeneMessageRequest.Headers, "version"));
    }

    public string GetBody(BenzeneMessageContext context)
    {
        return context.BenzeneMessageRequest.Body;
    }
}