using Benzene.Abstractions;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.CorrelationId;

/// <summary>
/// Outbound middleware that stamps the current <see cref="ICorrelationId"/> value onto
/// <see cref="OutboundContext.Headers"/>. The middleware-pipeline replacement for
/// <see cref="CorrelationIdBenzeneMessageClient"/> - see
/// <c>work/benzene-clients-redesign-plan.md</c> §2.4.
/// </summary>
public class CorrelationIdMiddleware : IMiddleware<OutboundContext>
{
    private readonly ICorrelationId _correlationId;
    private readonly string _correlationKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="correlationId">The correlation ID provider to read from.</param>
    /// <param name="correlationKey">The header key to stamp the correlation ID onto.</param>
    public CorrelationIdMiddleware(ICorrelationId correlationId, string correlationKey = "correlationId")
    {
        _correlationId = correlationId;
        _correlationKey = correlationKey;
    }

    /// <summary>Gets the name of this middleware.</summary>
    public string Name => nameof(CorrelationIdMiddleware);

    /// <summary>
    /// Stamps the current correlation ID onto the outbound headers, then continues the pipeline.
    /// </summary>
    /// <param name="context">The outbound context to stamp headers onto.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    public Task HandleAsync(OutboundContext context, Func<Task> next)
    {
        context.Headers[_correlationKey] = _correlationId.Get();
        return next();
    }
}
