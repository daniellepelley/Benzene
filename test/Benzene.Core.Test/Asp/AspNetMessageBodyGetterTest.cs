using System.IO;
using System.Text;
using System.Threading.Tasks;
using Benzene.AspNet.Core;
using Benzene.Http.RequestBody;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Benzene.Test.Asp;

/// <summary>
/// Coverage for <see cref="AspNetMessageBodyGetter"/>'s async-buffering behavior: it serves the body
/// from the scoped <see cref="HttpRequestBodyBuffer"/> when the async pre-read populated it (the
/// normal, non-blocking path), reads the stream asynchronously via
/// <see cref="IHttpRequestBodyReader{AspNetContext}"/>, and only falls back to reading the stream from
/// <c>GetBody</c> when nothing buffered it.
/// </summary>
public class AspNetMessageBodyGetterTest
{
    private static AspNetContext ContextWithBody(string body)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return new AspNetContext(httpContext);
    }

    [Fact]
    public void GetBody_WhenBuffered_ReturnsBufferedValue_WithoutTouchingTheStream()
    {
        var buffer = new HttpRequestBodyBuffer();
        buffer.Set("{\"buffered\":true}");
        var getter = new AspNetMessageBodyGetter(buffer);

        // The stream body differs from the buffered value - proving GetBody serves the buffer, not the stream.
        var result = getter.GetBody(ContextWithBody("{\"fromStream\":true}"));

        Assert.Equal("{\"buffered\":true}", result);
    }

    [Fact]
    public async Task ReadBodyAsync_ReadsTheStreamBody()
    {
        var getter = new AspNetMessageBodyGetter(new HttpRequestBodyBuffer());

        var result = await getter.ReadBodyAsync(ContextWithBody("{\"name\":\"orders\"}"));

        Assert.Equal("{\"name\":\"orders\"}", result);
    }

    [Fact]
    public void GetBody_WhenNotBuffered_FallsBackToReadingTheStream()
    {
        // No buffering middleware ran (IsBuffered == false), so GetBody must still return the body by
        // reading the stream itself.
        var getter = new AspNetMessageBodyGetter(new HttpRequestBodyBuffer());

        var result = getter.GetBody(ContextWithBody("{\"fallback\":true}"));

        Assert.Equal("{\"fallback\":true}", result);
    }

    [Fact]
    public async Task ReadBodyAsync_LeavesBodyReadableForDownstream()
    {
        // EnableBuffering + position reset means a component reading the body after the pre-read still sees it.
        var context = ContextWithBody("{\"reread\":true}");
        var getter = new AspNetMessageBodyGetter(new HttpRequestBodyBuffer());

        await getter.ReadBodyAsync(context);

        using var reader = new StreamReader(context.HttpContext.Request.Body);
        Assert.Equal("{\"reread\":true}", await reader.ReadToEndAsync());
    }
}
