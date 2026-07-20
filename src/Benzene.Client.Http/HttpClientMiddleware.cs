using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Client.Http;

public class HttpClientMiddleware : IMiddleware<HttpSendMessageContext>
{
    private readonly HttpClient _httpClient;
    private readonly ICancellationTokenAccessor _cancellationTokenAccessor;

    public HttpClientMiddleware(HttpClient httpClient)
        : this(httpClient, null)
    {
    }

    /// <summary>
    /// Initializes the middleware, additionally resolving the ambient cancellation token so an
    /// upstream cancel/timeout aborts the outbound request instead of running it to completion.
    /// </summary>
    public HttpClientMiddleware(HttpClient httpClient, ICancellationTokenAccessor cancellationTokenAccessor)
    {
        _httpClient = httpClient;
        _cancellationTokenAccessor = cancellationTokenAccessor;
    }

    public string Name => nameof(HttpClientMiddleware);

    public async Task HandleAsync(HttpSendMessageContext context, Func<Task> next)
    {
        var cancellationToken = _cancellationTokenAccessor?.CancellationToken ?? CancellationToken.None;
        context.Response = await _httpClient.SendAsync(context.Request, cancellationToken);
    }
}
