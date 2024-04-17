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

        public async Task<IClientResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message, IDictionary<string, string> headers)
        {
            for (var i = 0; i < _numberOfRetries; i++)
            {
                var result = await _inner.SendMessageAsync<TMessage, TResponse>(topic, message, headers);
                if (!result.IsServiceUnavailable())
                {
                    return result;
                }
            }

            return ClientResult.ServiceUnavailable<TResponse>();
        }
    }

}
