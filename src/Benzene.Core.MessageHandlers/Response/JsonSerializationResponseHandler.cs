using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// <see cref="ISerializationResponseHandler{TContext}"/> that writes the response body as JSON
/// (using a fresh <see cref="JsonSerializer"/>, independent of whatever <c>ISerializer</c> is
/// registered for the request), unless a body has already been set by an earlier response handler.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public class JsonSerializationResponseHandler<TContext> : ISerializationResponseHandler<TContext> where TContext : class
{
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializationResponseHandler{TContext}"/> class.
    /// </summary>
    /// <param name="benzeneResponseAdapter">Reads/writes the body and content type on the transport context.</param>
    public JsonSerializationResponseHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
    }

    /// <inheritdoc />
    public void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult, IBodySerializer bodySerializer)
    {
        if (!string.IsNullOrEmpty(_benzeneResponseAdapter.GetBody(context)))
        {
            return;
        }

        _benzeneResponseAdapter.SetBody(context, bodySerializer.Serialize(new JsonSerializer(), messageHandlerResult));
        _benzeneResponseAdapter.SetContentType(context, "application/json");
    }
}
