using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Adapts an <see cref="ISerializationResponseHandler{TContext}"/> (which needs an
/// <see cref="IBodySerializer"/> to do its work) to the plain <see cref="ISyncResponseHandler{TContext}"/>
/// shape a <see cref="Benzene.Abstractions.MessageHandlers.Response.IResponseHandlerContainer{TContext}"/>
/// expects, by binding the current context's <see cref="IResponsePayloadMapper{TContext}"/> into a
/// <see cref="BodySerializer{TContext}"/> for it to use.
/// </summary>
/// <typeparam name="T">The serialization response handler to delegate to.</typeparam>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public class ResponseHandler<T, TContext> : ISyncResponseHandler<TContext> where T : ISerializationResponseHandler<TContext> where TContext : class
{
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly T _httpSerializationResponseHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseHandler{T,TContext}"/> class.
    /// </summary>
    /// <param name="httpSerializationResponseHandler">The serialization-specific handler to delegate the actual body writing to.</param>
    /// <param name="responsePayloadMapper">Maps the handler's result into a serialized response body.</param>
    public ResponseHandler(T httpSerializationResponseHandler, IResponsePayloadMapper<TContext> responsePayloadMapper)
    {
        _httpSerializationResponseHandler = httpSerializationResponseHandler;
        _responsePayloadMapper = responsePayloadMapper;
    }

    /// <inheritdoc />
    public void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        var httpHttpBodySerializer = new BodySerializer<TContext>(_responsePayloadMapper, context);
        _httpSerializationResponseHandler.HandleAsync(context, messageHandlerResult, httpHttpBodySerializer);
    }
}
