using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Http;
using Microsoft.AspNetCore.Mvc;
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// Implements <see cref="IHttpContext"/> for an Azure Functions HTTP trigger request.
/// </summary>
public class AspNetContext : IHttpContext, IHasMessageResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetContext"/> class.
    /// </summary>
    /// <param name="httpRequest">The incoming ASP.NET Core HTTP request supplied by the Functions host.</param>
    public AspNetContext(HttpRequest httpRequest)
    {
        HttpRequest = httpRequest;
    }

    /// <summary>
    /// Gets the incoming ASP.NET Core HTTP request.
    /// </summary>
    public HttpRequest HttpRequest { get; }

    /// <summary>
    /// Gets or sets the action result to return from the HTTP trigger function. Set by
    /// <see cref="AspNetResponseAdapter"/>; created lazily via <see cref="Extensions.EnsureResponseExists"/>.
    /// </summary>
    public ContentResult? ContentResult { get; set; }

    /// <summary>
    /// Gets or sets the outcome of handling this request, recorded by the response-writing result setter
    /// so a cross-cutting observer of the completed pipeline (e.g. metrics) sees a real success/failure
    /// signal rather than a missing one.
    /// </summary>
    public IBenzeneResult MessageResult { get; set; } = null!;
}
