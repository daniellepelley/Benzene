using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Http;
using Benzene.Mesh.Ui;
using Moq;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshFleetUiMiddlewareTest
{
    public class FakeHttpContext : IHttpContext
    {
    }

    private static (Mock<IHttpRequestAdapter<FakeHttpContext>> RequestAdapter, Mock<IBenzeneResponseAdapter<FakeHttpContext>> ResponseAdapter)
        CreateAdapters(FakeHttpContext context, string method, string path)
    {
        var requestAdapter = new Mock<IHttpRequestAdapter<FakeHttpContext>>();
        requestAdapter.Setup(x => x.Map(context)).Returns(new HttpRequest { Method = method, Path = path });

        var responseAdapter = new Mock<IBenzeneResponseAdapter<FakeHttpContext>>();

        return (requestAdapter, responseAdapter);
    }

    [Theory]
    // Path matching must be case-insensitive and trailing-slash-insensitive, matching the sibling
    // MeshUiMiddleware/SpecUiMiddleware convention. The Fleet middleware's NormalizePath omitted the
    // final ToLowerInvariant, so a canonically-cased request fell through to next() (a 404).
    [InlineData("/mesh-fleet-ui/")]
    [InlineData("/MESH-FLEET-UI")]
    [InlineData("/Mesh-Fleet-Ui/")]
    public async Task HandleAsync_PathNormalization_TrailingSlashAndCaseAreIgnored(string requestPath)
    {
        var context = new FakeHttpContext();
        var (requestAdapter, responseAdapter) = CreateAdapters(context, "GET", requestPath);
        var middleware = new MeshFleetUiMiddleware<FakeHttpContext>(
            "/mesh-fleet-ui", "envelope.json", requestAdapter.Object, responseAdapter.Object);

        var nextCalled = false;
        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.False(nextCalled);
        responseAdapter.Verify(x => x.SetStatusCode(context, "200"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ConfiguredPathWithTrailingSlashAndDifferentCase_StillMatches()
    {
        var context = new FakeHttpContext();
        var (requestAdapter, responseAdapter) = CreateAdapters(context, "GET", "/mesh-fleet-ui");
        var middleware = new MeshFleetUiMiddleware<FakeHttpContext>(
            "/Mesh-Fleet-Ui/", "envelope.json", requestAdapter.Object, responseAdapter.Object);

        var nextCalled = false;
        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.False(nextCalled);
        responseAdapter.Verify(x => x.SetStatusCode(context, "200"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NonMatchingPath_CallsNext()
    {
        var context = new FakeHttpContext();
        var (requestAdapter, responseAdapter) = CreateAdapters(context, "GET", "/other");
        var middleware = new MeshFleetUiMiddleware<FakeHttpContext>(
            "/mesh-fleet-ui", "envelope.json", requestAdapter.Object, responseAdapter.Object);

        var nextCalled = false;
        await middleware.HandleAsync(context, () => { nextCalled = true; return Task.CompletedTask; });

        Assert.True(nextCalled);
        responseAdapter.Verify(x => x.SetBody(context, It.IsAny<string>()), Times.Never);
    }
}
