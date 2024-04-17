using System.Collections.Generic;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Helper;
using Benzene.Core.Mappers;

namespace Benzene.Core.DirectMessage;

public class DirectMessageMapper : IMessageMapper<DirectMessageContext>
{
    public IDictionary<string, string> GetHeaders(DirectMessageContext context)
    {
        return context.DirectMessageRequest.Headers ?? new Dictionary<string, string>();
    }

    public ITopic GetTopic(DirectMessageContext context)
    {
        return new Topic(
            context.DirectMessageRequest.Topic,
            context.DirectMessageRequest.Headers.GetValue("version"));
    }

    public string GetMessage(DirectMessageContext context)
    {
        return context.DirectMessageRequest.Message;
    }
}
