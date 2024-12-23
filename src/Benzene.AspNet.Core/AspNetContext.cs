using Benzene.Abstractions.Results;
using Benzene.Http;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

public class AspNetContext : IHttpContext
{
    public AspNetContext(HttpContext httpContext)
    {
        HttpContext = httpContext;
    }

    public HttpContext HttpContext { get; }
}