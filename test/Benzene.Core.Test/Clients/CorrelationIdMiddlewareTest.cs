using System.Threading.Tasks;
using Benzene.Abstractions;
using Benzene.Clients;
using Benzene.Clients.CorrelationId;
using Moq;
using Xunit;

namespace Benzene.Test.Clients;

public class CorrelationIdMiddlewareTest
{
    [Fact]
    public async Task HandleAsync_StampsCorrelationIdOntoContextHeaders_UnderDefaultKey()
    {
        var mockCorrelationId = new Mock<ICorrelationId>();
        mockCorrelationId.Setup(x => x.Get()).Returns("abc-123");

        var context = new OutboundContext("my-topic", "hello");
        var middleware = new CorrelationIdMiddleware(mockCorrelationId.Object);
        var nextCalled = false;

        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        Assert.Equal("abc-123", context.Headers["correlationId"]);
    }

    [Fact]
    public async Task HandleAsync_CustomCorrelationKey_StampsUnderThatKey()
    {
        var mockCorrelationId = new Mock<ICorrelationId>();
        mockCorrelationId.Setup(x => x.Get()).Returns("abc-123");

        var context = new OutboundContext("my-topic", "hello");
        var middleware = new CorrelationIdMiddleware(mockCorrelationId.Object, "x-correlation-id");

        await middleware.HandleAsync(context, () => Task.CompletedTask);

        Assert.Equal("abc-123", context.Headers["x-correlation-id"]);
    }
}
