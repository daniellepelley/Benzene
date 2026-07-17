using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;

namespace Benzene.Clients
{
    /// <summary>
    /// Superseded by <see cref="IBenzeneMessageSender"/>'s per-call <c>headers</c> parameter. See
    /// <c>work/benzene-clients-redesign-plan.md</c>.
    /// </summary>
    [Obsolete("Use IBenzeneMessageSender.SendAsync's headers parameter instead - see work/benzene-clients-redesign-plan.md")]
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
