using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Clients;
using Benzene.Core.Correlation;
using Benzene.Results;

namespace Benzene.Elements.Core.Clients;

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
                { _correlationKey, _correlationId.Get() }
            };
        }

        headers[_correlationKey] = _correlationId.Get();

        return headers;
    }
}
