using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Http;

/// <summary>
/// Sets the HTTP status code on the response based on the message handler result.
/// </summary>
/// <typeparam name="TContext">The context type for the HTTP request/response.</typeparam>
/// <remarks>
/// This response handler translates Benzene result statuses to HTTP status codes using
/// an <see cref="IHttpStatusCodeMapper"/> and applies them to the HTTP response through
/// a response adapter. It should be registered in the response handler pipeline to ensure
/// that HTTP responses have appropriate status codes based on the handler's result.
/// </remarks>
public class HttpStatusCodeResponseHandler<TContext> : IResponseHandler<TContext> where TContext : class
{
    private readonly IHttpStatusCodeMapper _httpStatusCodeMapper;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpStatusCodeResponseHandler{TContext}"/> class.
    /// </summary>
    /// <param name="benzeneResponseAdapter">The adapter used to set the status code on the HTTP response.</param>
    /// <param name="httpStatusCodeMapper">The mapper that translates Benzene result statuses to HTTP status codes.</param>
    public HttpStatusCodeResponseHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter, IHttpStatusCodeMapper httpStatusCodeMapper)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _httpStatusCodeMapper = httpStatusCodeMapper;
    }

    /// <summary>
    /// Handles the response by setting the appropriate HTTP status code based on the message handler result.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="messageHandlerResult">The result from the message handler execution.</param>
    public ValueTask HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        _benzeneResponseAdapter.SetStatusCode(context, _httpStatusCodeMapper.Map(messageHandlerResult.BenzeneResult.Status));
        return default;
    }
}
