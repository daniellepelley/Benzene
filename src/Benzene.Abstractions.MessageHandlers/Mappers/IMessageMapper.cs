using Benzene.Abstractions.Mappers;

namespace Benzene.Abstractions.MessageHandlers.Mappers;

public interface IMessageMapper<TContext>
    : IMessageBodyMapper<TContext>, IMessageHeadersMapper<TContext>, IMessageTopicMapper<TContext> 
{}