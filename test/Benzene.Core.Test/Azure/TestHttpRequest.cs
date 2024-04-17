using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Benzene.Test.Azure;

public class TestHttpRequest : HttpRequest
{
    public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        throw new System.NotImplementedException();
    }

    public override HttpContext HttpContext { get; } = new DefaultHttpContext();
    public override string Method { get; set; }
    public override string Scheme { get; set; }
    public override bool IsHttps { get; set; }
    public override HostString Host { get; set; }
    public override PathString PathBase { get; set; }
    public override PathString Path { get; set; }
    public override QueryString QueryString { get; set; }
    public override IQueryCollection Query { get; set; }
    public override string Protocol { get; set; }
    public override IHeaderDictionary Headers => new HeaderDictionary();
    public override IRequestCookieCollection Cookies { get; set; }
    public override long? ContentLength { get; set; }
    public override string ContentType { get; set; }
    public override Stream Body { get; set; } = Stream.Null;
    public override bool HasFormContentType { get; }
    public override IFormCollection Form { get; set; }
}
