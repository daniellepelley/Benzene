using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Clients
{
    public class RetryBenzeneMessageClient : IBenzeneMessageClient
    {
        private readonly IBenzeneMessageClient _inner;
        private readonly int _numberOfRetries;

        public RetryBenzeneMessageClient(IBenzeneMessageClient inner, int numberOfRetries = 3)
        {
            _numberOfRetries = numberOfRetries;
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
        {
            for (var i = 0; i < _numberOfRetries; i++)
            {
                var result = await _inner.SendMessageAsync<TRequest, TResponse>(request);
                if (!result.IsServiceUnavailable())
                {
                    return result;
                }
            }

            return BenzeneResult.ServiceUnavailable<TResponse>();
        }
    }
}
