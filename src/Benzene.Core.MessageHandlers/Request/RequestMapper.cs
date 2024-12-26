using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Request;

public class RequestMapper<TContext> : IRequestMapper<TContext>
{
    private readonly IMessageBodyMapper<TContext> _messageBodyMapper;
    private readonly ISerializer _serializer;

    public RequestMapper(IMessageBodyMapper<TContext> messageBodyMapper, ISerializer serializer)
    {
        _serializer = serializer;
        _messageBodyMapper = messageBodyMapper;
    }
    
    public TRequest? GetBody<TRequest>(TContext context) where TRequest : class
    {
        if (context is IRequestContext<TRequest>)
        {
            return ((IRequestContext<TRequest?>)context).Request;
        }
        
        var bodyAsString = _messageBodyMapper.GetBody(context);

        if (!string.IsNullOrEmpty(bodyAsString))
        {
            return _serializer.Deserialize<TRequest>(bodyAsString);
        }

        return Activator.CreateInstance<TRequest>();
    }
}
