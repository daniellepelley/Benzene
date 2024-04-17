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

        public Task<IClientResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message, IDictionary<string, string> headers)
        {
            var matchingHeaders = PopulateHeaders(headers);
            return _inner.SendMessageAsync<TMessage, TResponse>(topic, message, matchingHeaders);
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
