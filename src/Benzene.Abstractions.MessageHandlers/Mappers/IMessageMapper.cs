namespace Benzene.Abstractions.Mappers;

public interface IMessageMapper<TContext>
    : IMessageBodyMapper<TContext>, IMessageHeadersMapper<TContext>, IMessageTopicMapper<TContext> 
{}