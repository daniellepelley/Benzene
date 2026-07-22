using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients;

public static class ClientExtensions
{
    public static Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(
        this IBenzeneMessageClient source, string topic, TMessage message)
    {
        return source.SendMessageAsync<TMessage, TResponse>(topic, message, new Dictionary<string, string>());
    }

    public static Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(
        this IBenzeneMessageClient source, string topic, TMessage message, IDictionary<string, string> headers)
    {
        return source.SendMessageAsync<TMessage, TResponse>(new BenzeneClientRequest<TMessage>(topic, message, headers));
    }

    public static async Task<IBenzeneResult> SendMessageAsync<TRequest>(this IBenzeneMessageClient client,
        string topic, TRequest request)
    {
        var clientRequest = new BenzeneClientRequest<TRequest>(topic, request, new Dictionary<string, string>());
        return await client.SendMessageAsync<TRequest, Void>(clientRequest);
    }

    /// <summary>
    /// Sends <paramref name="message"/> on <paramref name="topic"/> declaring the payload schema
    /// <paramref name="version"/>, so a consumer that upcasts (or dispatches by version) sees it. The version
    /// travels in the standard <see cref="MessageVersionHeaders.Default"/> header, merged over any existing
    /// version header in <paramref name="headers"/>.
    /// </summary>
    public static Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(
        this IBenzeneMessageClient source, string topic, TMessage message, string version,
        IDictionary<string, string>? headers = null)
    {
        return source.SendMessageAsync<TMessage, TResponse>(topic, message, WithVersion(headers, version));
    }

    /// <summary>
    /// Returns a copy of <paramref name="headers"/> with the standard version header
    /// (<see cref="MessageVersionHeaders.Default"/>) set to <paramref name="version"/>. A null/empty version
    /// leaves the headers unchanged (returns a non-null copy).
    /// </summary>
    public static IDictionary<string, string> WithVersion(this IDictionary<string, string>? headers, string version)
    {
        var merged = headers is null ? new Dictionary<string, string>() : new Dictionary<string, string>(headers);
        if (!string.IsNullOrEmpty(version))
        {
            merged[MessageVersionHeaders.Default] = version;
        }
        return merged;
    }

    /// <summary>
    /// Sends <paramref name="request"/> on <paramref name="topic"/> declaring the payload schema
    /// <paramref name="version"/>. The version travels in the standard
    /// <see cref="MessageVersionHeaders.Default"/> header (merged over <paramref name="headers"/>), so the
    /// receiving service can upcast an older payload to its handler's schema or dispatch to a version-specific
    /// handler.
    /// </summary>
    public static Task<IBenzeneResult<TResponse>> SendAsync<TRequest, TResponse>(
        this IBenzeneMessageSender sender, string topic, TRequest request, string version,
        IDictionary<string, string>? headers = null)
    {
        return sender.SendAsync<TRequest, TResponse>(topic, request, WithVersion(headers, version));
    }
}
