using Benzene.Abstractions.Results;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

public class AspNetContext : IHasMessageResult, IHttpContext
{
    public AspNetContext(HttpContext httpContext)
    {
        HttpContext = httpContext;
    }

    public HttpContext HttpContext { get; }
    public IMessageResult? MessageResult { get; set; }
}