using Benzene.Azure.AspNet;

namespace Benzene.Aws.ApiGateway;

public class AspNetHttpRequestAdapter : IHttpRequestAdapter<AspNetContext>
{
    public HttpRequest2 Map(AspNetContext context)
    {
        return new HttpRequest2
        {
            Path = context.HttpRequest.Path.Value,
            Method = context.HttpRequest.Method,
            Headers = context.HttpRequest.Headers.ToDictionary(x =>x.Key.ToLowerInvariant(), x => x.Value.ToString())
        };
    }
}