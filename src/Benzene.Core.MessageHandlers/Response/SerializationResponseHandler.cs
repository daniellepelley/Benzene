using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Writes the handler's result as the response body, in whichever <see cref="IMediaFormat{TContext}"/>
/// <see cref="IMediaFormatNegotiator{TContext}"/> selects for the current message (JSON by default;
/// XML, or any other registered format, when negotiated via <c>accept</c>/<c>content-type</c>), unless
/// a body has already been set by an earlier response handler. The single response-writing handler
/// every transport registers, replacing the pre-Phase-2 per-format stack
/// (<c>ResponseHandler{T,TContext}</c> + <c>ISerializationResponseHandler{TContext}</c> +
/// <c>JsonSerializationResponseHandler</c>/<c>XmlSerializationResponseHandler</c> +
/// <c>IBodySerializer</c>/<c>BodySerializer{TContext}</c> + <c>ResponseBodyHandler{TContext}</c>).
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public class SerializationResponseHandler<TContext> : IResponseHandler<TContext> where TContext : class
{
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly IMediaFormatNegotiator<TContext> _mediaFormatNegotiator;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationResponseHandler{TContext}"/> class.
    /// </summary>
    /// <param name="benzeneResponseAdapter">Reads/writes the body and content type on the transport context.</param>
    /// <param name="responsePayloadMapper">Maps the handler's result into a serialized response body.</param>
    /// <param name="mediaFormatNegotiator">Selects which format to write the response in.</param>
    /// <param name="serviceResolver">Resolver used to obtain the negotiated format's serializer.</param>
    public SerializationResponseHandler(
        IBenzeneResponseAdapter<TContext> benzeneResponseAdapter,
        IResponsePayloadMapper<TContext> responsePayloadMapper,
        IMediaFormatNegotiator<TContext> mediaFormatNegotiator,
        IServiceResolver serviceResolver)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _responsePayloadMapper = responsePayloadMapper;
        _mediaFormatNegotiator = mediaFormatNegotiator;
        _serviceResolver = serviceResolver;
    }

    /// <inheritdoc />
    public ValueTask HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        if (!string.IsNullOrEmpty(_benzeneResponseAdapter.GetBody(context)))
        {
            return default;
        }

        var format = _mediaFormatNegotiator.SelectWrite(context);
        var serializer = format.GetSerializer(_serviceResolver);

        _benzeneResponseAdapter.SetBody(context, _responsePayloadMapper.Map(context, messageHandlerResult, serializer));
        _benzeneResponseAdapter.SetContentType(context, format.ContentType);

        return default;
    }
}
