using System.Text;
using System.Threading.RateLimiting;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;

namespace Benzene.RateLimiting;

/// <summary>
/// Pipeline extensions for best-effort, per-instance rate limiting. Place the call <b>before</b>
/// the middleware it should protect (e.g. before <c>UseHealthCheck</c>/<c>UseSpec</c>/
/// <c>UseMessageHandlers</c>). The limit is per service instance — authoritative limiting belongs
/// at the gateway; see docs/rate-limiting.md.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Rate-limits the pipeline with a caller-supplied (bring-your-own) <see cref="RateLimiter"/>,
    /// costing one permit per message. The limiter instance is shared for the pipeline's lifetime —
    /// the caller owns its disposal (for a process-lifetime pipeline that is process exit).
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="rateLimiter">Any limiter: fixed/sliding window, token bucket, concurrency, partitioned, or custom.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseRateLimiting<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, RateLimiter rateLimiter)
        where TContext : class
    {
        return app.UseRateLimiting(rateLimiter, (_, _) => 1);
    }

    /// <summary>
    /// Rate-limits the pipeline with a caller-supplied <see cref="RateLimiter"/> and a
    /// caller-supplied per-message permit cost (e.g. a payload-derived weight).
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="rateLimiter">Any limiter; shared for the pipeline's lifetime, disposal owned by the caller.</param>
    /// <param name="permitCost">Computes the current message's permit cost from the message scope and context.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseRateLimiting<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, RateLimiter rateLimiter,
        Func<IServiceResolver, TContext, int> permitCost)
        where TContext : class
    {
        return app.Use(resolver => new RateLimitingMiddleware<TContext>(rateLimiter, permitCost, resolver));
    }

    /// <summary>
    /// Rate-limits to at most <paramref name="permitLimit"/> messages per <paramref name="window"/>
    /// (a <see cref="FixedWindowRateLimiter"/>; no queuing — excess messages get
    /// <c>TooManyRequests</c> immediately). The simple guard for utility endpoints like health checks.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="permitLimit">Messages allowed per window.</param>
    /// <param name="window">The window length.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseFixedWindowRateLimiting<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, int permitLimit, TimeSpan window)
        where TContext : class
    {
        return app.UseRateLimiting(new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    }

    /// <summary>
    /// Rate-limits messages through a <see cref="TokenBucketRateLimiter"/>: bursts up to
    /// <paramref name="tokenLimit"/>, refilled with <paramref name="tokensPerPeriod"/> every
    /// <paramref name="replenishmentPeriod"/>. One token per message; no queuing.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="tokenLimit">The bucket size (maximum burst).</param>
    /// <param name="tokensPerPeriod">Tokens restored each period.</param>
    /// <param name="replenishmentPeriod">How often tokens are restored.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseTokenBucketRateLimiting<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, int tokenLimit, int tokensPerPeriod,
        TimeSpan replenishmentPeriod)
        where TContext : class
    {
        return app.UseRateLimiting(CreateTokenBucket(tokenLimit, tokensPerPeriod, replenishmentPeriod));
    }

    /// <summary>
    /// Rate-limits by <b>payload size</b>: a token bucket where each message costs its request
    /// body's size in UTF-8 bytes (a bodyless message costs 1), allowing up to
    /// <paramref name="bytesPerPeriod"/> bytes every <paramref name="replenishmentPeriod"/> with
    /// bursts up to <paramref name="maxBurstBytes"/>. A single payload larger than
    /// <paramref name="maxBurstBytes"/> is always rejected.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="maxBurstBytes">The bucket size — the most bytes admissible at once.</param>
    /// <param name="bytesPerPeriod">Bytes restored each period (the sustained rate).</param>
    /// <param name="replenishmentPeriod">How often the byte budget is restored.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UsePayloadSizeRateLimiting<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, int maxBurstBytes, int bytesPerPeriod,
        TimeSpan replenishmentPeriod)
        where TContext : class
    {
        return app.UseRateLimiting(
            CreateTokenBucket(maxBurstBytes, bytesPerPeriod, replenishmentPeriod),
            static (resolver, context) =>
            {
                var body = resolver.TryGetService<IMessageBodyGetter<TContext>>()?.GetBody(context);
                return string.IsNullOrEmpty(body) ? 1 : Encoding.UTF8.GetByteCount(body);
            });
    }

    private static TokenBucketRateLimiter CreateTokenBucket(int tokenLimit, int tokensPerPeriod,
        TimeSpan replenishmentPeriod)
    {
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = tokenLimit,
            TokensPerPeriod = tokensPerPeriod,
            ReplenishmentPeriod = replenishmentPeriod,
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }
}
