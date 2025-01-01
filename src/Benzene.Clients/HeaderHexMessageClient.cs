using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Clients
{
    public class HeaderBenzeneMessageClient : IBenzeneMessageClient
    {
        private readonly IBenzeneMessageClient _inner;
        private readonly string _key;
        private readonly string _value;

        public HeaderBenzeneMessageClient(IBenzeneMessageClient inner, string key, string value)
        {
            _inner = inner;
            _key = key;
            _value = value;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
        {
            var matchingHeaders = PopulateHeaders(request.Headers);
            return _inner.SendMessageAsync<TRequest, TResponse>(new BenzeneClientRequest<TRequest>(request.Topic, request.Message, matchingHeaders));
        }

        private IDictionary<string, string> PopulateHeaders(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                return new Dictionary<string, string>
                {
                    { _key, _value }
                };
            }

            headers[_key] = _value;

            return headers;
        }
    }
}
