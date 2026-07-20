using System.Threading.Tasks;
using Benzene.Http.RequestBody;
using Xunit;

namespace Benzene.Test.Http;

/// <summary>
/// Coverage for the shared async body-buffering primitive that removes the sync-over-async block from
/// the HTTP body getters: <see cref="BufferRequestBodyMiddleware{TContext}"/> reads the body once via
/// the transport's <see cref="IHttpRequestBodyReader{TContext}"/> and stores it in the scoped
/// <see cref="HttpRequestBodyBuffer"/>, which the synchronous body getter then serves from memory.
/// </summary>
public class BufferRequestBodyMiddlewareTest
{
    private sealed class FakeContext
    {
    }

    private sealed class FakeReader : IHttpRequestBodyReader<FakeContext>
    {
        private readonly string? _body;
        public int ReadCount { get; private set; }

        public FakeReader(string? body) => _body = body;

        public Task<string?> ReadBodyAsync(FakeContext context)
        {
            ReadCount++;
            return Task.FromResult(_body);
        }
    }

    [Fact]
    public void HttpRequestBodyBuffer_BeforeSet_IsNotBuffered()
    {
        var buffer = new HttpRequestBodyBuffer();

        Assert.False(buffer.IsBuffered);
        Assert.Null(buffer.Body);
    }

    [Fact]
    public void HttpRequestBodyBuffer_Set_StoresBodyAndMarksBuffered()
    {
        var buffer = new HttpRequestBodyBuffer();

        buffer.Set("hello");

        Assert.True(buffer.IsBuffered);
        Assert.Equal("hello", buffer.Body);
    }

    [Fact]
    public void HttpRequestBodyBuffer_SetNull_IsBufferedButNullBody()
    {
        var buffer = new HttpRequestBodyBuffer();

        buffer.Set(null);

        // A bodyless request is still "buffered" - the getter must serve null, not fall back to a read.
        Assert.True(buffer.IsBuffered);
        Assert.Null(buffer.Body);
    }

    [Fact]
    public async Task HandleAsync_ReadsBodyOnceIntoBuffer_ThenCallsNext()
    {
        var buffer = new HttpRequestBodyBuffer();
        var reader = new FakeReader("the-body");
        var middleware = new BufferRequestBodyMiddleware<FakeContext>(reader, buffer);

        var nextCalled = false;
        await middleware.HandleAsync(new FakeContext(), () =>
        {
            // The buffer must already be populated by the time the rest of the pipeline runs.
            Assert.True(buffer.IsBuffered);
            Assert.Equal("the-body", buffer.Body);
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Equal(1, reader.ReadCount);
    }
}
