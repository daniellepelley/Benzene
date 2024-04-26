using Benzene.Azure.AspNet;
using Benzene.Http;

namespace Benzene.Aws.ApiGateway;

public class AspNetHttpRequestAdapter : IHttpRequestAdapter<AspNetContext>
{
    public HttpRequest Map(AspNetContext context)
    {
        return new HttpRequest
        {
            Path = context.HttpRequest.Path.Value,
            Method = context.HttpRequest.Method,
            Headers = context.HttpRequest.Headers.ToDictionary(x =>x.Key.ToLowerInvariant(), x => x.Value.ToString())
        };
    }
}