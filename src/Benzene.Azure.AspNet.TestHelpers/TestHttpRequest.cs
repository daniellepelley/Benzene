using Microsoft.AspNetCore.Http;

namespace Benzene.Azure.AspNet.TestHelpers;

/// <summary>
/// A minimal, settable <see cref="HttpRequest"/> implementation used by
/// <see cref="HttpBuilderExtensions.AsAspNetCoreHttpRequest{T}(IHttpBuilder{T})"/> to build requests for
/// tests, since ASP.NET Core's own <see cref="HttpRequest"/> properties are otherwise read-only outside
/// of a real <see cref="HttpContext"/>.
/// </summary>
public class TestHttpRequest : HttpRequest
{
    /// <summary>
    /// Not implemented; form reading isn't needed for the scenarios this class supports.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <exception cref="System.NotImplementedException">Always thrown.</exception>
    public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Gets the HTTP context. Always a fresh <see cref="DefaultHttpContext"/>.
    /// </summary>
    public override HttpContext HttpContext { get; } = new DefaultHttpContext();

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public override string Method { get; set; }

    /// <summary>
    /// Gets or sets the request scheme.
    /// </summary>
    public override string Scheme { get; set; }

    /// <summary>
    /// Gets or sets whether the request was made over HTTPS.
    /// </summary>
    public override bool IsHttps { get; set; }

    /// <summary>
    /// Gets or sets the request host.
    /// </summary>
    public override HostString Host { get; set; }

    /// <summary>
    /// Gets or sets the request path base.
    /// </summary>
    public override PathString PathBase { get; set; }

    /// <summary>
    /// Gets or sets the request path.
    /// </summary>
    public override PathString Path { get; set; }

    /// <summary>
    /// Gets or sets the raw query string.
    /// </summary>
    public override QueryString QueryString { get; set; }

    /// <summary>
    /// Gets or sets the parsed query collection.
    /// </summary>
    public override IQueryCollection Query { get; set; }

    /// <summary>
    /// Gets or sets the request protocol.
    /// </summary>
    public override string Protocol { get; set; }

    /// <summary>
    /// Gets the request headers.
    /// </summary>
    public override IHeaderDictionary Headers { get; } = new HeaderDictionary();

    /// <summary>
    /// Gets or sets the request cookies.
    /// </summary>
    public override IRequestCookieCollection Cookies { get; set; }

    /// <summary>
    /// Gets or sets the content length.
    /// </summary>
    public override long? ContentLength { get; set; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public override string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the request body stream. Defaults to <see cref="Stream.Null"/>.
    /// </summary>
    public override Stream Body { get; set; } = Stream.Null;

    /// <summary>
    /// Gets whether the request has a recognized form content type. Always <c>false</c>.
    /// </summary>
    public override bool HasFormContentType { get; }

    /// <summary>
    /// Gets or sets the parsed form collection.
    /// </summary>
    public override IFormCollection Form { get; set; }
}
