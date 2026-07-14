using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Renders the handler's result in whichever <see cref="IMediaFormat{TContext}"/>
/// <see cref="IMediaFormatNegotiator{TContext}"/> selects for the current message (JSON by default;
/// XML, or any other registered format, when negotiated via <c>accept</c>/<c>content-type</c>). The
/// catch-all <see cref="IResponseRenderer{TContext}"/> every transport registers last, wrapped by
/// <see cref="RendererResponseHandler{TContext}"/> (replacing Phase 2's
/// <c>SerializationResponseHandler{TContext}</c>).
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public class SerializerResponseRenderer<TContext> : IResponseRenderer<TContext> where TContext : class
{
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly IMediaFormatNegotiator<TContext> _mediaFormatNegotiator;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializerResponseRenderer{TContext}"/> class.
    /// </summary>
    /// <param name="responsePayloadMapper">Maps the handler's result into a serialized response body.</param>
    /// <param name="mediaFormatNegotiator">Selects which format to write the response in.</param>
    /// <param name="serviceResolver">Resolver used to obtain the negotiated format's serializer.</param>
    public SerializerResponseRenderer(
        IResponsePayloadMapper<TContext> responsePayloadMapper,
        IMediaFormatNegotiator<TContext> mediaFormatNegotiator,
        IServiceResolver serviceResolver)
    {
        _responsePayloadMapper = responsePayloadMapper;
        _mediaFormatNegotiator = mediaFormatNegotiator;
        _serviceResolver = serviceResolver;
    }

    /// <summary>The catch-all: always applies, so this must be registered last.</summary>
    public bool CanRender(TContext context, IMessageHandlerResult result, IServiceResolver resolver) => true;

    /// <inheritdoc />
    public Task RenderAsync(TContext context, IMessageHandlerResult result, IBenzeneResponseAdapter<TContext> response)
    {
        var format = _mediaFormatNegotiator.SelectWrite(context);
        var serializer = format.GetSerializer(_serviceResolver);

        response.SetBody(context, _responsePayloadMapper.Map(context, result, serializer));
        response.SetContentType(context,
            result.BenzeneResult.PayloadAsObject is IRawContentMessage rawContentMessage
                ? rawContentMessage.ContentType
                : format.ContentType);

        return Task.CompletedTask;
    }
}
