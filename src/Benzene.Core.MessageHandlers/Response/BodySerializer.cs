using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Response;

public class BodySerializer<TContext> : IBodySerializer
{
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly TContext _context;

    public BodySerializer(IResponsePayloadMapper<TContext> responsePayloadMapper, TContext context)
    {
        _context = context;
        _responsePayloadMapper = responsePayloadMapper;
    }

    public string Serialize(ISerializer serializer, IMessageHandlerResult messageHandlerResult)
    {
        return _responsePayloadMapper.Map(_context, messageHandlerResult, serializer);
    }
}
