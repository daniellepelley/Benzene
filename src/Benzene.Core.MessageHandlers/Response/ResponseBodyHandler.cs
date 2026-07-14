using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Writes the handler's result as the response body (and JSON content type) using the registered
/// <see cref="IResponsePayloadMapper{TContext}"/> and <see cref="ISerializer"/>, unconditionally
/// overwriting any body already set. Registered by <c>AddBenzeneMessage</c> as an
/// <see cref="IResponseHandler{TContext}"/> for <c>BenzeneMessageContext</c>.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public class ResponseBodyHandler<TContext> : ISyncResponseHandler<TContext>
{
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseBodyHandler{TContext}"/> class.
    /// </summary>
    /// <param name="benzeneResponseAdapter">Writes the body and content type onto the transport context.</param>
    /// <param name="responsePayloadMapper">Maps the handler's result into a serialized response body.</param>
    /// <param name="serializer">The serializer used to produce the response body.</param>
    public ResponseBodyHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter, IResponsePayloadMapper<TContext> responsePayloadMapper, ISerializer serializer)
    {
        _serializer = serializer;
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _responsePayloadMapper = responsePayloadMapper;
    }

    /// <inheritdoc />
    public void HandleAsync(TContext context, IMessageHandlerResult result)
    {
        _benzeneResponseAdapter.SetBody(context, _responsePayloadMapper.Map(context, result, _serializer));
        _benzeneResponseAdapter.SetContentType(context, Constants.JsonContentType);
    }
}
