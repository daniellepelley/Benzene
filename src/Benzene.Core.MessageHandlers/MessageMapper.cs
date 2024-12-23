using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.Mappers;

public class MessageMapper<TContext> : IMessageMapper<TContext>
{
    private readonly IMessageHeadersMapper<TContext> _messageHeadersMapper;
    private readonly IMessageTopicMapper<TContext> _messageTopicMapper;
    private readonly IMessageBodyMapper<TContext> _messageBodyMapper;

    public MessageMapper(IMessageTopicMapper<TContext> messageTopicMapper, IMessageBodyMapper<TContext> messageBodyMapper, IMessageHeadersMapper<TContext> messageHeadersMapper)
    {
        _messageHeadersMapper = messageHeadersMapper;
        _messageTopicMapper = messageTopicMapper;
        _messageBodyMapper = messageBodyMapper;
    }

    public string GetBody(TContext context)
    {
        return _messageBodyMapper.GetBody(context);
    }

    public IDictionary<string, string> GetHeaders(TContext context)
    {
        return _messageHeadersMapper.GetHeaders(context);
    }

    public ITopic GetTopic(TContext context)
    {
        return _messageTopicMapper.GetTopic(context);
    }
}


