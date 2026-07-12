using Benzene.Http;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Implements <see cref="IHttpContext"/> for a request flowing through a real ASP.NET Core middleware
/// pipeline (as opposed to an Azure Functions HTTP trigger — see <c>Benzene.Azure.Function.AspNet</c>).
/// </summary>
public class AspNetContext : IHttpContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetContext"/> class.
    /// </summary>
    /// <param name="httpContext">The ASP.NET Core HTTP context for the current request.</param>
    public AspNetContext(HttpContext httpContext)
    {
        HttpContext = httpContext;
    }

    /// <summary>
    /// Gets the ASP.NET Core HTTP context for the current request.
    /// </summary>
    public HttpContext HttpContext { get; }
}
