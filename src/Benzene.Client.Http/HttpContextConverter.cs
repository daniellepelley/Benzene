using System.Text;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Middleware.BenzeneClient;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Clients.Common;
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

    public HttpSendMessageContext CreateRequest(IBenzeneClientContext<TRequest, TResponse> contextIn)
    {
        return new HttpSendMessageContext(new HttpRequestMessage
        {
            Content = new StringContent(_serializer.Serialize(contextIn.Request.Message), Encoding.UTF8, "application/json"),
            RequestUri = new Uri(_path),
            Method = new HttpMethod(_verb)
        });
    }

    public void MapResponse(IBenzeneClientContext<TRequest, TResponse> contextIn, HttpSendMessageContext contextOut)
    {
        var body = contextOut.Response.Content.ReadAsStringAsync().Result;
        var response = _serializer.Deserialize<TResponse>(body);
        contextIn.Response = contextOut.Response.StatusCode.Convert<TResponse>(response);
    }
}