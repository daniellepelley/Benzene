using Benzene.Abstractions.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Benzene.Azure.Core.AspNet;

public class AspNetContext : IHasMessageResult
{
    public AspNetContext(HttpRequest httpRequest)
    {
        HttpRequest = httpRequest;
    }

    public HttpRequest HttpRequest { get; }
    public ContentResult? ContentResult { get; set; }
    public IMessageResult? MessageResult { get; set; }
}
