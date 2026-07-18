namespace Benzene.Resilience.Polly;

/// <summary>
/// The sentinel exception <see cref="PollyResilienceMiddleware{TContext}"/> throws internally when a
/// configured <c>isFailure</c> predicate reports that the pipeline produced an unsuccessful result
/// (rather than throwing). This lets a Polly <see cref="global::Polly.ResiliencePipeline"/> treat an
/// unsuccessful <c>IBenzeneResult</c> as a handled outcome - retry it, trip a circuit breaker on it,
/// fall back from it - since Benzene middleware reports domain failure as a result on the context,
/// not as a thrown exception (see docs/specification/core-concepts.md §5).
/// </summary>
/// <remarks>
/// To act on failure results, configure your Polly pipeline to handle this type, e.g.
/// <c>.AddRetry(new RetryStrategyOptions { ShouldHandle = new PredicateBuilder().Handle&lt;BenzeneFailureResultException&gt;() })</c>.
/// The middleware never lets this exception escape: once the pipeline has finished (all retries
/// exhausted), it is swallowed and the last unsuccessful result remains on the context, exactly as
/// if no resilience middleware were present. A <em>real</em> exception thrown by the pipeline is
/// never wrapped and propagates normally.
/// </remarks>
public sealed class BenzeneFailureResultException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="BenzeneFailureResultException"/> class.</summary>
    public BenzeneFailureResultException()
        : base("The Benzene pipeline produced an unsuccessful result (surfaced to Polly as a handled outcome).")
    {
    }
}
