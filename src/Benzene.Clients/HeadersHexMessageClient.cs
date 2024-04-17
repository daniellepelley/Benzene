using Benzene.Results;

namespace Benzene.Clients
{
    public sealed class HeadersBenzeneMessageClient : IBenzeneMessageClient
    {
        private readonly IClientHeaders _clientHeaders;
        private readonly IBenzeneMessageClient _inner;

        public HeadersBenzeneMessageClient(IBenzeneMessageClient inner, IClientHeaders clientHeaders)
        {
            _clientHeaders = clientHeaders;
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public Task<IClientResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message, IDictionary<string, string> headers)
        {
            var newHeaders = PopulateHeaders(headers, _clientHeaders.Get());
            return _inner.SendMessageAsync<TMessage, TResponse>(topic, message, newHeaders);
        }

        private static IDictionary<string, string> PopulateHeaders(IDictionary<string, string> sourceHeaders,
            IDictionary<string, string> newHeaders)
        {
            foreach (var newHeader in newHeaders)
            {
                sourceHeaders[newHeader.Key] = newHeader.Value;
            }

            return sourceHeaders;
        }
    }
}
