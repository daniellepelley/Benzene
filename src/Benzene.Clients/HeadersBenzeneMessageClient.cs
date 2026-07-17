using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;

namespace Benzene.Clients
{
    /// <summary>
    /// Superseded by <see cref="IBenzeneMessageSender"/>'s per-call <c>headers</c> parameter. See
    /// <c>work/benzene-clients-redesign-plan.md</c>.
    /// </summary>
    [Obsolete("Use IBenzeneMessageSender.SendAsync's headers parameter instead - see work/benzene-clients-redesign-plan.md")]
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

        public Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(
            IBenzeneClientRequest<TRequest> request)
        {
            var headers = PopulateHeaders(request.Headers, _clientHeaders.Get());
            return _inner.SendMessageAsync<TRequest, TResponse>(
                new BenzeneClientRequest<TRequest>(request.Topic, request.Message, headers));
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
