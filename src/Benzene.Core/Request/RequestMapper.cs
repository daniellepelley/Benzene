using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
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
    
    public TRequest GetBody<TRequest>(TContext context) where TRequest : class
    {
        var bodyAsString = _messageBodyMapper.GetMessage(context);

        return string.IsNullOrEmpty(bodyAsString)
            ? null
            : _serializer.Deserialize<TRequest>(bodyAsString);
    }
}
