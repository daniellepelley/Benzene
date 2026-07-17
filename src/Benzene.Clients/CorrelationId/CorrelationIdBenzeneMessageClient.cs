using Benzene.Abstractions;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;

namespace Benzene.Clients.CorrelationId;

/// <summary>
/// Superseded by <see cref="CorrelationIdMiddleware"/>/<c>.UseCorrelationId()</c> on an outbound
/// route pipeline. See <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use CorrelationIdMiddleware/.UseCorrelationId() instead - see work/benzene-clients-redesign-plan.md")]
public class CorrelationIdBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneMessageClient _inner;
    private readonly ICorrelationId _correlationId;
    private readonly string _correlationKey;

    public CorrelationIdBenzeneMessageClient(IBenzeneMessageClient inner, ICorrelationId correlationId, string correlationKey = "correlationId")
    {
        _correlationKey = correlationKey;
        _correlationId = correlationId;
        _inner = inner;
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
                { _correlationKey, _correlationId.Get() }
            };
        }

        headers[_correlationKey] = _correlationId.Get();

        return headers;
    }
}

