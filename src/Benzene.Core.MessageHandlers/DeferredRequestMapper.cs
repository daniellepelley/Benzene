using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Request;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IDeferredRequestMapper"/> implementation: defers mapping of the transport context
/// into a strongly-typed request until the handler's request type is known (via the generic
/// <see cref="GetRequest{TRequest}"/> call), since the router itself only knows the topic being
/// dispatched, not the concrete request type, until a handler has been resolved.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type being mapped from.</typeparam>
internal class DeferredRequestMapper<TContext> : IDeferredRequestMapper
{
    private readonly TContext _context;
    private readonly IRequestMapper<TContext> _requestMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeferredRequestMapper{TContext}"/> class.
    /// </summary>
    /// <param name="requestMapper">Maps the context into a strongly-typed request on demand.</param>
    /// <param name="context">The transport context to map from.</param>
    public DeferredRequestMapper(IRequestMapper<TContext> requestMapper, TContext context)
    {
        _context = context;
        _requestMapper = requestMapper;
    }

    /// <summary>
    /// Maps the captured context into the requested strongly-typed request.
    /// </summary>
    /// <typeparam name="TRequest">The handler's request type to map into.</typeparam>
    /// <returns>The mapped request, or <c>null</c> if the context carries no body.</returns>
    public TRequest? GetRequest<TRequest>() where TRequest : class
    {
        return _requestMapper.GetBody<TRequest>(_context);
    }
}
