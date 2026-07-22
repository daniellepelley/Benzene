using System.Threading.RateLimiting;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Results;

namespace Benzene.RateLimiting;

/// <summary>
/// Best-effort, per-instance rate limiting over any <see cref="RateLimiter"/>
/// (System.Threading.RateLimiting). Each message attempts to acquire its permit cost (1 by
/// default; e.g. the payload's byte size for a bytes-per-second token bucket) without queuing;
/// a message the limiter rejects is short-circuited with a <c>TooManyRequests</c> result (HTTP
/// 429 via the standard status mapping). The acquired lease is held across <c>next()</c> so
/// concurrency-style limiters release correctly.
/// </summary>
/// <remarks>
/// This is deliberately simple protection for endpoints a service can't avoid exposing (health
/// checks, spec) — a brake on abuse and runaway serverless cost, not an exact science: the limit
/// is per service instance, so a fleet of N instances admits up to N× the configured rate.
/// Authoritative rate limiting belongs at the gateway in front of all instances.
/// </remarks>
/// <typeparam name="TContext">The transport-specific context type.</typeparam>
public class RateLimitingMiddleware<TContext> : IMiddleware<TContext> where TContext : class
{
    private readonly RateLimiter _rateLimiter;
    private readonly Func<IServiceResolver, TContext, int> _permitCost;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>Initializes the middleware over a shared limiter and a per-message permit cost.</summary>
    /// <param name="rateLimiter">The limiter, shared across every message on the pipeline (and any other pipeline given the same instance).</param>
    /// <param name="permitCost">Computes the permit cost of the current message (e.g. 1, or the payload size in bytes).</param>
    /// <param name="serviceResolver">The current message's scope, used to compute the cost and write the rejection result.</param>
    public RateLimitingMiddleware(RateLimiter rateLimiter, Func<IServiceResolver, TContext, int> permitCost,
        IServiceResolver serviceResolver)
    {
        _rateLimiter = rateLimiter;
        _permitCost = permitCost;
        _serviceResolver = serviceResolver;
    }

    public string Name => "RateLimiting";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var cost = Math.Max(0, _permitCost(_serviceResolver, context));

        RateLimitLease? lease = null;
        try
        {
            try
            {
                lease = _rateLimiter.AttemptAcquire(cost);
            }
            catch (ArgumentOutOfRangeException)
            {
                // The cost exceeds what the limiter could ever grant (e.g. a payload larger than
                // the whole token bucket) - that is a rejection, not an internal error.
            }

            if (lease is not { IsAcquired: true })
            {
                await SetTooManyRequestsAsync(context, lease);
                return;
            }

            await next();
        }
        finally
        {
            // Held across next() so a concurrency-style limiter's permits are returned when the
            // message completes; a no-op for window/bucket limiters.
            lease?.Dispose();
        }
    }

    private Task SetTooManyRequestsAsync(TContext context, RateLimitLease? lease)
    {
        var error = "Rate limit exceeded";
        if (lease != null && lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            error = $"Rate limit exceeded; retry after {retryAfter.TotalSeconds:0}s";
        }

        // Attach the topic's handler definition so the response pipeline writes the ErrorPayload
        // body (it skips definition-less results) - same pattern as Benzene.JsonSchema.
        var topicGetter = _serviceResolver.TryGetService<IMessageTopicGetter<TContext>>();
        var topic = topicGetter?.GetTopic(context);
        var definition = topic != null
            ? _serviceResolver.TryGetService<IMessageHandlerDefinitionLookUp>()?.FindHandler(topic)
            : null;

        var resultSetter = _serviceResolver.GetService<IMessageHandlerResultSetter<TContext>>();
        return resultSetter.SetResultAsync(context,
            new MessageHandlerResult(topic, definition, BenzeneResult.TooManyRequests(error)));
    }
}
