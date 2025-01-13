using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Messages;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

public class BenzeneMessageGetter : IMessageGetter<BenzeneMessageContext>
{
    public IDictionary<string, string> GetHeaders(BenzeneMessageContext context)
    {
        return context.BenzeneMessageRequest.Headers ?? new Dictionary<string, string>();
    }

    public ITopic GetTopic(BenzeneMessageContext context)
    {
        if (context?.BenzeneMessageRequest?.Topic == null)
        {
            return new Topic(Messages.Constants.Missing.Id);
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