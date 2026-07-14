using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Base <see cref="IMessageHandlerResultSetter{TContext}"/> implementation for transports that report
/// a handler's outcome by writing a response back through the pipeline's registered
/// <see cref="IResponseHandler{TContext}"/>s (headers, status code, body), e.g. HTTP-style or
/// direct request/response transports such as <c>BenzeneMessage</c>.
/// </summary>
/// <typeparam name="TContext">The transport context type, constrained to a reference type since it is written to.</typeparam>
/// <remarks>
/// The "Message" + "MessageHandlerResultSetter" naming reflects what the type does, not a typo: it
/// is a setter that implements <c>IMessageHandlerResultSetter&lt;TContext&gt;</c> (the "MessageHandlerResultSetter"
/// part) by writing the outbound message/response (the "Message" part) via an
/// <see cref="IResponseHandlerContainer{TContext}"/>, as opposed to
/// <see cref="MessageMessageHandlerResultSetterBase{TContext}"/>, which records a simple pass/fail
/// outcome onto the context instead of producing a response.
/// </remarks>
public class ResponseMessageMessageHandlerResultSetterBase<TContext> : IMessageHandlerResultSetter<TContext> where TContext : class
{
    private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseMessageMessageHandlerResultSetterBase{TContext}"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">Runs the registered response handlers to produce the outbound response.</param>
    public ResponseMessageMessageHandlerResultSetterBase(IResponseHandlerContainer<TContext> responseHandlerContainer)
    {
        _responseHandlerContainer = responseHandlerContainer;
    }

    /// <summary>
    /// Runs every registered response handler over the handler's outcome, writing headers, status
    /// code and body onto <paramref name="context"/> via the underlying response adapter.
    /// </summary>
    /// <param name="context">The transport context to write the response onto.</param>
    /// <param name="messageHandlerResult">The outcome of routing and handling the message.</param>
    public async Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        await _responseHandlerContainer.HandleAsync(context, messageHandlerResult);
    }
}
