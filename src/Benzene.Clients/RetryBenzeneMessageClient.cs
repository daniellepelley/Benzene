using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Clients
{
    /// <summary>
    /// Decorates an <see cref="IBenzeneMessageClient"/> with a simple retry loop. By default it
    /// retries results whose status is <c>ServiceUnavailable</c> or <c>TooManyRequests</c> —
    /// transient conditions where the request was not processed. <c>Timeout</c> is deliberately
    /// <b>not</b> retried by default: a timed-out operation may have been applied, so blind
    /// retries are only safe for idempotent calls — opt in with the <c>shouldRetry</c> predicate
    /// (e.g. <c>r =&gt; BenzeneResultStatus.IsTransient(r.Status)</c>).
    /// </summary>
    public class RetryBenzeneMessageClient : IBenzeneMessageClient
    {
        private readonly IBenzeneMessageClient _inner;
        private readonly int _numberOfRetries;
        private readonly Func<IBenzeneResult, bool> _shouldRetry;

        public RetryBenzeneMessageClient(IBenzeneMessageClient inner, int numberOfRetries = 3)
            : this(inner, numberOfRetries, null)
        { }

        /// <param name="inner">The client to decorate.</param>
        /// <param name="numberOfRetries">The maximum number of attempts.</param>
        /// <param name="shouldRetry">
        /// Predicate deciding whether a result warrants another attempt. Defaults to
        /// <c>ServiceUnavailable</c> or <c>TooManyRequests</c>.
        /// </param>
        public RetryBenzeneMessageClient(IBenzeneMessageClient inner, int numberOfRetries,
            Func<IBenzeneResult, bool>? shouldRetry)
        {
            _numberOfRetries = numberOfRetries;
            _inner = inner;
            _shouldRetry = shouldRetry ?? DefaultShouldRetry;
        }

        private static bool DefaultShouldRetry(IBenzeneResult result)
        {
            return result.IsServiceUnavailable() || result.IsTooManyRequests();
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
        {
            IBenzeneResult<TResponse> result = BenzeneResult.ServiceUnavailable<TResponse>();
            for (var i = 0; i < _numberOfRetries; i++)
            {
                result = await _inner.SendMessageAsync<TRequest, TResponse>(request);
                if (!_shouldRetry(result))
                {
                    return result;
                }
            }

            // Exhausted: return the last result rather than synthesizing a fresh one, so the
            // caller keeps the true status and any errors it carried.
            return result;
        }
    }
}
