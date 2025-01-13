using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.Mappers;

public class MessageGetter<TContext> : IMessageGetter<TContext>
{
    private readonly IMessageHeadersGetter<TContext> _messageHeadersGetter;
    private readonly IMessageTopicGetter<TContext> _messageTopicGetter;
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;

    public MessageGetter(IMessageTopicGetter<TContext> messageTopicGetter, IMessageBodyGetter<TContext> messageBodyGetter, IMessageHeadersGetter<TContext> messageHeadersGetter)
    {
        _messageHeadersGetter = messageHeadersGetter;
        _messageTopicGetter = messageTopicGetter;
        _messageBodyGetter = messageBodyGetter;
    }

    public string GetBody(TContext context)
    {
        return _messageBodyGetter.GetBody(context);
    }

    public IDictionary<string, string> GetHeaders(TContext context)
    {
        return _messageHeadersGetter.GetHeaders(context);
    }

    public ITopic GetTopic(TContext context)
    {
        return _messageTopicGetter.GetTopic(context);
    }
}


