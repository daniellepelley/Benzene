using System.Text;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Results;

namespace Benzene.Client.Http;

public class HttpContextConverter<TRequest, TResponse> : IContextConverter<IBenzeneClientContext<TRequest, TResponse>, HttpSendMessageContext>
{
    private readonly ISerializer _serializer;
    private readonly string _verb;
    private readonly string _path;

    public HttpContextConverter(string verb, string path)
        :this(new JsonSerializer())
    {
        _path = path;
        _verb = verb;
    }
    
    public HttpContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public Task<HttpSendMessageContext> CreateRequestAsync(IBenzeneClientContext<TRequest, TResponse> contextIn)
    {
        var request = new HttpRequestMessage
        {
            Content = new StringContent(_serializer.Serialize(contextIn.Request.Message), Encoding.UTF8, "application/json"),
            RequestUri = new Uri(_path),
            Method = new HttpMethod(_verb)
        };

        foreach (var header in contextIn.Request.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return Task.FromResult(new HttpSendMessageContext(request));
    }

    public async Task MapResponseAsync(IBenzeneClientContext<TRequest, TResponse> contextIn, HttpSendMessageContext contextOut)
    {
        // Both HttpRequestMessage and HttpResponseMessage are IDisposable and created per outbound
        // call; dispose them once the body has been read (this is the terminal step that consumes the
        // response). Not socket exhaustion today - SendAsync buffers the body - but a real
        // disposable-not-disposed gap that would bite under a future streaming completion option.
        contextOut.Request?.Dispose();
        using var httpResponse = contextOut.Response;
        var body = await httpResponse.Content.ReadAsStringAsync();
        var response = _serializer.Deserialize<TResponse>(body)!;
        contextIn.Response = httpResponse.StatusCode.Convert(response);
    }
}