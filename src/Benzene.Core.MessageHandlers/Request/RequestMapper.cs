using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Request;

public class RequestMapper<TContext> : IRequestMapper<TContext>
{
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly ISerializer _serializer;

    public RequestMapper(IMessageBodyGetter<TContext> messageBodyGetter, ISerializer serializer)
    {
        _serializer = serializer;
        _messageBodyGetter = messageBodyGetter;
    }
    
    public TRequest? GetBody<TRequest>(TContext context) where TRequest : class
    {
        if (context is IRequestContext<TRequest> requestContext)
        {
            return requestContext.Request;
        }
        
        var bodyAsString = _messageBodyGetter.GetBody(context);

        return !string.IsNullOrEmpty(bodyAsString)
            ? _serializer.Deserialize<TRequest>(bodyAsString)
            : Activator.CreateInstance<TRequest>();
    }
}
