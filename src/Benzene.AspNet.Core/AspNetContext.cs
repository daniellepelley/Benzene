using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Http;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Implements <see cref="IHttpContext"/> for a request flowing through a real ASP.NET Core middleware
/// pipeline (as opposed to an Azure Functions HTTP trigger — see <c>Benzene.Azure.Function.AspNet</c>).
/// </summary>
public class AspNetContext : IHttpContext, IHasMessageResult
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

    /// <summary>
    /// Gets or sets the outcome of handling this request, recorded by
    /// <see cref="AspMessageHandlerResultSetter"/> so a cross-cutting observer of the completed pipeline
    /// (e.g. metrics) sees a real success/failure signal rather than a missing one.
    /// </summary>
    public IBenzeneResult MessageResult { get; set; } = null!;
}
