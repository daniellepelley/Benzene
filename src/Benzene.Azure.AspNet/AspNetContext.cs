using Benzene.Abstractions.Results;
using Benzene.Http;
using Microsoft.AspNetCore.Mvc;
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

namespace Benzene.Azure.AspNet;

public class AspNetContext : IHasMessageResult, IHttpContext
{
    public AspNetContext(HttpRequest httpRequest)
    {
        HttpRequest = httpRequest;
    }

    public HttpRequest HttpRequest { get; }
    public ContentResult? ContentResult { get; set; }
    public IMessageResult? MessageResult { get; set; }
}
