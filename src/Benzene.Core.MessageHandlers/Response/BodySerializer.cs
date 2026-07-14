using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Default <see cref="IBodySerializer"/> implementation: binds an <see cref="IResponsePayloadMapper{TContext}"/>
/// and a specific context instance together, so callers that only know the serializer to use (e.g.
/// <see cref="JsonSerializationResponseHandler{TContext}"/>) can produce a body without threading the
/// context through separately.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public class BodySerializer<TContext> : IBodySerializer
{
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly TContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="BodySerializer{TContext}"/> class.
    /// </summary>
    /// <param name="responsePayloadMapper">Maps a handler's result into a serialized response body.</param>
    /// <param name="context">The fixed context to map the result for.</param>
    public BodySerializer(IResponsePayloadMapper<TContext> responsePayloadMapper, TContext context)
    {
        _context = context;
        _responsePayloadMapper = responsePayloadMapper;
    }

    /// <inheritdoc />
    public string Serialize(ISerializer serializer, IMessageHandlerResult messageHandlerResult)
    {
        return _responsePayloadMapper.Map(_context, messageHandlerResult, serializer);
    }
}
