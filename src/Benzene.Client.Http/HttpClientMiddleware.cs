using Benzene.Abstractions.Middleware;

namespace Benzene.Client.Http;

public class HttpClientMiddleware : IMiddleware<HttpSendMessageContext>
{
    private readonly HttpClient _httpClient;

    public HttpClientMiddleware(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public string Name => nameof(HttpClientMiddleware);

    public async Task HandleAsync(HttpSendMessageContext context, Func<Task> next)
    {
        context.Response = await _httpClient.SendAsync(context.Request);
    }
}