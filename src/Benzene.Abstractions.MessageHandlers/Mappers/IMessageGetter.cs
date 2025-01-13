namespace Benzene.Abstractions.MessageHandlers.Mappers;

public interface IMessageGetter<TContext>
    : IMessageBodyGetter<TContext>, IMessageHeadersGetter<TContext>, IMessageTopicGetter<TContext> 
{}