using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// Response handler that copies the handler's result status onto the transport response's status
/// code via <see cref="IBenzeneResponseAdapter{TContext}.SetStatusCode"/>. Registered by
/// <c>AddBenzeneMessage</c> alongside <see cref="Response.RendererResponseHandler{TContext}"/> so
/// both body and status are written for every <c>BenzeneMessage</c> response.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
/// <remarks>
/// <see cref="DefaultStatuses"/> supplies the *error* status codes the pipeline reports on failure;
/// this type is unrelated - it simply propagates whatever status <see cref="IMessageHandlerResult.BenzeneResult"/>
/// already carries onto the response.
/// </remarks>
internal class DefaultResponseStatusHandler<TContext> : IResponseHandler<TContext> where TContext : class
{
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultResponseStatusHandler{TContext}"/> class.
    /// </summary>
    /// <param name="benzeneResponseAdapter">Writes the status code onto the transport context.</param>
    public DefaultResponseStatusHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
    }

    /// <inheritdoc />
    public ValueTask HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        _benzeneResponseAdapter.SetStatusCode(context, messageHandlerResult.BenzeneResult.Status);
        return default;
    }
}
