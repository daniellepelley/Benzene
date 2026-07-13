using System.Diagnostics;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;

namespace Benzene.Clients.TraceContext;

/// <summary>
/// A Benzene message client decorator that stamps the current <see cref="Activity"/>'s W3C
/// <c>traceparent</c>/<c>tracestate</c> onto outgoing message headers, so the receiving service can
/// continue the same distributed trace.
/// </summary>
public class TraceContextBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneMessageClient _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceContextBenzeneMessageClient"/> class.
    /// </summary>
    /// <param name="inner">The inner client to decorate.</param>
    public TraceContextBenzeneMessageClient(IBenzeneMessageClient inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _inner.Dispose();
    }

    /// <inheritdoc />
    public Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        var headers = PopulateHeaders(request.Headers);
        return _inner.SendMessageAsync<TRequest, TResponse>(new BenzeneClientRequest<TRequest>(request.Topic, request.Message, headers));
    }

    private static IDictionary<string, string> PopulateHeaders(IDictionary<string, string> headers)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return headers ?? new Dictionary<string, string>();
        }

        headers ??= new Dictionary<string, string>();
        headers["traceparent"] = activity.Id!;
        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            headers["tracestate"] = activity.TraceStateString;
        }

        return headers;
    }
}
